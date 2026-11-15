using System.Diagnostics;
using Tesseract;
using ViaPermanente.Dados.Chegadas;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Testes.Helpers;

/// <summary>
/// O operador tira a foto, espera e confere: a leitura tem de ser rápida (Victor, 2026-11-15: no máximo uns 45 s no celular). A foto de
/// 12 MP do celular, ampliada para a leitura de texto solto, levava de 2 a 5 minutos e o app ficava carregando; agora a foto é reduzida
/// e a leitura tem prazo.
/// </summary>
public class PrazoDoOcrTestes
{
    private static byte[] Foto(string nome) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Recursos", nome));

    private static string Tessdata()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (Directory.Exists(Path.Combine(d.FullName, "tessdata"))) return Path.Combine(d.FullName, "tessdata");
        throw new DirectoryNotFoundException("tessdata");
    }

    [Fact]
    public void Foto_grande_do_celular_fica_com_o_lado_maior_de_1600()
    {
        using var grande = Pix.Create(3060, 4080, 32);
        using var menor = TesseractOcrRepository.Reduzir(grande);
        Assert.Equal((1200, 1600), (menor.Width, menor.Height));
    }

    [Fact]
    public void A_foto_menor_da_leitura_tem_o_lado_maior_de_800()
    {
        using var grande = Pix.Create(3060, 4080, 32);
        using var menor = TesseractOcrRepository.Reduzir(grande, TesseractOcrRepository.LadoMenor);
        Assert.Equal((600, 800), (menor.Width, menor.Height));
    }

    /// <summary>As caixas dos campos voltam para a medida da foto enviada (em pé), como eram antes da redução.</summary>
    [Fact]
    public void As_caixas_voltam_para_a_medida_da_foto_enviada()
    {
        var lida = new ResultadoOcr("AR00224", 90, [new LinhaOcr("AR00224", 90, 10, 20, 30, 40)]);
        var linha = TesseractOcrRepository.NaFotoEnviada(lida, 2.5).Linhas.Single();
        Assert.Equal((25, 50, 75, 100), (linha.X, linha.Y, linha.Largura, linha.Altura));
    }

    [Fact]
    public void Foto_pequena_fica_como_esta()
    {
        using var pequena = Pix.Create(960, 1280, 32);
        using var igual = TesseractOcrRepository.Reduzir(pequena);
        Assert.Equal((960, 1280), (igual.Width, igual.Height));
    }

    [Fact]
    public void Passado_o_prazo_devolve_o_que_ja_leu_sem_esperar_o_resto()
    {
        var ocr = new TesseractOcrRepository(Tessdata(), TimeSpan.FromMilliseconds(1));
        var relogio = Stopwatch.StartNew();
        var leituras = ocr.Ler(Foto("placa-celular-deitada.jpg"), "por");
        Assert.True(relogio.Elapsed < TimeSpan.FromSeconds(1), $"levou {relogio.Elapsed.TotalSeconds:F1} s");
        Assert.NotEmpty(leituras);
    }

    [Fact]
    public void Foto_que_nao_e_imagem_da_o_erro_do_tesseract()
        => Assert.ThrowsAny<IOException>(() => new TesseractOcrRepository(Tessdata()).Ler([1, 2, 3], "por"));
}
