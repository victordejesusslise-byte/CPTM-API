using ClosedXML.Excel;
using ViaPermanente.Dominio.Integracoes;

namespace ViaPermanente.Dados.Integracoes;

public class PlanilhaExportacaoRepository : IPlanilhaExportacaoRepository
{
    public byte[] Montar(string folha, IReadOnlyList<string> cabecalhos, IEnumerable<object?[]> linhas)
    {
        using var livro = new XLWorkbook();
        var aba = livro.AddWorksheet(folha);
        for (var i = 0; i < cabecalhos.Count; i++)
            aba.Cell(1, i + 1).Value = cabecalhos[i];
        aba.Row(1).Style.Font.Bold = true;
        var n = 2;
        foreach (var linha in linhas)
        {
            for (var i = 0; i < linha.Length; i++)
                aba.Cell(n, i + 1).Value = XLCellValue.FromObject(linha[i] is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : linha[i]);
            n++;
        }
        aba.Columns().AdjustToContents();
        using var memoria = new MemoryStream();
        livro.SaveAs(memoria);
        return memoria.ToArray();
    }
}