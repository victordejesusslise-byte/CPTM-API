using ViaPermanente.Dominio.Helpers;

namespace ViaPermanente.Testes.Helpers;

public class VolumeHelperTestes
{
    [Theory]
    [InlineData(2.80, 0.11424)]
    [InlineData(3.00, 0.12240)]
    [InlineData(4.00, 0.16320)]
    [InlineData(4.80, 0.19584)]
    public void M3_do_catalogo_com_medida_provisoria(decimal comprimento, decimal esperado)
        => Assert.Equal(esperado, VolumeHelper.M3(comprimento, 0.24m, 0.17m));
}