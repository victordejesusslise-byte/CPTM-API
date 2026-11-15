using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViaPermanente.Dados;

namespace ViaPermanente.Api.Comum;

/// <summary>Confere se o back alcança o banco (SQL Server). Usado pelo GET /hc.</summary>
public class BancoHealthCheck(ViaPermanenteContext db) : IHealthCheck
{
    /// <summary>Tenta falar com o banco: respondeu, tudo bem; não respondeu ou deu erro, o /hc volta 503.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("O banco responde.")
                : HealthCheckResult.Unhealthy("O banco não responde.");
        }
        catch (Exception e)
        {
            // O erro vai junto no resultado, para o log; a resposta do /hc mostra só a descrição.
            return HealthCheckResult.Unhealthy("O banco não responde.", e);
        }
    }
}

/// <summary>
/// GET /hc (health check): diz se o back subiu e se alcança o banco. Sem login. 200 quando está tudo bem, 503 quando algo falhou.
/// Padrão do Victor para todos os projetos (2026-10-17): o back na porta 8045 e o /hc.
/// </summary>
public static class Saude
{
    /// <summary>A resposta do /hc em JSON: o status geral, o tempo (em ms) e o resultado de cada verificação (hoje, só "banco").</summary>
    public static HealthCheckOptions Opcoes() => new()
    {
        ResponseWriter = async (contexto, relatorio) =>
        {
            contexto.Response.ContentType = "application/json; charset=utf-8";
            await contexto.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = relatorio.Status.ToString(),
                duracaoMs = Math.Round(relatorio.TotalDuration.TotalMilliseconds),
                verificacoes = relatorio.Entries.ToDictionary(e => e.Key, e => new { status = e.Value.Status.ToString(), descricao = e.Value.Description })
            }));
        }
    };
}
