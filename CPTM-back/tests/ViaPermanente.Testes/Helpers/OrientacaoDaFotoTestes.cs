using Tesseract;
using ViaPermanente.Dados.Chegadas;

namespace ViaPermanente.Testes.Helpers;

/// <summary>A foto do celular vem deitada, com a orientação no EXIF (2026-10-27: o OCR lia a placa de lado e não achava nada).</summary>
public class OrientacaoDaFotoTestes
{
    private static byte[] Foto(string nome) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Recursos", nome));

    [Fact]
    public void A_foto_do_celular_vem_deitada()
        => Assert.Equal(6, OrientacaoDaFoto.Ler(Foto("placa-celular-deitada.jpg")));

    [Fact]
    public void Png_e_lixo_ficam_em_pe()
    {
        Assert.Equal(1, OrientacaoDaFoto.Ler(Foto("placa-real.png")));
        Assert.Equal(1, OrientacaoDaFoto.Ler([]));
        Assert.Equal(1, OrientacaoDaFoto.Ler([0xFF, 0xD8, 0xFF, 0xE1, 0x00]));
    }

    /// <summary>EXIF "MM" (Motorola, o primeiro byte é o mais alto), com a orientação 8 no segundo campo.</summary>
    [Fact]
    public void Le_o_exif_motorola()
    {
        byte[] tiff = [(byte)'M', (byte)'M', 0, 42, 0, 0, 0, 8, 0, 2,
            0x01, 0x0F, 0, 2, 0, 0, 0, 1, 0, 0, 0, 0,
            0x01, 0x12, 0, 3, 0, 0, 0, 1, 0, 8, 0, 0];
        var tamanho = 2 + 6 + tiff.Length;
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE1, (byte)(tamanho >> 8), (byte)tamanho, (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0, .. tiff, 0xFF, 0xDA];
        Assert.Equal(8, OrientacaoDaFoto.Ler(jpeg));
    }

    [Fact]
    public void Endireita_a_foto_deitada()
    {
        var foto = Foto("placa-celular-deitada.jpg");
        using var deitada = Pix.LoadFromMemory(foto);
        using var emPe = OrientacaoDaFoto.Endireitar(deitada, OrientacaoDaFoto.Ler(foto));
        Assert.True(deitada.Width > deitada.Height);
        Assert.Equal((deitada.Height, deitada.Width), (emPe.Width, emPe.Height));
    }
}
