using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Api.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;

namespace ViaPermanente.Api.Pessoas;

[ApiController]
[Route("api/auth")]
public class AuthHandler(AuthService auth, UsuarioService usuarios, JwtOpcoes jwt) : ControllerBase
{
    /// <summary>Entra com e-mail e senha e recebe o token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenDto>> Login(LoginDto login)
    {
        var usuario = await auth.Entrar(login);
        if (usuario is null)
            return Problem("E-mail ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);
        return TokenHelper.Gerar(usuario, jwt, DateTime.UtcNow);
    }

    /// <summary>Quem está logado.</summary>
    [HttpGet("eu")]
    public async Task<ActionResult<UsuarioDto>> Eu()
    {
        var usuario = await usuarios.BuscarPorId(TokenHelper.IdUsuario(User));
        return usuario is null ? NotFound() : usuario;
    }
}