using Microsoft.IdentityModel.JsonWebTokens;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Api.Models;
using ViaPermanente.Dominio.Pessoas.Dtos;

namespace ViaPermanente.Testes.Helpers;

public class TokenHelperTestes
{
    private static readonly JwtOpcoes Opcoes = new()
    {
        Chave = new string('k', 64),
        Emissor = "ViaPermanente.Api",
        Publico = "CPTM-front",
        MinutosValidade = 60
    };

    [Fact]
    public async Task Token_valido_leva_o_id_e_o_cargo()
    {
        var token = TokenHelper.Gerar(new UsuarioDto(7, "Ana", "ana@cptm.local", "Tecnico", true), Opcoes, DateTime.UtcNow);

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, TokenHelper.Parametros(Opcoes));

        Assert.True(resultado.IsValid);
        Assert.Equal("7", resultado.ClaimsIdentity.FindFirst(TokenHelper.ClaimId)!.Value);
        Assert.True(new System.Security.Claims.ClaimsPrincipal(resultado.ClaimsIdentity).IsInRole("Tecnico"));
    }

    [Fact]
    public async Task Token_assinado_com_outra_chave_nao_vale()
    {
        var token = TokenHelper.Gerar(new UsuarioDto(7, "Ana", "ana@cptm.local", "Tecnico", true), Opcoes, DateTime.UtcNow);
        var outra = new JwtOpcoes { Chave = new string('x', 64), Emissor = Opcoes.Emissor, Publico = Opcoes.Publico };

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, TokenHelper.Parametros(outra));

        Assert.False(resultado.IsValid);
    }
}