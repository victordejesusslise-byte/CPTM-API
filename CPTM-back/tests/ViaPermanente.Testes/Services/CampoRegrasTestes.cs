using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Testes.Services;

public class CampoRegrasTestes
{
    [Theory]
    [InlineData(EstadoConservacao.Bom, RachaduraInspecao.Leve, AtaqueBiologicoInspecao.Nenhum, 24, ResultadoInspecao.Aprovada)]
    [InlineData(EstadoConservacao.Ruim, RachaduraInspecao.Nenhuma, AtaqueBiologicoInspecao.Nenhum, 24, ResultadoInspecao.Reprovada)]
    [InlineData(EstadoConservacao.Bom, RachaduraInspecao.Grave, AtaqueBiologicoInspecao.Nenhum, 24, ResultadoInspecao.Reprovada)]
    [InlineData(EstadoConservacao.Bom, RachaduraInspecao.Nenhuma, AtaqueBiologicoInspecao.Avancado, 24, ResultadoInspecao.Reprovada)]
    [InlineData(EstadoConservacao.Regular, RachaduraInspecao.Media, AtaqueBiologicoInspecao.Inicio, 6, ResultadoInspecao.Reprovada)]
    [InlineData(EstadoConservacao.Regular, RachaduraInspecao.Media, AtaqueBiologicoInspecao.Inicio, 7, ResultadoInspecao.Aprovada)]
    public void Checklist_v2_reprova_como_o_prototipo(EstadoConservacao c, RachaduraInspecao r, AtaqueBiologicoInspecao a, int vida, ResultadoInspecao esperado)
        => Assert.Equal(esperado, ChecklistInspecao.Resultado(c, r, a, vida));

    [Theory]
    [InlineData(5, FaixaPosicao.Ok)]
    [InlineData(20, FaixaPosicao.Ok)]
    [InlineData(-25, FaixaPosicao.Atencao)]
    [InlineData(40, FaixaPosicao.Atencao)]
    [InlineData(-41, FaixaPosicao.Fora)]
    public void Faixa_pelo_desvio_medido(int desvio, FaixaPosicao esperada)
        => Assert.Equal(esperada, PosicaoNaVia.Faixa(desvio, 20, 40));

    [Fact]
    public void Um_alerta_por_leitura_na_ordem_do_prototipo()
    {
        Assert.Equal(TipoAlerta.TagMuda, PosicaoNaVia.AlertaDaLeitura(false, true, 80, 40));
        Assert.Equal(TipoAlerta.Rachadura, PosicaoNaVia.AlertaDaLeitura(true, true, 80, 40));
        Assert.Equal(TipoAlerta.Posicao, PosicaoNaVia.AlertaDaLeitura(true, false, -45, 40));
        Assert.Null(PosicaoNaVia.AlertaDaLeitura(true, false, 30, 40));
    }

    [Fact]
    public void Separacao_exige_o_mesmo_comprimento() => Assert.False(CompatibilidadeTroca.Serve(2, 1, idTamanhoQueSai: 1, classeExigida: 1));

    [Fact]
    public void Segunda_classe_nao_serve_em_trecho_de_primeira() => Assert.False(CompatibilidadeTroca.Serve(1, 2, idTamanhoQueSai: 1, classeExigida: 1));

    [Fact]
    public void Primeira_classe_serve_em_trecho_de_segunda() => Assert.True(CompatibilidadeTroca.Serve(1, 1, idTamanhoQueSai: 1, classeExigida: 2));
}