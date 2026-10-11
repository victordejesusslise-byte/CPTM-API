using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Services;

public class GravacaoRigorosaTestes
{
    [Theory]
    [InlineData(1, "BR0000000007", ResultadoGravacao.Ok)]
    [InlineData(2, "BR0000000007", ResultadoGravacao.Bloqueada)]
    [InlineData(0, "BR0000000007", ResultadoGravacao.Bloqueada)]
    [InlineData(1, null, ResultadoGravacao.Falhou)]
    [InlineData(1, "BR0000000008", ResultadoGravacao.Divergente)]
    public void So_vale_com_uma_tag_e_releitura_igual(int tagsVistas, string? relida, ResultadoGravacao esperado)
        => Assert.Equal(esperado, GravacaoRigorosa.Resultado(tagsVistas, "BR0000000007", relida));

    [Fact]
    public void Alvo_unico_vale_para_modulo_e_pistola()
    {
        Assert.True(AlvoUnico.EhAmbigua(MetodoCaptura.ModuloUhf, 2));
        Assert.True(AlvoUnico.EhAmbigua(MetodoCaptura.Pistola, 3));
        Assert.False(AlvoUnico.EhAmbigua(MetodoCaptura.Digitado, 3));
        Assert.False(AlvoUnico.EhAmbigua(MetodoCaptura.ModuloUhf, 1));
    }
}