using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Via;

/// <summary>O resultado da manutenção: semanas abertas, semanas descartadas, linhas apagadas e as partições de agora.</summary>
public record ManutencaoLeituraViaDto(IReadOnlyList<DateTime> SemanasAbertas, IReadOnlyList<DateTime> SemanasDescartadas, long LinhasApagadas,
    IReadOnlyList<ParticaoLeituraVia> Particoes);

/// <summary>A manutenção diária de LEITURA_VIA (RetencaoLeituraVia): abre as semanas à frente e descarta o que passou de 90 dias.</summary>
public class ManutencaoLeituraViaService(IParticaoRepository particoes)
{
    public async Task<ManutencaoLeituraViaDto> Executar(DateTime hoje)
    {
        var atuais = await particoes.Fronteiras();
        var abrir = RetencaoLeituraVia.FronteirasNovas(atuais, hoje);
        foreach (var fronteira in abrir)
            await particoes.Abrir(fronteira);

        var descartar = RetencaoLeituraVia.FronteirasParaDescartar([.. atuais, .. abrir], hoje);
        long apagadas = 0;
        foreach (var fronteira in descartar)
            apagadas += await particoes.DescartarPrimeira(fronteira);
        return new ManutencaoLeituraViaDto(abrir, descartar, apagadas, await particoes.Particoes());
    }

    public async Task<IReadOnlyList<ParticaoLeituraVia>> Particoes() => await particoes.Particoes();
}