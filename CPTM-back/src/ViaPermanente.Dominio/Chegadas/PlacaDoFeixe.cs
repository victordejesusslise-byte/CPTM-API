using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dominio.Chegadas;

/// <summary>
/// Acha os 9 campos da placa no texto do OCR (Tesseract lê só texto) e confere com o pedido.
/// Entende dois formatos. "Rótulo: valor" por linha: "Fornecedor: TG – Três Guri", "Ata de registro: AR00224",
/// "Essência / classe: Ocotea neesiana, Lauraceae / 1ª", "Comprimento: 2,80 m", "Tratamento / empresa: CCA / Granoski",
/// "Empresa inspetora / ano: UFV / 2025", "Contrato: AR00224-03". E a placa real da CPTM: uma tabela de duas colunas, sem dois-pontos,
/// com rótulo e valor em duas linhas ("ESSÊNCIA/" + "CLASSE", "CCA" + "/ GRANOSKI", "EMPRESA" + "INSP. / ANO") e lixo do OCR em volta.
/// Tratamento, tratadora, inspetora e ano não estão no pedido: conferência nula ("sem pedido").
/// </summary>
public static partial class PlacaDoFeixe
{
    public static IReadOnlyList<CampoLido> Extrair(IReadOnlyList<LinhaOcr> linhas)
    {
        // Cada linha vira (rótulo, valor): pelo ":" ou, sem ":", pelo rótulo conhecido no começo da linha.
        var partes = linhas.Select(Separar).Where(p => p.HasValue).Select(p => p!.Value).ToList();
        var campos = new List<CampoLido>();
        for (var i = 0; i < partes.Count; i++)
        {
            var (rotulo, valor, linha) = partes[i];
            // Rótulo partido em duas linhas: junta rótulo com rótulo e valor com valor.
            if (i + 1 < partes.Count && Continua(rotulo, partes[i + 1].Rotulo))
            {
                var seguinte = partes[++i];
                rotulo += seguinte.Rotulo;
                valor = valor + "\n" + seguinte.Valor;
                linha = linha with { Confianca = Math.Min(linha.Confianca, seguinte.Linha.Confianca) };
            }
            void Add(CampoOcr campo, string v)
            {
                if (Limpar(campo, v) is { } limpo && campos.All(c => c.Campo != campo))
                    campos.Add(new(campo, limpo, linha.Confianca, linha));
            }

            // A ordem importa: "tratamento" contém "ata".
            if (rotulo.Contains("fornecedor")) Add(CampoOcr.Fornecedor, valor);
            else if (rotulo.Contains("tratamento")) { var (a, b) = Dividir(valor); Add(CampoOcr.Tratamento, a); Add(CampoOcr.Tratadora, b); }
            else if (rotulo.Contains("insp")) { var (a, b) = Dividir(valor); Add(CampoOcr.Inspetora, a); Add(CampoOcr.Ano, b); }
            else if (rotulo.Contains("contrato")) Add(CampoOcr.Contrato, valor);
            else if (rotulo.Contains("comprimento")) Add(CampoOcr.Comprimento, valor);
            else if (rotulo.Contains("essencia") || rotulo.Contains("especie")) Add(CampoOcr.EspecieClasse, valor);
            else if (rotulo.StartsWith("ata") || rotulo.Contains("regist")) Add(CampoOcr.Ata, valor);
        }
        return campos;
    }

    /// <summary>
    /// O fornecedor da placa real vem sozinho no alto (branco no preto, sem rótulo). Com pedido, procura o nome do fornecedor do pedido
    /// em qualquer linha, aceitando um erro de leitura a cada 5 letras.
    /// </summary>
    public static CampoLido? AcharFornecedor(IEnumerable<LinhaOcr> linhas, string fornecedor)
    {
        var alvo = Chave(fornecedor);
        if (alvo.Length < 3)
            return null;
        return linhas
            .Select(l => (Linha: l, Distancia: DistanciaDentro(alvo, Chave(l.Texto)), Valor: Limpar(CampoOcr.Fornecedor, l.Texto)))
            .Where(x => x.Valor is not null && x.Distancia <= alvo.Length / 5)
            .OrderBy(x => x.Distancia).ThenByDescending(x => x.Linha.Confianca)
            .Select(x => new CampoLido(CampoOcr.Fornecedor, x.Valor!, x.Linha.Confianca, x.Linha))
            .FirstOrDefault();
    }

