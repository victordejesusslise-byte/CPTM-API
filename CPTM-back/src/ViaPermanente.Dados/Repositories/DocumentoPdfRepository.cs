using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using ViaPermanente.Dominio.Models.Laudos;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

/// <summary>
/// Desenha os laudos com o PDFsharp/MigraDoc (licença MIT). A fonte é a Arial do Windows (FontesDoWindows):
/// para rodar fora do Windows, o resolvedor precisa apontar para outra pasta de fontes.
/// </summary>
public class DocumentoPdfRepository : IDocumentoPdfRepository
{
    private const double LarguraUtilCm = 18;

    static DocumentoPdfRepository()
    {
        GlobalFontSettings.FontResolver ??= new FontesDoWindows();
    }

    public byte[] Montar(DocumentoPdf d)
    {
        var documento = new Document();
        documento.Info.Title = $"{d.Codigo} · {d.Titulo}";
        documento.Info.Author = "Via Permanente";
        documento.Styles[StyleNames.Normal]!.Font.Name = "Arial";
        documento.Styles[StyleNames.Normal]!.Font.Size = 9;

        var secao = documento.AddSection();
        secao.PageSetup = documento.DefaultPageSetup.Clone();
        secao.PageSetup.PageFormat = PageFormat.A4;
        secao.PageSetup.LeftMargin = secao.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        secao.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
        secao.PageSetup.BottomMargin = Unit.FromCentimeter(2);

        var rodape = secao.Footers.Primary.AddParagraph($"{d.Codigo} · gerado em {d.GeradoEm:dd/MM/yyyy HH:mm} a partir das tabelas · página ");
        rodape.AddPageField();
        rodape.Format.Font.Size = 7;
        rodape.Format.Font.Color = Colors.Gray;

        var titulo = secao.AddParagraph(d.Titulo);
        titulo.Format.Font.Size = 16;
        titulo.Format.Font.Bold = true;
        var codigo = secao.AddParagraph($"{d.Codigo} · {d.Subtitulo}");
        codigo.Format.Font.Color = Colors.DimGray;
        codigo.Format.SpaceAfter = Unit.FromPoint(10);

        foreach (var s in d.Secoes)
        {
            var cabecalho = secao.AddParagraph(s.Titulo.ToUpperInvariant());
            cabecalho.Format.Font.Bold = true;
            cabecalho.Format.Font.Size = 8;
            cabecalho.Format.SpaceBefore = Unit.FromPoint(8);
            cabecalho.Format.SpaceAfter = Unit.FromPoint(3);
            cabecalho.Format.Borders.Bottom.Width = 0.5;

            if (s.Campos.Count > 0)
            {
                var campos = secao.AddTable();
                campos.AddColumn(Unit.FromCentimeter(5));
                campos.AddColumn(Unit.FromCentimeter(LarguraUtilCm - 5));
                foreach (var c in s.Campos)
                {
                    var linha = campos.AddRow();
                    linha.Cells[0].AddParagraph(c.Rotulo).Format.Font.Color = Colors.DimGray;
                    linha.Cells[1].AddParagraph(c.Valor);
                }
            }
            if (s.Tabela is { } t)
                Tabela(secao, t);
        }

        var renderizador = new PdfDocumentRenderer { Document = documento };
        renderizador.RenderDocument();
        using var memoria = new MemoryStream();
        renderizador.PdfDocument.Save(memoria);
        return memoria.ToArray();
    }

    private static void Tabela(Section secao, TabelaPdf t)
    {
        if (t.Linhas.Count == 0)
        {
            secao.AddParagraph("Nenhum registro.").Format.Font.Italic = true;
            return;
        }
        var tabela = secao.AddTable();
        tabela.Borders.Width = 0.25;
        tabela.Borders.Color = Colors.LightGray;
        foreach (var _ in t.Colunas)
            tabela.AddColumn(Unit.FromCentimeter(LarguraUtilCm / t.Colunas.Count));
        var cabecalho = tabela.AddRow();
        cabecalho.HeadingFormat = true;
        cabecalho.Format.Font.Bold = true;
        cabecalho.Shading.Color = Colors.WhiteSmoke;
        for (var i = 0; i < t.Colunas.Count; i++)
            cabecalho.Cells[i].AddParagraph(t.Colunas[i]);
        foreach (var valores in t.Linhas)
        {
            var linha = tabela.AddRow();
            for (var i = 0; i < t.Colunas.Count && i < valores.Count; i++)
                linha.Cells[i].AddParagraph(valores[i] ?? "");
        }
    }
}