using ViaPermanente.Dominio.Helpers;

namespace ViaPermanente.Testes.Helpers;

public class TagHelperTestes
{
    [Theory]
    [InlineData(null, "BR0000000001")]
    [InlineData("BR0000000041", "BR0000000042")]
    [InlineData("XX123", "BR0000000001")]
    public void Proxima_tag_segue_a_sequencia(string? ultima, string esperada)
        => Assert.Equal(esperada, TagHelper.Proxima(ultima));

    [Theory]
    [InlineData("BR0000000001", true)]
    [InlineData("BR000000001", false)]
    [InlineData("XX0000000001", false)]
    [InlineData("BR00000000A1", false)]
    [InlineData(null, false)]
    public void Formato_do_numero_gravado(string? tag, bool valida) => Assert.Equal(valida, TagHelper.Valida(tag));
}