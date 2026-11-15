using ViaPermanente.Dominio.Via;

namespace ViaPermanente.Api.Via;

/// <summary>A porta de entrada do relógio: uma vez ao subir e depois todo dia, roda a manutenção de LEITURA_VIA.</summary>
public class ManutencaoDiariaHandler(IServiceScopeFactory escopos, ILogger<ManutencaoDiariaHandler> log) : BackgroundService
{
    /// <summary>Roda a manutenção agora e repete a cada 24 horas, até o back parar.</summary>
    protected override async Task ExecuteAsync(CancellationToken parar)
    {
        using var relogio = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                // Um escopo novo a cada rodada, porque o service usa o banco.
                using var escopo = escopos.CreateScope();
                var r = await escopo.ServiceProvider.GetRequiredService<ManutencaoLeituraViaService>().Executar(DateTime.Now);
                log.LogInformation("LEITURA_VIA: {Abertas} semana(s) aberta(s), {Descartadas} descartada(s), {Linhas} linha(s) apagada(s).",
                    r.SemanasAbertas.Count, r.SemanasDescartadas.Count, r.LinhasApagadas);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                // Uma falha não derruba o back: fica no log e tenta de novo no dia seguinte.
                log.LogError(erro, "A manutenção de LEITURA_VIA falhou; tenta de novo amanhã.");
            }
        } while (await relogio.WaitForNextTickAsync(parar));
    }
}