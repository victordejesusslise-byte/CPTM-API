using System.Runtime.InteropServices;
using Tesseract;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dados.Chegadas;

/// <summary>
/// O Tesseract (pacote Tesseract 5.2.0, Apache 2.0) com os modelos da pasta tessdata (por.traineddata é o português padrão, versão 1).
/// Lê a mesma foto de quatro jeitos, porque a placa real é uma tabela presa numa chapa perfurada e nenhum jeito sozinho pega tudo:
/// a foto inteira como veio; a tabela célula por célula (com e sem a erosão que solta as células dos rebites), mais as faixas pretas
/// entre as células (o fornecedor, em letra branca); e a foto ampliada
/// procurando texto solto, com os pedaços da mesma faixa juntados. Vêm da mais certeira para a menos (a tabela primeiro); o serviço
/// junta os campos das quatro.
/// </summary>
public class TesseractOcrRepository(string pastaTessdata) : IOcrRepository
{
    public IReadOnlyList<ResultadoOcr> Ler(byte[] imagem, string modeloBase)
    {
        using var motor = new TesseractEngine(pastaTessdata, modeloBase, EngineMode.Default);
        using var comoVeio = Pix.LoadFromMemory(imagem);
        // A foto do celular vem deitada, com a orientação no EXIF: em pé antes de ler (OrientacaoDaFoto).
        using var original = OrientacaoDaFoto.Endireitar(comoVeio, OrientacaoDaFoto.Ler(imagem));
        var inteira = FotoInteira(motor, original, PageSegMode.Auto, 1, emFaixas: false);
        // As outras leituras precisam da foto em tons de cinza (8 bits); foto em outro formato fica só com a leitura inteira.
        if (original.Depth is not (8 or 32))
            return [inteira];
        using var cinza = original.Depth == 32 ? original.ConvertRGBToGray() : original.Clone();
        using var dobro = cinza.Scale(2f, 2f);
        using var invertido = dobro.Invert();
        var pixels = Pixels(cinza);
        var leituras = new[] { false, true }.Select(erodir => Tabela(motor, dobro, invertido, CelulasDaPlaca.Achar(pixels, cinza.Width, cinza.Height, erodir))).ToList();
        leituras.Add(inteira);
        leituras.Add(FotoInteira(motor, dobro, PageSegMode.SparseText, 2, emFaixas: true));
        return leituras;
    }

    /// <summary>A foto toda de uma vez. Na foto ampliada (escala 2), as caixas voltam para o tamanho da foto enviada.</summary>
    private static ResultadoOcr FotoInteira(TesseractEngine motor, Pix pix, PageSegMode modo, int escala, bool emFaixas)
    {
        using var pagina = motor.Process(pix, modo);
        var linhas = new List<LinhaOcr>();
        using (var iterador = pagina.GetIterator())
        {
            iterador.Begin();
            do
            {
                var texto = iterador.GetText(PageIteratorLevel.TextLine)?.Trim();
                if (string.IsNullOrEmpty(texto)) continue;
                iterador.TryGetBoundingBox(PageIteratorLevel.TextLine, out var caixa);
                linhas.Add(new LinhaOcr(texto, (decimal)iterador.GetConfidence(PageIteratorLevel.TextLine),
                    caixa.X1 / escala, caixa.Y1 / escala, caixa.Width / escala, caixa.Height / escala));
            } while (iterador.Next(PageIteratorLevel.TextLine));
        }
        if (!emFaixas)
            return new ResultadoOcr(pagina.GetText(), (decimal)pagina.GetMeanConfidence() * 100, linhas);
        var faixas = PlacaDoFeixe.EmFaixas(linhas);
        return new ResultadoOcr(Texto(faixas), (decimal)pagina.GetMeanConfidence() * 100, faixas);
    }

    /// <summary>
    /// Cada célula lida sozinha (bloco de texto), na foto ampliada; as células da mesma faixa viram "rótulo: valor".
    /// Uma faixa preta entre células, mais alta que meia célula, tem letra branca (na placa real, o fornecedor): é lida na foto invertida.
    /// </summary>
    private static ResultadoOcr Tabela(TesseractEngine motor, Pix dobro, Pix invertido, IReadOnlyList<CelulaDaPlaca> celulas)
    {
        var lidas = new List<LinhaOcr>();
        foreach (var c in celulas)
            if (Bloco(motor, dobro, c) is { } lida) lidas.Add(lida);
        var ordem = celulas.OrderBy(c => c.Y).ToList();
        if (ordem.Count > 1)
        {
            int x0 = ordem.Min(c => c.X), x1 = ordem.Max(c => c.X + c.Largura);
            var meiaCelula = ordem.Select(c => c.Altura).Order().ElementAt(ordem.Count / 2) / 2;
            var fundo = ordem[0].Y + ordem[0].Altura;
            foreach (var c in ordem.Skip(1))
            {
                if (c.Y - fundo >= meiaCelula && Bloco(motor, invertido, new CelulaDaPlaca(x0, fundo, x1 - x0, c.Y - fundo)) is { } faixa)
                    lidas.Add(faixa);
                fundo = Math.Max(fundo, c.Y + c.Altura);
            }
        }
        var faixas = PlacaDoFeixe.EmFaixas(lidas);
        return new ResultadoOcr(Texto(faixas), lidas.Count > 0 ? lidas.Average(l => l.Confianca) : 0, faixas);
    }

    /// <summary>Um retângulo lido como bloco de texto. As linhas de dentro ficam separadas por quebra ("CCA" e "/ Granoski"): o leitor da placa divide por elas.</summary>
    private static LinhaOcr? Bloco(TesseractEngine motor, Pix dobro, CelulaDaPlaca c)
    {
        using var pagina = motor.Process(dobro, new Rect(c.X * 2, c.Y * 2, c.Largura * 2, c.Altura * 2), PageSegMode.SingleBlock);
        var texto = string.Join("\n", pagina.GetText().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
        return texto.Length > 0 ? new LinhaOcr(texto, (decimal)pagina.GetMeanConfidence() * 100, c.X, c.Y, c.Largura, c.Altura) : null;
    }

    private static string Texto(IEnumerable<LinhaOcr> linhas) => string.Join("\n", linhas.Select(l => l.Texto.Replace('\n', ' ')));

    /// <summary>Os pixels da foto cinza (8 bits): o Leptonica guarda 4 pixels por palavra de 32 bits, o primeiro no byte mais alto.</summary>
    private static byte[] Pixels(Pix cinza)
    {
        var dados = cinza.GetData();
        var palavras = new int[dados.WordsPerLine * cinza.Height];
        Marshal.Copy(dados.Data, palavras, 0, palavras.Length);
        var pixels = new byte[cinza.Width * cinza.Height];
        for (var y = 0; y < cinza.Height; y++)
            for (var x = 0; x < cinza.Width; x++)
                pixels[y * cinza.Width + x] = (byte)((uint)palavras[y * dados.WordsPerLine + (x >> 2)] >> (24 - 8 * (x & 3)));
        return pixels;
    }
}
