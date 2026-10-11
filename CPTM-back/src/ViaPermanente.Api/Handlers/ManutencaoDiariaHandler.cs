using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>A porta de entrada do relógio: uma vez ao subir e depois todo dia, roda a manutenção de LEITURA_VIA.</summary>
public class ManutencaoDiariaHandler(IServiceScopeFactory escopos, ILogger<ManutencaoDiariaHandler> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parar)
    {
        using var relogio = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                using var escopo = escopos.CreateScope();
                var r = await escopo.ServiceProvider.GetRequiredService<ManutencaoLeituraViaService>().Executar(DateTime.Now);
                log.LogInformation("LEITURA_VIA: {Abertas} semana(s) aberta(s), {Descartadas} descartada(s), {Linhas} linha(s) apagada(s).",
                    r.SemanasAbertas.Count, r.SemanasDescartadas.Count, r.LinhasApagadas);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                log.LogError(erro, "A manutenção de LEITURA_VIA falhou; tenta de novo amanhã.");
            }
        } while (await relogio.WaitForNextTickAsync(parar));
    }
}