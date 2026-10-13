using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;

namespace ViaPermanente.Testes.Services;

public class TrechoMapaTestes
{
    private static PecaNoTrechoDto Peca(decimal km, string? nota) => new(1, "DM", null, null, km, nota);

    [Fact]
    public void Corta_o_trecho_em_faixas_de_100_m_e_conta_cada_nota()
    {
        var faixas = TrechoMapaService.Faixas(212.000m, 212.300m, 100,
            [Peca(212.010m, "Bom"), Peca(212.090m, "Ruim"), Peca(212.150m, "Crítico"), Peca(212.300m, null)]);

        Assert.Equal(3, faixas.Count);
        Assert.Equal((212.000m, 212.100m), (faixas[0].KmInicio, faixas[0].KmFim));
        Assert.Equal((1, 1, 0), (faixas[0].Bom, faixas[0].Ruim, faixas[0].Critico));
        Assert.Equal(1, faixas[1].Critico);
        Assert.Equal(1, faixas[2].SemNota);   // a peça no km do fim entra na última faixa
    }

    [Fact]
    public void A_peca_antes_da_estacao_estica_o_comeco()
    {
        var faixas = TrechoMapaService.Faixas(212.000m, 212.100m, 100, [Peca(211.950m, "Bom")]);

        Assert.Equal(211.900m, faixas[0].KmInicio);
        Assert.Equal(1, faixas[0].Bom);
        Assert.Equal(2, faixas.Count);
    }

    [Fact]
    public void Trecho_sem_peca_ainda_mostra_as_faixas()
        => Assert.Equal(10, TrechoMapaService.Faixas(5.000m, 6.000m, 100, []).Count);
}