    /// <summary>
    /// Junta os pedaços lidos que estão na mesma faixa da foto (mesma altura), da esquerda para a direita: o primeiro é o rótulo e o resto
    /// o valor ("ATA DE REGISTRO" + "AR00224" vira "ATA DE REGISTRO: AR00224"). Serve para as células da tabela e para o texto solto.
    /// </summary>
    public static IReadOnlyList<LinhaOcr> EmFaixas(IReadOnlyList<LinhaOcr> pedacos)
    {
        var faixas = new List<List<LinhaOcr>>();
        foreach (var p in pedacos.OrderBy(p => p.Y + p.Altura / 2.0))
        {
            var faixa = faixas.FirstOrDefault(f => Sobreposicao(f[0], p) >= Math.Min(f[0].Altura, p.Altura) * 0.5);
            if (faixa is null) faixas.Add([p]);
            else faixa.Add(p);
        }
        return faixas.Select(f =>
        {
            var ordem = f.OrderBy(p => p.X).ToList();
            var texto = ordem.Count == 1 ? ordem[0].Texto : ordem[0].Texto.TrimEnd(':', ' ') + ": " + string.Join(" ", ordem.Skip(1).Select(p => p.Texto));
            int x0 = ordem.Min(p => p.X), y0 = ordem.Min(p => p.Y);
            return new LinhaOcr(texto, ordem.Min(p => p.Confianca), x0, y0, ordem.Max(p => p.X + p.Largura) - x0, ordem.Max(p => p.Y + p.Altura) - y0);
        }).ToList();

        static int Sobreposicao(LinhaOcr a, LinhaOcr b) => Math.Min(a.Y + a.Altura, b.Y + b.Altura) - Math.Max(a.Y, b.Y);
    }

