using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Services;

public class RetencaoLeituraViaTestes
{
    private static readonly DateTime Hoje = new(2026, 10, 11); // domingo

    [Fact]
    public void Semana_comeca_na_segunda() => Assert.Equal(new DateTime(2026, 10, 5), RetencaoLeituraVia.Segunda(Hoje));

    [Fact]
    public void Abre_as_semanas_que_faltam_ate_quatro_a_frente()
        => Assert.Equal([new DateTime(2026, 10, 26), new DateTime(2026, 11, 2)],
            RetencaoLeituraVia.FronteirasNovas([new DateTime(2026, 10, 12), new DateTime(2026, 10, 19)], Hoje));

    [Fact]
    public void Descarta_so_o_que_passou_de_90_dias_e_deixa_uma_fronteira()
    {
        // limite: 13/07/2026
        Assert.Equal([new DateTime(2026, 6, 29), new DateTime(2026, 7, 6), new DateTime(2026, 7, 13)],
            RetencaoLeituraVia.FronteirasParaDescartar([new DateTime(2026, 6, 29), new DateTime(2026, 7, 6), new DateTime(2026, 7, 13), new DateTime(2026, 7, 20)], Hoje));
        Assert.Empty(RetencaoLeituraVia.FronteirasParaDescartar([new DateTime(2026, 6, 29)], Hoje));
    }
}