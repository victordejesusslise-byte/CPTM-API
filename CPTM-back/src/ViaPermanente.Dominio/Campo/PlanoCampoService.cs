using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O trabalho da noite para o celular da via: as trocas e os lotes do plano (e os de noites anteriores ainda não feitos),
/// os alertas abertos e as peças deles, para abrir sem rede. Vai a lista inteira, que é pequena: o celular troca a dele por esta.
/// </summary>
public class PlanoCampoService(TrocaService trocas, AlertaService alertas, PecaService pecas, LoteTrocaService? lotes = null)
{
    // As trocas que ainda vão para a via (já aprovadas, ainda não feitas).
    private static readonly StatusTroca[] NoPlano = [StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada];
    // O mesmo para os lotes.
    private static readonly StatusLoteTroca[] LoteNoPlano = [StatusLoteTroca.Aprovado, StatusLoteTroca.AguardandoPeca, StatusLoteTroca.Separado];

    /// <summary>Os lotes de troca da noite (e os de noites anteriores ainda não feitos), abertos: com a faixa e as separadas, para a via conferir sem rede.</summary>
    private async Task<IReadOnlyList<LoteTrocaDto>> Lotes(DateOnly noite)
    {
        // Sem o serviço dos lotes, não há lote no plano.
        if (lotes is null)
            return [];
        var daNoite = new List<LoteTrocaDto>();
        // Os lotes desta noite ou de antes; cada um aberto por inteiro (com a faixa e as separadas).
        foreach (var status in LoteNoPlano)
            foreach (var lote in (await lotes.Listar(status)).Where(l => l.NoiteDoPlano is null || l.NoiteDoPlano <= noite))
                if (await lotes.BuscarPorId(lote.Id) is { } aberto)
                    daNoite.Add(aberto);
        return daNoite.OrderBy(l => l.IdTrecho).ThenBy(l => l.KmInicio).ToList();
    }

    /// <summary>Monta o plano da noite que está valendo agora.</summary>
    public async Task<PlanoCampoDto> Montar(DateTime agora)
    {
        var noite = PlanoDaNoite.NoiteAtual(agora);
        // As trocas desta noite e as de noites anteriores que ficaram para trás.
        var daNoite = new List<TrocaDto>();
        foreach (var status in NoPlano)
            daNoite.AddRange((await trocas.Listar(status)).Where(t => t.NoiteDoPlano is null || t.NoiteDoPlano <= noite));
        var abertos = await alertas.Listar(null, StatusAlerta.Aberto);
        // As peças que o celular precisa: a que sai em cada troca e a de cada alerta.
        var daPeca = new List<PecaDto>();
        // O dormente antigo sem cadastro (troca feita no lote) não tem peça para baixar: só entram os ids que existem.
        foreach (var id in daNoite.Select(t => t.IdDormenteSaiu).OfType<int>().Concat(abertos.Where(a => a.IdDormente is not null).Select(a => a.IdDormente!.Value)).Distinct())
            if (await pecas.BuscarPorId(id) is { } peca)
                daPeca.Add(peca);
        return new PlanoCampoDto(noite, agora, daNoite.OrderBy(t => t.IdTrecho).ThenBy(t => t.Km).ToList(), abertos, daPeca, await Lotes(noite));
    }
}
