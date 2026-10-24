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
public class PlanoCampoService(TrocaService trocas, AlertaService alertas, PecaService pecas)
{
    private static readonly StatusTroca[] NoPlano = [StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada];

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
        return new PlanoCampoDto(noite, agora, daNoite.OrderBy(t => t.IdTrecho).ThenBy(t => t.Km).ToList(), abertos, daPeca);
    }
}
