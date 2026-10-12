using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Helpers;

public class CodigoDaPecaTestes
{
    [Theory]
    [InlineData("DM-E01-L1002-1030", 1030)]
    [InlineData(" dm-e01-l02-0034 ", 34)]
    [InlineData("DM-E01-L02", null)]
    [InlineData("BR0000000034", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Tira_o_numero_da_peca_do_fim_do_codigo(string? codigo, int? esperado)
        => Assert.Equal(esperado, PecaService.IdDoCodigo(codigo));
}
