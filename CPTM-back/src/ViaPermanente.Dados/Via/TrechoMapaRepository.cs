using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Via;

public class TrechoMapaRepository(ViaPermanenteContext db) : ITrechoMapaRepository
{
    public async Task<IReadOnlyList<TrechoDaVia>> Trechos()
        => await (from t in db.Set<Trecho>().AsNoTracking()
                  join i in db.Set<Estacao>().AsNoTracking() on t.IdEstacaoInicio equals i.IdEstacao
                  join f in db.Set<Estacao>().AsNoTracking() on t.IdEstacaoFim equals f.IdEstacao
                  orderby i.Linha, i.Km
                  select new TrechoDaVia(t.IdTrecho, i.Nome, f.Nome, i.Linha, i.Km, f.Km)).ToListAsync();

    public async Task<IReadOnlyList<(int IdDormente, int IdTrecho)>> PecasNaVia(int? idTrecho = null)
        => (await db.Set<Dormente>().AsNoTracking()
                .Where(d => d.IdTrecho != null && d.KmProjeto != null && d.RetiradaEm == null && (idTrecho == null || d.IdTrecho == idTrecho))
                .Select(d => new { d.IdDormente, IdTrecho = d.IdTrecho!.Value }).ToListAsync())
            .Select(d => (d.IdDormente, d.IdTrecho)).ToList();
}
