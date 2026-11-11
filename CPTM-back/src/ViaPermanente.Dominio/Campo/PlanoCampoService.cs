using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O trabalho da noite para o celular (app Android, Etapa 4, 2026-10-24): as trocas do plano da noite que está valendo (e as de
/// noites anteriores que ainda não foram feitas), os alertas abertos e as peças deles (Etapa 7: a inspeção sem rede abre por elas).
/// Vai a lista inteira, que é pequena: o celular troca a dele por esta.
/// </summary>
public class PlanoCampoService(TrocaService trocas, AlertaService alertas, PecaService pecas, LoteTrocaService? lotes = null)
{
    private static readonly StatusTroca[] NoPlano = [StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada];
    private static readonly StatusLoteTroca[] LoteNoPlano = [StatusLoteTroca.Aprovado, StatusLoteTroca.AguardandoPeca, StatusLoteTroca.Separado];

    /// <summary>Os lotes de troca da noite (e os de noites anteriores ainda não feitos), abertos: com a faixa e as separadas, para a via conferir sem rede.</summary>
    private async Task<IReadOnlyList<LoteTrocaDto>> Lotes(DateOnly noite)
    {
        if (lotes is null)
            return [];
        var daNoite = new List<LoteTrocaDto>();
        foreach (var status in LoteNoPlano)
            foreach (var lote in (await lotes.Listar(status)).Where(l => l.NoiteDoPlano is null || l.NoiteDoPlano <= noite))
                if (await lotes.BuscarPorId(lote.Id) is { } aberto)
                    daNoite.Add(aberto);
        return daNoite.OrderBy(l => l.IdTrecho).ThenBy(l => l.KmInicio).ToList();
    }

    public async Task<PlanoCampoDto> Montar(DateTime agora)
    {
        var noite = PlanoDaNoite.NoiteAtual(agora);
        var daNoite = new List<TrocaDto>();
        foreach (var status in NoPlano)
            daNoite.AddRange((await trocas.Listar(status)).Where(t => t.NoiteDoPlano is null || t.NoiteDoPlano <= noite));
        var abertos = await alertas.Listar(null, StatusAlerta.Aberto);
        var daPeca = new List<PecaDto>();
        foreach (var id in daNoite.Select(t => t.IdDormenteSaiu).Concat(abertos.Where(a => a.IdDormente is not null).Select(a => a.IdDormente!.Value)).Distinct())
            if (await pecas.BuscarPorId(id) is { } peca)
                daPeca.Add(peca);
        return new PlanoCampoDto(noite, agora, daNoite.OrderBy(t => t.IdTrecho).ThenBy(t => t.Km).ToList(), abertos, daPeca, await Lotes(noite));
    }
}
