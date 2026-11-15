using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ViaPermanente.Api.Helpers;

namespace ViaPermanente.Api.Via;

/// <summary>
/// A porta de entrada do carro de inspeção: ele se identifica pelo cabeçalho X-Chave-Carro, com a chave do .env
/// (Integracao__ChaveCarro). Quem entra assim tem só o papel "Carro", que abre apenas o envio da passagem.
/// </summary>
public class ChaveCarroHandler(IOptionsMonitor<AuthenticationSchemeOptions> opcoes, ILoggerFactory log, UrlEncoder codificador, IConfiguration configuracao)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, log, codificador)
{
    /// <summary>O nome deste jeito de entrar, usado no [Authorize(AuthenticationSchemes = ...)].</summary>
    public const string Esquema = "ChaveCarro";
    /// <summary>O cabeçalho em que o carro manda a chave.</summary>
    public const string Cabecalho = "X-Chave-Carro";
    /// <summary>O papel de quem entra com a chave do carro (não é um cargo de pessoa).</summary>
    public const string Papel = "Carro";

    /// <summary>Confere a chave do cabeçalho e, se bater, deixa entrar como "carro de inspeção".</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Sem o cabeçalho, este jeito de entrar não se aplica (pode ser uma pessoa com token).
        if (!Request.Headers.TryGetValue(Cabecalho, out var chave))
            return Task.FromResult(AuthenticateResult.NoResult());
        // Chave errada: recusa.
        if (!ChaveHelper.Confere(chave.ToString(), configuracao["Integracao:ChaveCarro"]))
            return Task.FromResult(AuthenticateResult.Fail("Chave do carro inválida."));
        // Chave certa: entra com o nome "carro de inspeção" e o papel Carro.
        var identidade = new ClaimsIdentity([new Claim(TokenHelper.ClaimNome, "carro de inspeção"), new Claim(TokenHelper.ClaimCargo, Papel)], Esquema,
            TokenHelper.ClaimNome, TokenHelper.ClaimCargo);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
    }
}