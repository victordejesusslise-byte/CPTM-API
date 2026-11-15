using System.Runtime.ExceptionServices;
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
/// procurando texto solto, com os pedaços da mesma faixa juntados. Os quatro jeitos rodam em duas medidas da foto (1600 e 800 px);
/// vêm da mais certeira para a menos (a tabela primeiro), e o serviço junta os campos de todas. A leitura tem prazo: passado o prazo,
/// vale o que já foi lido.
/// </summary>
public class TesseractOcrRepository(string pastaTessdata, TimeSpan? prazo = null) : IOcrRepository
{
    /// <summary>O lado maior da foto lida, em px: a foto de 12 MP do celular (4080 px) ampliada levava de 2 a 5 minutos (2026-11-15).</summary>
    public const int LadoMaior = 1600;

    /// <summary>O lado maior da segunda medida, em px: na foto menor somem as listras da placa fotografada numa tela (moiré, 2026-11-15).</summary>
    public const int LadoMenor = 800;

    /// <summary>Quanto a leitura espera o Tesseract: o operador tira a foto, espera e confere (Victor, 2026-11-15: no máximo uns 45 s no app).</summary>
    public static readonly TimeSpan PrazoPadrao = TimeSpan.FromSeconds(20);

    public IReadOnlyList<ResultadoOcr> Ler(byte[] imagem, string modeloBase)
    {
        // As leituras rodam numa linha de execução própria; a resposta espera até o prazo e leva as que já terminaram, na ordem de cada uma.
        var prontas = new List<(int Ordem, ResultadoOcr Lida)>();
        // Sem using: a leitura que passou do prazo ainda consulta o cancelamento depois que a resposta saiu.
        var parar = new CancellationTokenSource();
        var trabalho = Task.Factory.StartNew(() => LerTudo(imagem, modeloBase, prontas, parar.Token), TaskCreationOptions.LongRunning);
        try { trabalho.Wait(prazo ?? PrazoPadrao); }
        // Erro antes do prazo (foto que não é imagem): sobe o erro do Tesseract, não o AggregateException da Task.
        catch (AggregateException erro) when (erro.InnerException is not null) { ExceptionDispatchInfo.Capture(erro.InnerException).Throw(); }
        // Passou do prazo: a leitura que está rodando termina sozinha e as próximas não começam.
        parar.Cancel();
        lock (prontas)
            return prontas.Count > 0 ? [.. prontas.OrderBy(p => p.Ordem).Select(p => p.Lida)] : [new ResultadoOcr("", 0, [])];
    }

    /// <summary>A foto com o lado maior de até "ladoMaior" px (nunca aumenta). Foto menor volta como está.</summary>
    public static Pix Reduzir(Pix foto, int ladoMaior = LadoMaior)
    {
        var escala = (float)ladoMaior / Math.Max(foto.Width, foto.Height);
        return escala < 1 ? foto.Scale(escala, escala) : foto.Clone();
    }

    /// <summary>As caixas de uma leitura na medida da foto enviada (em pé), como eram antes da redução: a leitura foi feita na foto menor.</summary>
    public static ResultadoOcr NaFotoEnviada(ResultadoOcr lida, double fator)
    {
        int Medida(int valor) => (int)Math.Round(valor * fator);
        return fator == 1 ? lida : lida with
        {
            Linhas = [.. lida.Linhas.Select(l => l with { X = Medida(l.X), Y = Medida(l.Y), Largura = Medida(l.Largura), Altura = Medida(l.Altura) })]
        };
    }

