using ViaPermanente.Api.Helpers;

namespace ViaPermanente.Testes.Helpers;

public class RedeLocalHelperTestes
{
    [Theory]
    [InlineData("http://192.168.0.10:4200", true)]
    [InlineData("http://10.0.0.7:4200", true)]
    [InlineData("http://172.20.1.5:4200", true)]
    [InlineData("http://localhost:4200", true)]
    // o site de produção, na 4300 (Victor, 2026-10-31: o ng serve fica na 4200 e a produção na 4300, a que a VPN serve)
    [InlineData("http://192.168.18.7:4300", true)]
    [InlineData("http://localhost:4300", true)]
    [InlineData("http://192.168.0.10:8080", false)]
    [InlineData("http://192.168.0.10:4301", false)]
    [InlineData("http://172.32.1.5:4200", false)]
    [InlineData("http://200.10.1.5:4200", false)]
    [InlineData("http://exemplo.com:4200", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Aceita_so_o_front_na_rede_privada(string? origem, bool esperado)
        => Assert.Equal(esperado, RedeLocalHelper.EhFrontDaRede(origem));
}
