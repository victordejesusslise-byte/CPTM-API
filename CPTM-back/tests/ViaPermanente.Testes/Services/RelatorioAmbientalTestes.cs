using ViaPermanente.Dominio.Ambiental;

namespace ViaPermanente.Testes.Services;

/// <summary>Os casos da Tarefa 18 do plano (2,80 m = 0,11424 m³, seção provisória).</summary>
public class RelatorioAmbientalTestes
{
    private static readonly DateOnly Outubro = new(2026, 10, 1);

    [Fact]
    public void Soma_m3_por_especie()
    {
        var item = Assert.Single(RelatorioAmbientalCalculo.Itens(Outubro,
            [new(1, 0.11424m, new DateTime(2026, 10, 2), null), new(1, 0.11424m, new DateTime(2026, 9, 20), null)]));
        Assert.Equal(0.22848m, item.VolumeM3Estoque);
        Assert.Equal(0.11424m, item.VolumeM3Entrada);
    }

    [Fact]
    public void Reprovadas_e_retiradas_ficam_fora()
    {
        var item = Assert.Single(RelatorioAmbientalCalculo.Itens(Outubro,
            [new(1, 0.11424m, null, null),                                                 // nunca entrou (reprovada)
             new(1, 0.11424m, new DateTime(2026, 9, 1), new DateTime(2026, 10, 15)),       // saiu para a troca em outubro
             new(1, 0.11424m, new DateTime(2026, 9, 1), null)]));
        Assert.Equal(0.11424m, item.VolumeM3Estoque);
        Assert.Equal(0.11424m, item.VolumeM3Saida);
    }

    [Fact]
    public void Idade_media_em_dias()
        => Assert.Equal(36, Assert.Single(RelatorioAmbientalCalculo.Itens(Outubro,
            [new(1, 0.1m, new DateTime(2026, 10, 1), null), new(1, 0.1m, new DateTime(2026, 9, 20), null)])).IdadeMediaDias); // 31 e 42 dias até 1/11
}