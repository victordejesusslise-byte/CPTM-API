using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ViaPermanente.Api.Models;
using ViaPermanente.Dominio.Pessoas.Dtos;

namespace ViaPermanente.Api.Helpers;

/// <summary>Gera e valida o token JWT. O cargo vai na claim "role", que o [Authorize(Roles = ...)] lê.</summary>
public static class TokenHelper
{
    public const string ClaimId = "sub";
    public const string ClaimCargo = "role";
    public const string ClaimNome = "name";

    public static TokenDto Gerar(UsuarioDto usuario, JwtOpcoes opcoes, DateTime agoraUtc)
    {
        var expiraEm = agoraUtc.AddMinutes(opcoes.MinutosValidade);
        var descritor = new SecurityTokenDescriptor
        {
            Issuer = opcoes.Emissor,
            Audience = opcoes.Publico,
            IssuedAt = agoraUtc,
            NotBefore = agoraUtc,
            Expires = expiraEm,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimId, usuario.Id.ToString()),
                new Claim(ClaimNome, usuario.Nome),
                new Claim("email", usuario.Email),
                new Claim(ClaimCargo, usuario.Cargo)
            }),
            SigningCredentials = new SigningCredentials(Chave(opcoes), SecurityAlgorithms.HmacSha256)
        };
        return new TokenDto(new JsonWebTokenHandler().CreateToken(descritor), expiraEm, usuario);
    }

    public static TokenValidationParameters Parametros(JwtOpcoes opcoes) => new()
    {
        ValidIssuer = opcoes.Emissor,
        ValidAudience = opcoes.Publico,
        IssuerSigningKey = Chave(opcoes),
        NameClaimType = ClaimNome,
        RoleClaimType = ClaimCargo,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    /// <summary>O id do usuário que está chamando a API.</summary>
    public static int IdUsuario(ClaimsPrincipal usuario) => int.Parse(usuario.FindFirstValue(ClaimId)!);

    private static SymmetricSecurityKey Chave(JwtOpcoes opcoes) => new(Encoding.UTF8.GetBytes(opcoes.Chave));
}