using Tesseract;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dados.Chegadas;

/// <summary>O Tesseract (pacote Tesseract 5.2.0, Apache 2.0) com os modelos da pasta tessdata (por.traineddata é o português padrão, versão 1).</summary>
public class TesseractOcrRepository(string pastaTessdata) : IOcrRepository
{
    public ResultadoOcr Ler(byte[] imagem, string modeloBase)
    {
        using var motor = new TesseractEngine(pastaTessdata, modeloBase, EngineMode.Default);
        using var pix = Pix.LoadFromMemory(imagem);
        using var pagina = motor.Process(pix);
        var linhas = new List<LinhaOcr>();
        using (var iterador = pagina.GetIterator())
        {
            iterador.Begin();
            do
            {
                var texto = iterador.GetText(PageIteratorLevel.TextLine)?.Trim();
                if (string.IsNullOrEmpty(texto)) continue;
                iterador.TryGetBoundingBox(PageIteratorLevel.TextLine, out var caixa);
                linhas.Add(new LinhaOcr(texto, (decimal)iterador.GetConfidence(PageIteratorLevel.TextLine), caixa.X1, caixa.Y1, caixa.Width, caixa.Height));
            } while (iterador.Next(PageIteratorLevel.TextLine));
        }
        return new ResultadoOcr(pagina.GetText(), (decimal)pagina.GetMeanConfidence() * 100, linhas);
    }
}