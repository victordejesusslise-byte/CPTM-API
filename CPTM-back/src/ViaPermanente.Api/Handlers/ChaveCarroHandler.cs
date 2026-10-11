using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ViaPermanente.Api.Helpers;

namespace ViaPermanente.Api.Handlers;

/// <summary>
/// A porta de entrada do carro de inspeção: ele se identifica pelo cabeçalho X-Chave-Carro, com a chave do .env
/// (Integracao__ChaveCarro). Quem entra assim tem só o papel "Carro", que abre apenas o envio da passagem.
/// </summary>
public class ChaveCarroHandler(IOptionsMonitor<AuthenticationSchemeOptions> opcoes, ILoggerFactory log, UrlEncoder codificador, IConfiguration configuracao)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, log, codificador)
{
    public const string Esquema = "ChaveCarro";
    public const string Cabecalho = "X-Chave-Carro";
    public const string Papel = "Carro";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Cabecalho, out var chave))
            return Task.FromResult(AuthenticateResult.NoResult());
        if (!ChaveHelper.Confere(chave.ToString(), configuracao["Integracao:ChaveCarro"]))
            return Task.FromResult(AuthenticateResult.Fail("Chave do carro inválida."));
        var identidade = new ClaimsIdentity([new Claim(TokenHelper.ClaimNome, "carro de inspeção"), new Claim(TokenHelper.ClaimCargo, Papel)], Esquema,
            TokenHelper.ClaimNome, TokenHelper.ClaimCargo);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }
}