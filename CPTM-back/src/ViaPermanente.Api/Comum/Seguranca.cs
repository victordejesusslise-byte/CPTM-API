using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Pessoas;

namespace ViaPermanente.Api.Comum;

/// <summary>
/// As defesas da API para os 4 achados da auditoria do Codex (12/10), corrigidos em 2026-10-18 com o Victor:
/// 1. HTTPS em produção (Seguranca:ExigirHttps): fora do próprio servidor, nada passa por HTTP; com proxy local, vale o X-Forwarded-Proto.
/// 2. A sessão cai na hora: a cada chamada, a conta tem de estar ativa e a versão da sessão do token tem de ser a atual.
/// 3. No máximo Seguranca:TentativasPorMinuto logins por minuto por aparelho (a espera crescente por conta fica no AuthService).
/// 4. (No front) o token só vai para a API do próprio sistema.
/// </summary>
public static class Seguranca
{
    public const string PoliticaLogin = "login";

    /// <summary>Depois de conferir assinatura e validade do token, confere a conta e a sessão no banco.</summary>
    public static JwtBearerEvents EventosDoToken() => new()
    {
        OnTokenValidated = async contexto =>
        {
            var principal = contexto.Principal;
            var id = int.TryParse(principal?.FindFirst(TokenHelper.ClaimId)?.Value, out var i) ? i : (int?)null;
            var versao = principal is null ? null : TokenHelper.VersaoSessao(principal);
            var auth = contexto.HttpContext.RequestServices.GetRequiredService<AuthService>();
            if (id is null || versao is null || !await auth.SessaoValida(id.Value, versao.Value))
                contexto.Fail("A sessão foi encerrada. Entre de novo.");
        }
    };

    /// <summary>O limite de tentativas de login por aparelho (endereço de origem), por minuto.</summary>
    public static IServiceCollection AddLimiteDeLogin(this IServiceCollection services, int porMinuto) => services.AddRateLimiter(o =>
    {
        o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        o.AddPolicy(PoliticaLogin, contexto => RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        o.OnRejected = (contexto, cancelar) => Problema(contexto.HttpContext, StatusCodes.Status429TooManyRequests,
            "Muitas tentativas de entrar deste aparelho. Espere um minuto e tente de novo.", cancelar);
    });

    /// <summary>
    /// HTTPS de produção. Lê o X-Forwarded-For/Proto só de proxy no próprio servidor (o padrão do ASP.NET). Com exigir, liga o HSTS e
    /// recusa (403) qualquer chamada por HTTP que não venha do próprio servidor, menos o /hc.
    /// </summary>
    public static void UseHttpsDeProducao(this WebApplication app, bool exigir)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
        if (!exigir)
            return;
        app.UseHsts();
        app.Use(async (contexto, proximo) =>
        {
            var doProprioServidor = contexto.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip);
            if (!contexto.Request.IsHttps && !doProprioServidor && contexto.Request.Path != "/hc")
            {
                await Problema(contexto, StatusCodes.Status403Forbidden, "Use o endereço com https://: a API não aceita senha nem token sem criptografia.", contexto.RequestAborted);
                return;
            }
            await proximo();
        });
    }

    /// <summary>
    /// Cabeçalhos de segurança em toda resposta: o navegador não adivinha o tipo do arquivo, a API não abre dentro de outra página e
    /// o endereço não vai adiante. A API só devolve JSON e arquivos, então a política de conteúdo não libera nada (menos o Swagger
    /// do desenvolvimento, que é uma página).
    /// </summary>
    public static void UseCabecalhosDeSeguranca(this WebApplication app) => app.Use(async (contexto, proximo) =>
    {
        var cabecalhos = contexto.Response.Headers;
        cabecalhos.XContentTypeOptions = "nosniff";
        cabecalhos.XFrameOptions = "DENY";
        cabecalhos["Referrer-Policy"] = "no-referrer";
        if (!contexto.Request.Path.StartsWithSegments("/swagger"))
            cabecalhos.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        await proximo();
    });

    private static async ValueTask Problema(HttpContext contexto, int status, string detalhe, CancellationToken cancelar)
    {
        contexto.Response.StatusCode = status;
        contexto.Response.ContentType = "application/problem+json; charset=utf-8";
        await contexto.Response.WriteAsJsonAsync(new { status, detail = detalhe }, cancelar);
    }
}
