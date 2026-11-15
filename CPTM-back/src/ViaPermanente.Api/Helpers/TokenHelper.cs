using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ViaPermanente.Api.Models;
using ViaPermanente.Dominio.Pessoas.Dtos;

namespace ViaPermanente.Api.Helpers;

/// <summary>
/// Gera e valida o token JWT. O cargo vai na claim "role", que o [Authorize(Roles = ...)] lê.
/// No login sai o token assinado com a chave do .env; a cada chamada ele é conferido (decisão: Login pelo e-mail da pessoa).
/// </summary>
public static class TokenHelper
{
    /// <summary>Claim com o id do usuário (→ USUARIO).</summary>
    public const string ClaimId = "sub";
    /// <summary>Claim com o cargo (Tecnico, Supervisor...): é a permissão do token.</summary>
    public const string ClaimCargo = "role";
    /// <summary>Claim com o nome da pessoa, para a tela mostrar quem está logado.</summary>
    public const string ClaimNome = "name";
    /// <summary>A versão da sessão do usuário quando o token saiu; se não for mais a atual, o token não vale (sair, desativar).</summary>
    public const string ClaimVersaoSessao = "sv";

    /// <summary>Monta o token de quem acabou de entrar (id, nome, e-mail, cargo e versão da sessão), válido por MinutosValidade.</summary>
    public static TokenDto Gerar(UsuarioDto usuario, int versaoSessao, JwtOpcoes opcoes, DateTime agoraUtc)
    {
        // A validade conta em UTC, igual ao relógio do token.
        var expiraEm = agoraUtc.AddMinutes(opcoes.MinutosValidade);
        var descritor = new SecurityTokenDescriptor
        {
            Issuer = opcoes.Emissor,
            Audience = opcoes.Publico,
            IssuedAt = agoraUtc,
            NotBefore = agoraUtc,
            Expires = expiraEm,
            // O que vai dentro do token: quem é, o cargo e a versão da sessão.
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimId, usuario.Id.ToString()),
                new Claim(ClaimNome, usuario.Nome),
                new Claim("email", usuario.Email),
                new Claim(ClaimCargo, usuario.Cargo),
                new Claim(ClaimVersaoSessao, versaoSessao.ToString())
            }),
            SigningCredentials = new SigningCredentials(Chave(opcoes), SecurityAlgorithms.HmacSha256)
        };
        return new TokenDto(new JsonWebTokenHandler().CreateToken(descritor), expiraEm, usuario);
    }

    /// <summary>As regras para aceitar um token: mesmo emissor, mesmo público, assinado com a nossa chave e dentro da validade.</summary>
    public static TokenValidationParameters Parametros(JwtOpcoes opcoes) => new()
    {
        ValidIssuer = opcoes.Emissor,
        ValidAudience = opcoes.Publico,
        IssuerSigningKey = Chave(opcoes),
        // Só a assinatura com que o token sai: token sem assinatura ("none") ou com outro algoritmo não vale.
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = ClaimNome,
        RoleClaimType = ClaimCargo,
        // Tolerância de 1 minuto para relógios um pouco diferentes entre o celular, o PC e o servidor.
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    /// <summary>A versão da sessão que veio no token (nula em token antigo, sem ela: não vale).</summary>
    public static int? VersaoSessao(ClaimsPrincipal usuario) => int.TryParse(usuario.FindFirstValue(ClaimVersaoSessao), out var v) ? v : null;

    /// <summary>O id do usuário que está chamando a API.</summary>
    public static int IdUsuario(ClaimsPrincipal usuario) => int.Parse(usuario.FindFirstValue(ClaimId)!);

    /// <summary>A chave simétrica feita do texto do .env (Jwt__Chave).</summary>
    private static SymmetricSecurityKey Chave(JwtOpcoes opcoes) => new(Encoding.UTF8.GetBytes(opcoes.Chave));
}