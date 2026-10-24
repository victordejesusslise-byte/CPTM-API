using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O trabalho da noite para o celular (app Android, Etapa 4, 2026-10-24): as trocas do plano da noite que está valendo (e as de
/// noites anteriores que ainda não foram feitas) e os alertas abertos. Vai a lista inteira, que é pequena: o celular troca a dele por esta.
/// </summary>
public class PlanoCampoService(TrocaService trocas, AlertaService alertas)
{
    private static readonly StatusTroca[] NoPlano = [StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada];

    public async Task<PlanoCampoDto> Montar(DateTime agora)
    {
        var noite = PlanoDaNoite.NoiteAtual(agora);
        var daNoite = new List<TrocaDto>();
        foreach (var status in NoPlano)
            daNoite.AddRange((await trocas.Listar(status)).Where(t => t.NoiteDoPlano is null || t.NoiteDoPlano <= noite));
        return new PlanoCampoDto(noite, agora, daNoite.OrderBy(t => t.IdTrecho).ThenBy(t => t.Km).ToList(), await alertas.Listar(null, StatusAlerta.Aberto));
    }
}
