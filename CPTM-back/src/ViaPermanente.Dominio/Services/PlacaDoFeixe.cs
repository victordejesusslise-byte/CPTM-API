using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ViaPermanente.Dominio.Models.Chegada;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// Acha os 9 campos da placa no texto do OCR (Tesseract lê só texto) e confere com o pedido.
/// Placa de exemplo: "Fornecedor: TG – Três Guri", "Ata de registro: AR00224", "Essência / classe: Ocotea neesiana, Lauraceae / 1ª",
/// "Comprimento: 2,80 m", "Tratamento / empresa: CCA / Granoski", "Empresa inspetora / ano: UFV / 2025", "Contrato: AR00224-03".
/// Tratamento, tratadora, inspetora e ano não estão no pedido: conferência nula ("sem pedido").
/// </summary>
public static partial class PlacaDoFeixe
{
    public static IReadOnlyList<CampoLido> Extrair(IReadOnlyList<LinhaOcr> linhas)
    {
        var campos = new List<CampoLido>();
        foreach (var linha in linhas)
        {
            var partes = linha.Texto.Split(':', 2);
            if (partes.Length < 2) continue;
            var rotulo = Chave(partes[0]);
            var valor = partes[1].Trim();
            if (valor.Length == 0) continue;
            void Add(CampoOcr campo, string v) { if (v.Trim().Length > 0 && campos.All(c => c.Campo != campo)) campos.Add(new(campo, v.Trim(), linha.Confianca, linha)); }

            if (rotulo.StartsWith("fornecedor")) Add(CampoOcr.Fornecedor, valor);
            else if (rotulo.StartsWith("ata")) Add(CampoOcr.Ata, valor);
            else if (rotulo.StartsWith("contrato")) Add(CampoOcr.Contrato, valor);
            else if (rotulo.StartsWith("essencia") || rotulo.StartsWith("especie")) Add(CampoOcr.EspecieClasse, valor);
            else if (rotulo.StartsWith("comprimento")) Add(CampoOcr.Comprimento, valor);
            else if (rotulo.StartsWith("tratamento")) { var (a, b) = Dividir(valor); Add(CampoOcr.Tratamento, a); Add(CampoOcr.Tratadora, b); }
            else if (rotulo.Contains("inspetora")) { var (a, b) = Dividir(valor); Add(CampoOcr.Inspetora, a); Add(CampoOcr.Ano, b); }
        }
        return campos;
    }

    /// <summary>Verdadeiro ou falso quando o pedido tem o dado; nulo quando não tem ("sem pedido").</summary>
    public static bool? Confere(CampoOcr campo, string valor, PedidoParaPlaca? pedido)
    {
        if (pedido is null) return null;
        switch (campo)
        {
            case CampoOcr.Fornecedor:
                var (lido, esperado) = (Chave(valor), Chave(pedido.Fornecedor));
                return lido.Length > 0 && (lido.Contains(esperado) || esperado.Contains(lido));
            case CampoOcr.Ata:
                return pedido.Ata is null ? null : Chave(valor) == Chave(pedido.Ata);
            case CampoOcr.Contrato:
                return pedido.Contrato is null ? null : Chave(valor) == Chave(pedido.Contrato);
            case CampoOcr.EspecieClasse:
                var especie = Chave(valor.Split(',', '/')[0]);
                var classe = ClasseNoTexto().Match(valor) is { Success: true } m ? byte.Parse(m.Groups[1].Value) : (byte?)null;
                var classeConfere = classe is null || pedido.Classes.Contains(classe.Value);
                return pedido.Especies.Count > 0
                    ? pedido.Especies.Any(e => Chave(e.Especie) == especie && (classe is null || e.Classe == classe)) && classeConfere
                    : classeConfere;
            case CampoOcr.Comprimento:
                return NumeroNoTexto().Match(valor) is { Success: true } n && decimal.TryParse(n.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var metros)
                    && pedido.Comprimentos.Contains(metros);
            default:
                return null;
        }
    }

    private static (string, string) Dividir(string valor) => valor.Split('/', 2) is [var a, var b] ? (a, b) : (valor, "");

    /// <summary>Sem acento, sem espaço e sem pontuação, em minúsculas: "TG – Três Guri" vira "tgtresguri".</summary>
    public static string Chave(string texto)
    {
        var d = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(d.Where(char.IsLetterOrDigit).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    [GeneratedRegex(@"([12])\s*[ªa°º]?\s*(classe)?\s*$", RegexOptions.IgnoreCase)] private static partial Regex ClasseNoTexto();
    [GeneratedRegex(@"\d+[.,]?\d*")] private static partial Regex NumeroNoTexto();
}