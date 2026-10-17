using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViaPermanente.Api.Comum;
using ViaPermanente.Dados;

namespace ViaPermanente.Testes.Banco;

/// <summary>O GET /hc: com o banco no ar, a verificação do banco dá saudável; com um banco que não existe, não.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class SaudeNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Banco_no_ar_da_saudavel()
        => Assert.Equal(HealthStatus.Healthy, (await new BancoHealthCheck(db).CheckHealthAsync(new HealthCheckContext())).Status);

    [Fact]
    public async Task Banco_que_nao_existe_nao_da_saudavel()
    {
        await using var semBanco = BancoDeTeste.Contexto();
        await semBanco.Database.EnsureDeletedAsync();
        Assert.Equal(HealthStatus.Unhealthy, (await new BancoHealthCheck(semBanco).CheckHealthAsync(new HealthCheckContext())).Status);
        await BancoDeTeste.Recriar(db);
    }
}
