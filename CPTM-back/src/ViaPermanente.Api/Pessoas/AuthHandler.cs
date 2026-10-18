using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ViaPermanente.Api.Comum;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Api.Models;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;

namespace ViaPermanente.Api.Pessoas;

[ApiController]
[Route("api/auth")]
public class AuthHandler(AuthService auth, UsuarioService usuarios, JwtOpcoes jwt, ILogger<AuthHandler> log) : ControllerBase
{
    /// <summary>
    /// Entra com e-mail e senha e recebe o token (vale 8 horas, um turno). No máximo 10 tentativas por minuto por aparelho (429);
    /// depois de 5 senhas erradas seguidas, a conta espera (429 com a hora). Toda falha fica no log, com o e-mail e o aparelho.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting(Seguranca.PoliticaLogin)]
    [HttpPost("login")]
    public async Task<ActionResult<TokenDto>> Login(LoginDto login)
    {
        var resultado = await auth.Entrar(login);
        var origem = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "?";
        if (resultado.EsperarAte is { } ate)
        {
            log.LogWarning("Login em espera para {Email} (de {Origem}) até {Ate}", EmailHelper.Normalizar(login.Email), origem, ate);
            return Problem($"Muitas senhas erradas seguidas. Tente de novo às {ate:HH:mm:ss}.", statusCode: StatusCodes.Status429TooManyRequests);
        }
        if (resultado.Usuario is null)
        {
            log.LogWarning("Login recusado para {Email} (de {Origem})", EmailHelper.Normalizar(login.Email), origem);
            return Problem("E-mail ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);
        }
        return TokenHelper.Gerar(resultado.Usuario, resultado.VersaoSessao, jwt, DateTime.UtcNow);
    }

    /// <summary>Sair: o token deste e de todos os aparelhos do usuário deixa de valer na hora.</summary>
    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    {
        await auth.Sair(TokenHelper.IdUsuario(User));
        return NoContent();
    }

    /// <summary>Quem está logado.</summary>
    [HttpGet("eu")]
    public async Task<ActionResult<UsuarioDto>> Eu()
    {
        var usuario = await usuarios.BuscarPorId(TokenHelper.IdUsuario(User));
        return usuario is null ? NotFound() : usuario;
    }
}