    /// <summary>
    /// As leituras nas duas medidas. Cada uma tem a sua ordem no resultado (a mais certeira antes, para o desempate do serviço), mas
    /// rodam da mais rápida para a mais lenta, para o prazo cortar só as lentas; cada uma entra em "prontas" ao terminar, e o
    /// cancelamento para antes da próxima.
    /// </summary>
    private void LerTudo(byte[] imagem, string modeloBase, List<(int Ordem, ResultadoOcr Lida)> prontas, CancellationToken parar)
    {
        using var motor = new TesseractEngine(pastaTessdata, modeloBase, EngineMode.Default);
        using var comoVeio = Pix.LoadFromMemory(imagem);
        // A foto do celular vem deitada, com a orientação no EXIF: em pé antes de ler (OrientacaoDaFoto).
        using var emPe = OrientacaoDaFoto.Endireitar(comoVeio, OrientacaoDaFoto.Ler(imagem));
        void Guardar(int ordem, ResultadoOcr lida, Pix foto) { lock (prontas) prontas.Add((ordem, NaFotoEnviada(lida, (double)emPe.Width / foto.Width))); }
        // Duas medidas: até 1600 px (os detalhes da placa) e até 800 px (sem as listras de tela).
        using var grande = Reduzir(emPe);
        using var pequena = Reduzir(emPe, LadoMenor);
        // As outras leituras precisam da foto em tons de cinza (8 bits); foto em outro formato fica só com a leitura inteira.
        if (grande.Depth is not (8 or 32))
        {
            Guardar(0, FotoInteira(motor, grande, PageSegMode.Auto, 1, emFaixas: false), grande);
            return;
        }
        using var g = new Medida(grande);
        using var p = new Medida(pequena);
        // A ordem no resultado: a tabela (sem e com erosão) e a foto inteira na grande, as mesmas na pequena, o texto solto na pequena
        // e, por último, o texto solto na grande. Foto que já é pequena (até 800 px) é lida uma vez só.
        var leituras = new List<(int Ordem, Func<ResultadoOcr> Ler, Pix Foto)>
        {
            (0, () => Tabela(motor, g.Dobro, g.Invertido, g.Celulas(erodir: false)), grande),
            (1, () => Tabela(motor, g.Dobro, g.Invertido, g.Celulas(erodir: true)), grande),
            (2, () => FotoInteira(motor, grande, PageSegMode.Auto, 1, emFaixas: false), grande),
            (7, () => FotoInteira(motor, g.Dobro, PageSegMode.SparseText, 2, emFaixas: true), grande),
        };
        if (pequena.Width != grande.Width)
            leituras.InsertRange(0, [
                (3, () => Tabela(motor, p.Dobro, p.Invertido, p.Celulas(erodir: false)), pequena),
                (4, () => Tabela(motor, p.Dobro, p.Invertido, p.Celulas(erodir: true)), pequena),
                (5, () => FotoInteira(motor, pequena, PageSegMode.Auto, 1, emFaixas: false), pequena),
                (6, () => FotoInteira(motor, p.Dobro, PageSegMode.SparseText, 2, emFaixas: true), pequena),
            ]);
        // Rodam na ordem da lista: a pequena primeiro (rápida, sem as listras de tela), depois a grande; o texto solto na grande, que
        // na foto de tela leva minutos, é o último.
        foreach (var (ordem, ler, foto) in leituras)
        {
            if (parar.IsCancellationRequested) return;
            Guardar(ordem, ler(), foto);
        }
    }

    /// <summary>Uma medida da foto pronta para as leituras: em cinza (8 bits), ampliada (dobro), invertida e com os pixels para achar as células.</summary>
    private sealed class Medida : IDisposable
    {
        public Pix Cinza { get; }
        public Pix Dobro { get; }
        public Pix Invertido { get; }
        private readonly byte[] pixels;

        public Medida(Pix foto)
        {
            Cinza = foto.Depth == 32 ? foto.ConvertRGBToGray() : foto.Clone();
            Dobro = Cinza.Scale(2f, 2f);
            Invertido = Dobro.Invert();
            pixels = Pixels(Cinza);
        }

        /// <summary>As células da tabela da placa nesta medida (com ou sem a erosão que solta as células dos rebites).</summary>
        public IReadOnlyList<CelulaDaPlaca> Celulas(bool erodir) => CelulasDaPlaca.Achar(pixels, Cinza.Width, Cinza.Height, erodir);

        public void Dispose() { Invertido.Dispose(); Dobro.Dispose(); Cinza.Dispose(); }
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
