using ViaPermanente.Dominio.Via;

namespace ViaPermanente.Testes.Services;

/// <summary>
/// O km de cada foto da passagem, só com o GPS do veículo (Victor, 2026-11-17): o primeiro ponto vale o km da estação A, o último o da B,
/// e os do meio ficam na proporção do caminho que o GPS andou. Os pontos usam latitudes que mudam sobre o mesmo meridiano, onde a
/// distância é exata (0,001 grau de latitude são cerca de 111 m).
/// </summary>
public class KmPeloGpsTestes
{
    private static readonly DateTime Inicio = new(2026, 11, 16, 22, 0, 0);

    /// <summary>Um ponto com GPS, a tantos minutos do começo, no meridiano, a tantos milésimos de grau de latitude.</summary>
    private static PontoGps Ponto(int minutos, double? milesimosDeGrau) => new(Inicio.AddMinutes(minutos),
        milesimosDeGrau is null ? null : -23.5 - milesimosDeGrau.Value / 1000, milesimosDeGrau is null ? null : -46.7);

    [Fact]
    public void O_primeiro_ponto_vale_o_km_da_estacao_A_e_o_ultimo_o_da_B()
    {
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(0, 0), Ponto(5, 1), Ponto(18, 3)]);

        Assert.Equal(251m, km[0]);
        Assert.Equal(260m, km[2]);
    }

    [Fact]
    public void O_ponto_do_meio_fica_na_proporcao_do_caminho_que_o_GPS_andou()
    {
        // O GPS andou 1 parte de 3 até o ponto do meio: 251 + 1/3 × 9 = 254.
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(0, 0), Ponto(5, 1), Ponto(18, 3)]);

        Assert.Equal(254m, km[1]);
    }

    [Fact]
    public void Veiculo_parado_nao_muda_o_km()
    {
        // Parou 10 minutos no mesmo lugar: a hora passou, o GPS não andou, então o km é o mesmo. Pela hora, ele andaria.
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(0, 0), Ponto(10, 0), Ponto(20, 3)]);

        Assert.Equal(251m, km[1]);
    }

    [Fact]
    public void A_ordem_vem_da_hora_e_o_resultado_volta_na_ordem_da_lista()
    {
        // A lista vem embaralhada: o último ponto vem primeiro. O km de cada um volta na posição dele.
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(18, 3), Ponto(0, 0), Ponto(5, 1)]);

        Assert.Equal(260m, km[0]);
        Assert.Equal(251m, km[1]);
        Assert.Equal(254m, km[2]);
    }

    [Fact]
    public void Ponto_sem_GPS_fica_pela_hora_entre_os_vizinhos_que_tem()
    {
        // O do meio perdeu o sinal: fica na metade do tempo entre o 251 e o 260.
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(0, 0), Ponto(10, null), Ponto(20, 3)]);

        Assert.Equal(255.5m, km[1]);
    }

    [Fact]
    public void Ponto_sem_GPS_fora_do_trecho_andado_fica_sem_km()
    {
        // Antes do primeiro ponto com GPS não há como saber: não chuta.
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(-5, null), Ponto(0, 0), Ponto(18, 3)]);

        Assert.Null(km[0]);
    }

    [Fact]
    public void Com_menos_de_dois_pontos_com_GPS_nao_calcula_nada()
    {
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(0, 0), Ponto(5, null)]);

        Assert.All(km, k => Assert.Null(k));
    }

    [Fact]
    public void Sem_caminho_andado_nao_calcula_nada()
    {
        // Dois pontos no mesmo lugar: a proporção seria 0 ÷ 0.
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(0, 0), Ponto(5, 0)]);

        Assert.All(km, k => Assert.Null(k));
    }

    [Fact]
    public void O_km_sai_com_tres_casas_decimais_como_a_coluna_do_banco()
    {
        // 1 parte de 7: 251 + 1/7 × 9 = 252,2857142… → 252,286.
        var km = KmPeloGps.Calcular(251m, 260m, [Ponto(0, 0), Ponto(5, 1), Ponto(18, 7)]);

        Assert.Equal(252.286m, km[1]);
    }

    [Fact]
    public void Lista_vazia_devolve_lista_vazia()
    {
        Assert.Empty(KmPeloGps.Calcular(251m, 260m, []));
    }
}
