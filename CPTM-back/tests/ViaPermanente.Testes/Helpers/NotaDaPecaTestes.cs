using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Testes.Helpers;

public class NotaDaPecaTestes
{
    private static readonly DateTime Ontem = new(2026, 10, 12, 8, 0, 0), Hoje = new(2026, 10, 13, 8, 0, 0);

    private static SinaisDaPeca Sinais(
        DateTime? qualidade = null, ResultadoVerificacao resultado = ResultadoVerificacao.Aprovada,
        RachaduraVerificacao rachadura = RachaduraVerificacao.Nenhuma, AtaqueBiologicoVerificacao ataque = AtaqueBiologicoVerificacao.Nenhum,
        DateTime? inspecao = null, EstadoConservacao estado = EstadoConservacao.Bom, ResultadoInspecao resultadoInspecao = ResultadoInspecao.Aprovada,
        DateTime? alerta = null, TipoAlerta tipo = TipoAlerta.Posicao, StatusAlerta status = StatusAlerta.Aberto)
        => new(qualidade, qualidade is null ? null : resultado, qualidade is null ? null : rachadura, qualidade is null ? null : ataque,
            inspecao, inspecao is null ? null : estado, inspecao is null ? null : resultadoInspecao,
            alerta, alerta is null ? null : tipo, alerta is null ? null : status, null, null);

    [Fact]
    public void Sem_avaliacao_nao_tem_nota()
    {
        Assert.Null(NotaDaPeca.Calcular(null));
        Assert.Null(NotaDaPeca.Calcular(Sinais()));
    }

    [Theory]
    [InlineData(ResultadoVerificacao.Aprovada, RachaduraVerificacao.Nenhuma, AtaqueBiologicoVerificacao.Nenhum, "Bom")]
    [InlineData(ResultadoVerificacao.Aprovada, RachaduraVerificacao.Leve, AtaqueBiologicoVerificacao.Nenhum, "Regular")]
    [InlineData(ResultadoVerificacao.Aprovada, RachaduraVerificacao.Nenhuma, AtaqueBiologicoVerificacao.Suspeito, "Regular")]
    [InlineData(ResultadoVerificacao.Reprovada, RachaduraVerificacao.Grave, AtaqueBiologicoVerificacao.Nenhum, "Crítico")]
    public void A_verificacao_de_qualidade_da_a_nota(ResultadoVerificacao resultado, RachaduraVerificacao rachadura, AtaqueBiologicoVerificacao ataque, string nota)
        => Assert.Equal(nota, NotaDaPeca.Calcular(Sinais(Ontem, resultado, rachadura, ataque)));

    [Theory]
    [InlineData(EstadoConservacao.Bom, ResultadoInspecao.Aprovada, "Bom")]
    [InlineData(EstadoConservacao.Regular, ResultadoInspecao.Aprovada, "Regular")]
    [InlineData(EstadoConservacao.Ruim, ResultadoInspecao.Aprovada, "Ruim")]
    [InlineData(EstadoConservacao.Regular, ResultadoInspecao.Reprovada, "Crítico")]
    public void A_inspecao_mais_nova_vale_mais_que_a_verificacao(EstadoConservacao estado, ResultadoInspecao resultado, string nota)
        => Assert.Equal(nota, NotaDaPeca.Calcular(Sinais(Ontem, inspecao: Hoje, estado: estado, resultadoInspecao: resultado)));

    [Theory]
    [InlineData(TipoAlerta.Posicao, StatusAlerta.Aberto, "Ruim")]
    [InlineData(TipoAlerta.TagMuda, StatusAlerta.Confirmado, "Ruim")]
    [InlineData(TipoAlerta.Rachadura, StatusAlerta.Aberto, "Ruim")]
    [InlineData(TipoAlerta.Quebra, StatusAlerta.Confirmado, "Crítico")]
    [InlineData(TipoAlerta.Rachadura, StatusAlerta.Descartado, "Bom")]
    public void O_alerta_aberto_ou_confirmado_derruba_a_nota_e_o_descartado_nao_conta(TipoAlerta tipo, StatusAlerta status, string nota)
        => Assert.Equal(nota, NotaDaPeca.Calcular(Sinais(Ontem, alerta: Hoje, tipo: tipo, status: status)));

    [Fact]
    public void A_inspecao_depois_do_alerta_devolve_a_nota()
        => Assert.Equal("Bom", NotaDaPeca.Calcular(Sinais(alerta: Ontem, inspecao: Hoje)));

    [Theory]
    [InlineData("9-Esmeralda", 213.1, 12, "L09-213100-0012")]
    [InlineData("Linha 11", 7.006, 3, "L11-007006-0003")]
    public void A_posicao_junta_linha_km_e_numero_no_trecho(string linha, double km, int numero, string posicao)
        => Assert.Equal(posicao, NotaDaPeca.Posicao(linha, (decimal)km, numero));

    [Fact]
    public void Fora_da_via_nao_tem_posicao()
        => Assert.Null(NotaDaPeca.Posicao("9-Esmeralda", null, null));
}
