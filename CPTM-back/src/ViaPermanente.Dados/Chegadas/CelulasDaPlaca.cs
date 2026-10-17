namespace ViaPermanente.Dados.Chegadas;

/// <summary>Uma célula da tabela da placa, em pixels da foto.</summary>
public record CelulaDaPlaca(int X, int Y, int Largura, int Altura);

/// <summary>
/// Acha as células da tabela da placa do feixe: retângulos claros (texto escuro em fundo claro) cercados pela moldura preta.
/// A placa real da CPTM é uma tabela de duas colunas (rótulo | valor) presa numa chapa perfurada; o Tesseract lendo a foto inteira
/// se perde nos furos. Lendo célula por célula, cada uma é só texto em fundo liso.
/// Com erodir, o claro encolhe um pouco antes: solta a célula presa ao fundo por um rebite do canto, mas pode partir a célula
/// de texto apertado (a margem some). Por isso o OCR roda dos dois jeitos.
/// </summary>
public static class CelulasDaPlaca
{
    public static IReadOnlyList<CelulaDaPlaca> Achar(byte[] cinza, int largura, int altura, bool erodir)
    {
        var limiar = Otsu(cinza);
        var raio = Math.Max(2, largura / 300);
        var claro = cinza.Select(v => v > limiar).ToArray();
        if (erodir)
            claro = Erodir(claro, largura, altura, raio);
        var visto = new bool[cinza.Length];
        var fila = new int[cinza.Length];
        var celulas = new List<CelulaDaPlaca>();
        var cauda = 0;
        for (var inicio = 0; inicio < cinza.Length; inicio++)
        {
            if (visto[inicio] || !claro[inicio]) continue;
            // Mancha clara ligada (vizinhança de 4): a célula inteira, contornando as letras.
            int cabeca = 0, pixels = 0;
            cauda = 0;
            int x0 = largura, y0 = altura, x1 = -1, y1 = -1;
            fila[cauda++] = inicio;
            visto[inicio] = true;
            while (cabeca < cauda)
            {
                var p = fila[cabeca++];
                int x = p % largura, y = p / largura;
                pixels++;
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
                if (y < y0) y0 = y;
                if (y > y1) y1 = y;
                if (x > 0) Visitar(p - 1);
                if (x < largura - 1) Visitar(p + 1);
                if (y > 0) Visitar(p - largura);
                if (y < altura - 1) Visitar(p + largura);
            }
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            // Célula: larga, de tamanho de linha de tabela, quase cheia (as letras são os buracos). O fundo da foto é maior que tudo isso.
            var ehCelula = w >= largura * 0.08 && h >= altura * 0.02 && w <= largura * 0.9 && h <= altura * 0.25
                && w >= h * 1.2 && pixels >= w * h * 0.5;
            // Sem erosão, a borda da mancha é irregular (cantos arredondados, sombra da moldura): entra um pouco para o OCR não ler a borda.
            var margem = erodir ? 0 : raio;
            if (ehCelula)
                celulas.Add(new CelulaDaPlaca(x0 + margem, y0 + margem, w - 2 * margem, h - 2 * margem));
        }
        return celulas.OrderBy(c => c.Y).ThenBy(c => c.X).ToList();

        void Visitar(int q)
        {
            if (visto[q] || !claro[q]) return;
            visto[q] = true;
            fila[cauda++] = q;
        }
    }

    /// <summary>Um pixel fica claro só se todo o quadrado de lado 2·raio+1 em volta dele é claro.</summary>
    private static bool[] Erodir(bool[] claro, int largura, int altura, int raio)
    {
        var horizontal = new bool[claro.Length];
        for (var y = 0; y < altura; y++)
        {
            var escuros = 0;
            for (var x = -raio; x < largura; x++)
            {
                if (x + raio < largura && !claro[y * largura + x + raio]) escuros++;
                if (x - raio - 1 >= 0 && !claro[y * largura + x - raio - 1]) escuros--;
                if (x >= 0) horizontal[y * largura + x] = escuros == 0 && x >= raio && x < largura - raio;
            }
        }
        var resultado = new bool[claro.Length];
        for (var x = 0; x < largura; x++)
        {
            var escuros = 0;
            for (var y = -raio; y < altura; y++)
            {
                if (y + raio < altura && !horizontal[(y + raio) * largura + x]) escuros++;
                if (y - raio - 1 >= 0 && !horizontal[(y - raio - 1) * largura + x]) escuros--;
                if (y >= 0) resultado[y * largura + x] = escuros == 0 && y >= raio && y < altura - raio;
            }
        }
        return resultado;
    }

    /// <summary>Limiar de Otsu: separa o escuro (moldura, letras) do claro (células, chapa).</summary>
    public static int Otsu(byte[] cinza)
    {
        var histograma = new long[256];
        foreach (var v in cinza) histograma[v]++;
        double total = cinza.Length, soma = 0;
        for (var i = 0; i < 256; i++) soma += i * (double)histograma[i];
        double somaFundo = 0, pesoFundo = 0, melhor = -1;
        var limiar = 127;
        for (var t = 0; t < 256; t++)
        {
            pesoFundo += histograma[t];
            if (pesoFundo == 0) continue;
            var pesoFrente = total - pesoFundo;
            if (pesoFrente == 0) break;
            somaFundo += t * (double)histograma[t];
            var mediaFundo = somaFundo / pesoFundo;
            var mediaFrente = (soma - somaFundo) / pesoFrente;
            var variancia = pesoFundo * pesoFrente * (mediaFundo - mediaFrente) * (mediaFundo - mediaFrente);
            if (variancia > melhor) { melhor = variancia; limiar = t; }
        }
        return limiar;
    }
}
