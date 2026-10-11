using ViaPermanente.Api.Helpers;

namespace ViaPermanente.Testes.Helpers;

public class ChaveHelperTestes
{
    [Theory]
    [InlineData("chave-certa", "chave-certa", true)]
    [InlineData("chave-errada", "chave-certa", false)]
    [InlineData("", "chave-certa", false)]
    [InlineData("chave-certa", null, false)]
    [InlineData(null, null, false)]
    public void Confere_so_a_chave_igual_e_nunca_sem_chave(string? recebida, string? configurada, bool esperado)
        => Assert.Equal(esperado, ChaveHelper.Confere(recebida, configurada));
}