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

    /// <summary>
    /// A tag do fornecedor montada pelo pedido (Victor, 2026-11-14): SIGLA-NÚMERO DO PEDIDO-SEQUÊNCIA, como TG-20261013193233-28. O número
    /// do pedido junta todos os dígitos do código (sem dígito, vale o id); a sequência é a peça seguinte do pedido, com pelo menos 2 dígitos.
    /// </summary>
    [Theory]
    [InlineData("TG", "EX-20261013193233", 5, 27, "TG-20261013193233-28")]
    [InlineData("TG", "PD-20261017-195108", 5, 0, "TG-20261017195108-01")]
    [InlineData(null, "PD-20261017-195108", 5, 8, "20261017195108-09")]
    [InlineData("  ", "PD-20261017-195108", 5, 8, "20261017195108-09")]
    [InlineData("TG", "PED-CADEIA", 42, 0, "TG-42-01")]
    [InlineData("AB12", "P-7", 7, 99, "AB12-7-100")]
    public void Tag_do_fornecedor_montada_pelo_pedido(string? sigla, string codigoPedido, int idPedido, int cadastradas, string esperada)
        => Assert.Equal(esperada, TagHelper.DoFornecedor(sigla, codigoPedido, idPedido, cadastradas));
}