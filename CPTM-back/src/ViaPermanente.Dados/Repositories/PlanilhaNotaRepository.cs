using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ViaPermanente.Dominio.Models.Ambiental;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

/// <summary>Lê e monta a planilha de notas (modelo provisório) com o ClosedXML. Os cabeçalhos são achados pelo nome, sem acento e sem unidade.</summary>
public class PlanilhaNotaRepository : IPlanilhaNotaRepository
{
    private static readonly string[] Cabecalhos = ["Pedido", "Nota", "Emissão", "Valor", "Espécie", "Comprimento (m)", "Classe", "Quantidade", "Volume (m³)"];

    public IReadOnlyList<LinhaNotaPlanilha> Ler(Stream planilha)
    {
        using var livro = new XLWorkbook(planilha);
        var folha = livro.Worksheets.First();
        var colunas = folha.Row(1).CellsUsed().ToDictionary(c => Chave(c.GetString()), c => c.Address.ColumnNumber);
        var faltando = Cabecalhos.Select(Chave).Where(c => !colunas.ContainsKey(c)).ToList();
        if (faltando.Count > 0)
            throw new InvalidDataException($"Faltam as colunas: {string.Join(", ", faltando)}.");

        var linhas = new List<LinhaNotaPlanilha>();
        foreach (var linha in folha.RowsUsed().Where(r => r.RowNumber() > 1))
        {
            var erros = new List<string>();
            string? Texto(string cabecalho) => linha.Cell(colunas[Chave(cabecalho)]).GetString().Trim() is { Length: > 0 } t ? t : null;
            decimal? Numero(string cabecalho)
            {
                var celula = linha.Cell(colunas[Chave(cabecalho)]);
                if (celula.IsEmpty()) return null;
                if (celula.Value.IsNumber) return (decimal)celula.Value.GetNumber();
                var texto = celula.GetString().Trim();
                if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var br)) return br;
                erros.Add($"\"{texto}\" não é número em {cabecalho}.");
                return null;
            }
            DateOnly? Data(string cabecalho)
            {
                var celula = linha.Cell(colunas[Chave(cabecalho)]);
                if (celula.IsEmpty()) return null;
                if (celula.Value.IsDateTime) return DateOnly.FromDateTime(celula.Value.GetDateTime());
                if (DateOnly.TryParse(celula.GetString(), CultureInfo.GetCultureInfo("pt-BR"), out var d)) return d;
                erros.Add($"\"{celula.GetString()}\" não é data em {cabecalho}.");
                return null;
            }

            var quantidade = Numero("Quantidade");
            var classe = Numero("Classe");
            linhas.Add(new LinhaNotaPlanilha(linha.RowNumber(), Texto("Pedido"), Texto("Nota"), Data("Emissão"), Numero("Valor"), Texto("Espécie"),
                Numero("Comprimento (m)"), classe is decimal c ? (int)c : null, quantidade is decimal q ? (int)q : null, Numero("Volume (m³)"),
                erros.Count > 0 ? string.Join(" ", erros) : null));
        }
        return linhas;
    }

    public byte[] Modelo()
    {
        using var livro = new XLWorkbook();
        var folha = livro.AddWorksheet("Notas");
        for (var i = 0; i < Cabecalhos.Length; i++)
            folha.Cell(1, i + 1).Value = Cabecalhos[i];
        folha.Row(1).Style.Font.Bold = true;
        folha.Columns().AdjustToContents();
        using var memoria = new MemoryStream();
        livro.SaveAs(memoria);
        return memoria.ToArray();
    }

    /// <summary>"Comprimento (m)" e "comprimento" viram a mesma chave: sem acento, sem unidade, minúsculas.</summary>
    private static string Chave(string cabecalho)
    {
        var semUnidade = cabecalho.Split('(')[0].Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(semUnidade.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(ch)).ToArray());
    }
}