    /// <summary>Verdadeiro ou falso quando o pedido tem o dado; nulo quando não tem ("sem pedido").</summary>
    public static bool? Confere(CampoOcr campo, string valor, PedidoParaPlaca? pedido)
    {
        if (pedido is null) return null;
        switch (campo)
        {
            case CampoOcr.Fornecedor:
                var (lido, esperado) = (Chave(valor), Chave(pedido.Fornecedor));
                return lido.Length > 0 && (lido.Contains(esperado) || esperado.Contains(lido) || DistanciaDentro(esperado, lido) <= esperado.Length / 5);
            case CampoOcr.Ata:
                return pedido.Ata is null ? null : Chave(valor) == Chave(pedido.Ata);
            case CampoOcr.Contrato:
                return pedido.Contrato is null ? null : Chave(valor) == Chave(pedido.Contrato);
            case CampoOcr.EspecieClasse:
                var especie = Chave(valor.Split(',', '/')[0]);
                var classe = ClasseNoTexto().Match(valor) is { Success: true } m ? byte.Parse(m.Groups[1].Value) : (byte?)null;
                var classeConfere = classe is null || pedido.Classes.Contains(classe.Value);
                if (classe is null && pedido.Especies.Count == 0)
                    return null; // nada do pedido para comparar
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

    private static (string Rotulo, string Valor, LinhaOcr Linha)? Separar(LinhaOcr linha)
    {
        var texto = linha.Texto;
        var doisPontos = texto.IndexOf(':');
        if (doisPontos >= 0)
            return (Chave(texto[..doisPontos]), texto[(doisPontos + 1)..].Trim(), linha);
        // Sem ":", só a primeira linha: numa célula de rótulo sozinha ("ESSÊNCIA/" em cima, "CLASSE" embaixo), a de baixo é rótulo, não valor.
        // SemAcento não muda o tamanho do texto: a posição achada vale no original.
        var primeira = texto.Split('\n')[0];
        var m = RotuloSemDoisPontos().Match(SemAcento(primeira));
        return m.Success ? (Chave(m.Groups["rotulo"].Value), primeira[(m.Index + m.Length)..].Trim(), linha) : null;
    }

    /// <summary>A segunda linha completa o rótulo da primeira: "essência/" + "classe", "tratamento" + "/ empresa", "empresa" + "insp. / ano".</summary>
    private static bool Continua(string rotulo, string seguinte)
        => (seguinte == "classe" && (rotulo.Contains("essencia") || rotulo.Contains("especie")) && !rotulo.Contains("classe"))
        || (seguinte == "empresa" && rotulo.Contains("tratamento") && !rotulo.Contains("empresa"))
        || (seguinte.StartsWith("insp") && rotulo.EndsWith("empresa") && !rotulo.Contains("tratamento"));

    /// <summary>"CCA / Granoski" pela barra; sem barra, pela quebra de linha da célula ("CCA" em cima, "Granoski" embaixo).</summary>
    private static (string, string) Dividir(string valor)
    {
        if (valor.Split('/', 2) is [var a, var b]) return (a, b);
        return valor.Split('\n', 2) is [var c, var d] ? (c, d) : (valor, "");
    }

    /// <summary>
    /// Tira o lixo que o OCR põe em volta (barras verticais, colchetes, letras soltas na ponta). Códigos, comprimento e ano são pegos pelo formato.
    /// Nulo quando não sobra um valor que sirva.
    /// </summary>
    private static string? Limpar(CampoOcr campo, string valor)
    {
        var texto = Regex.Replace(Lixo().Replace(valor.Replace('\n', ' '), " "), @"\s+", " ").Trim();
        switch (campo)
        {
            case CampoOcr.Ata:
            case CampoOcr.Contrato:
                var codigo = (campo == CampoOcr.Ata ? CodigoAta() : CodigoContrato()).Match(texto.ToUpperInvariant());
                if (codigo.Success) return codigo.Value;
                break;
            case CampoOcr.Comprimento:
                return ComprimentoNoTexto().Match(texto) is { Success: true } c ? c.Value : null;
            case CampoOcr.Ano:
                return AnoNoTexto().Match(texto) is { Success: true } a ? a.Value : null;
        }
        var palavras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        static bool Solta(string p) => !p.Any(char.IsLetterOrDigit) || (p.Length == 1 && !char.IsDigit(p[0]));
        while (palavras.Count > 0 && Solta(palavras[0])) palavras.RemoveAt(0);
        while (palavras.Count > 0 && Solta(palavras[^1])) palavras.RemoveAt(palavras.Count - 1);
        texto = string.Join(" ", palavras);
        if (campo == CampoOcr.EspecieClasse)
            texto = ClasseLidaErrada().Replace(texto, "/ 1$1"); // "/ la": o OCR troca o 1 por l
        var letras = texto.Count(char.IsLetter);
        return campo switch
        {
            CampoOcr.Ata or CampoOcr.Contrato => texto.Any(char.IsDigit) ? texto : null,
            CampoOcr.EspecieClasse => letras >= 4 ? texto : null,
            _ => letras >= 2 ? texto : null
        };
    }

    /// <summary>Quantas letras é preciso trocar para achar o padrão dentro do texto (o melhor trecho do texto).</summary>
    private static int DistanciaDentro(string padrao, string texto)
    {
        var anterior = new int[texto.Length + 1];
        var atual = new int[texto.Length + 1];
        for (var i = 1; i <= padrao.Length; i++)
        {
            atual[0] = i;
            for (var j = 1; j <= texto.Length; j++)
                atual[j] = Math.Min(Math.Min(anterior[j] + 1, atual[j - 1] + 1), anterior[j - 1] + (padrao[i - 1] == texto[j - 1] ? 0 : 1));
            (anterior, atual) = (atual, anterior);
        }
        return padrao.Length == 0 ? 0 : anterior.Min();
    }

    /// <summary>Sem acento, sem espaço e sem pontuação, em minúsculas: "TG – Três Guri" vira "tgtresguri".</summary>
    public static string Chave(string texto)
    {
        var d = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(d.Where(char.IsLetterOrDigit).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    /// <summary>Minúsculas sem acento, letra por letra (o texto fica do mesmo tamanho): "ESSÊNCIA" vira "essencia".</summary>
    private static string SemAcento(string texto)
        => new(texto.Select(c => char.ToLowerInvariant(c.ToString().Normalize(NormalizationForm.FormD)[0])).ToArray());

    // Rótulo da placa no começo da linha, depois de até 3 pedaços curtos de lixo ("2 [E TRATAMENTO CCA", "ds EMPRESA UFV /").
    [GeneratedRegex(@"^(?:\S{1,3}\s+){0,3}?(?<rotulo>fornecedor|ata\s+de\s+regist\w*|contrato|comprimento|(?:essencia|especie)\s*/?\s*(?:classe)?|tratamento(?:\s*/\s*empresa)?|empresa(?:\s+insp\w*\.?\s*(?:/\s*ano)?)?|insp\w*\.?\s*/?\s*ano|classe|/\s*empresa)(?=[^a-z]|$)")]
    private static partial Regex RotuloSemDoisPontos();
    [GeneratedRegex(@"[|\[\]{}*«»“”""_~^`'‘’()<>!]")] private static partial Regex Lixo();
    [GeneratedRegex(@"[A-Z]{1,4}\d{3,}")] private static partial Regex CodigoAta();
    [GeneratedRegex(@"[A-Z]{1,4}\d{3,}(?:-\d{1,3})?")] private static partial Regex CodigoContrato();
    [GeneratedRegex(@"\d+[.,]\d+(?:\s*m\b)?", RegexOptions.IgnoreCase)] private static partial Regex ComprimentoNoTexto();
    [GeneratedRegex(@"(?:19|20)\d{2}")] private static partial Regex AnoNoTexto();
    [GeneratedRegex(@"/\s*[lI|!]\s*([aªº°]?)$")] private static partial Regex ClasseLidaErrada();
    [GeneratedRegex(@"([12])\s*[ªa°º]?\s*(classe)?\s*$", RegexOptions.IgnoreCase)] private static partial Regex ClasseNoTexto();
    [GeneratedRegex(@"\d+[.,]?\d*")] private static partial Regex NumeroNoTexto();
}
