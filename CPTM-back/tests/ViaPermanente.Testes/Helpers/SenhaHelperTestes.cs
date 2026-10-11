using ViaPermanente.Dominio.Helpers;

namespace ViaPermanente.Testes.Helpers;

public class SenhaHelperTestes
{
    [Fact]
    public void Hash_confere_com_a_senha_certa_e_nao_guarda_a_senha()
    {
        var hash = SenhaHelper.GerarHash("trilho-forte-9");

        Assert.DoesNotContain("trilho-forte-9", hash);
        Assert.True(SenhaHelper.Conferir("trilho-forte-9", hash));
        Assert.False(SenhaHelper.Conferir("trilho-forte-8", hash));
    }

    [Fact]
    public void Mesma_senha_gera_hash_diferente_por_causa_do_sal()
        => Assert.NotEqual(SenhaHelper.GerarHash("abc12345"), SenhaHelper.GerarHash("abc12345"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("pbkdf2-sha256$100000$nao-e-base64$x")]
    public void Hash_vazio_ou_fora_do_formato_nao_confere(string? hash)
        => Assert.False(SenhaHelper.Conferir("abc12345", hash));
}