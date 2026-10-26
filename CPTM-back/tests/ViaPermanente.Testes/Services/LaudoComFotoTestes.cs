using ViaPermanente.Dados.Laudos;
using ViaPermanente.Dominio.Laudos.Models;

namespace ViaPermanente.Testes.Services;

/// <summary>O laudo com foto (Victor, 2026-10-25: "a imagem tem que aparecer no laudo"): o PDF desenha a imagem.</summary>
public class LaudoComFotoTestes
{
    // Um PNG de 1×1 pixel.
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static byte[] Montar(FotoPdf foto) => new DocumentoPdfRepository().Montar(
        new DocumentoPdf("Laudo da troca", "LD-0001-v4", "DM-01 · km 213,100", DateTime.Now, [new SecaoPdf("Fotos", [], null, [foto])]));

    private static bool TemImagem(byte[] pdf) => System.Text.RegularExpressions.Regex.IsMatch(System.Text.Encoding.Latin1.GetString(pdf), @"/Subtype\s*/Image");

    [Fact]
    public void O_pdf_desenha_a_foto_e_sem_o_arquivo_escreve_o_motivo()
    {
        var comFoto = Montar(new FotoPdf("A peça nova no lugar", "trocas/1/a.png", "image/png", Png));
        var semArquivo = Montar(new FotoPdf("A peça nova no lugar", "trocas/1/a.png", "image/png", null));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(comFoto, 0, 4));
        Assert.True(TemImagem(comFoto));
        Assert.False(TemImagem(semArquivo));
    }
}
