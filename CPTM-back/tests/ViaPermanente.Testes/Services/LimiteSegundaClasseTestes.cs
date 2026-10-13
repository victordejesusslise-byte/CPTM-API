using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Testes.Services;

/// <summary>Os casos da Tarefa 15 do plano. Volumes por peça do catálogo: 2,80 m = 0,11424; 4,80 m = 0,19584.</summary>
public class LimiteSegundaClasseTestes
{
    private const decimal M3_280 = 0.11424m;
    private const decimal M3_480 = 0.19584m;

    [Theory]
    [InlineData(UnidadeLimite.Pecas)]
    [InlineData(UnidadeLimite.MetroCubico)]
    public void Oito_mais_dois_de_280_passa_nas_duas_unidades(UnidadeLimite unidade)
    {
        var r = LimiteSegundaClasse.Validar([new(1, 8, M3_280), new(2, 2, M3_280)], unidade);
        Assert.True(r.Valido);
        Assert.Equal(0.2m, r.Percentual);
    }

    [Fact]
    public void Mesmo_pedido_passa_em_pecas_e_falha_em_m3()
    {
        ItemParaLimite[] itens = [new(1, 8, M3_280), new(2, 2, M3_480)];

        var pecas = LimiteSegundaClasse.Validar(itens, UnidadeLimite.Pecas);
        var m3 = LimiteSegundaClasse.Validar(itens, UnidadeLimite.MetroCubico);

        Assert.True(pecas.Valido);
        Assert.False(m3.Valido);
        Assert.Equal(0.3m, m3.Percentual);
        Assert.Contains("30% do m³", m3.Mensagem);
    }

    [Fact]
    public void Sete_mais_dois_nao_passa()
    {
        var r = LimiteSegundaClasse.Validar([new(1, 7, M3_280), new(2, 2, M3_280)], UnidadeLimite.Pecas);
        Assert.False(r.Valido);
        Assert.Contains("22,2%", r.Mensagem);
    }

    [Fact]
    public void So_segunda_classe_nao_passa()
        => Assert.False(LimiteSegundaClasse.Validar([new(2, 10, M3_280)], UnidadeLimite.MetroCubico).Valido);

    [Fact]
    public void Pedido_vazio_e_invalido()
        => Assert.False(LimiteSegundaClasse.Validar([], UnidadeLimite.MetroCubico).Valido);

    [Fact]
    public void Limite_vem_do_parametro()
        => Assert.True(LimiteSegundaClasse.Validar([new(1, 7, M3_280), new(2, 3, M3_280)], UnidadeLimite.Pecas, 0.30m).Valido);
}