using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Pecas;

public class SinaisPecaRepository(ViaPermanenteContext db) : ISinaisPecaRepository
{
    public async Task<IReadOnlyList<int>> PecasEmUso()
        => await db.Set<Dormente>().AsNoTracking().Where(d => d.RetiradaEm == null && (d.IdPilha != null || d.IdTrecho != null)).Select(d => d.IdDormente).ToListAsync();

    public async Task<IReadOnlyDictionary<int, SinaisDaPeca>> Sinais(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0) return new Dictionary<int, SinaisDaPeca>();
        var lista = ids.ToList();
        var qualidade = (await db.Set<VerificacaoQualidade>().AsNoTracking().Where(v => lista.Contains(v.IdDormente))
                .Select(v => new { v.IdDormente, v.Data, v.Resultado, v.Rachadura, v.AtaqueBiologico }).ToListAsync())
            .GroupBy(v => v.IdDormente).ToDictionary(g => g.Key, g => g.MaxBy(v => v.Data)!);
        var inspecoes = (await db.Set<Inspecao>().AsNoTracking().Where(i => lista.Contains(i.IdDormente))
                .Select(i => new { i.IdDormente, i.FinalizadoEm, i.EstadoConservacao, i.Resultado }).ToListAsync())
            .GroupBy(i => i.IdDormente).ToDictionary(g => g.Key, g => g.MaxBy(i => i.FinalizadoEm)!);
        var alertas = (await db.Set<Alerta>().AsNoTracking()
                .Where(a => a.IdDormente != null && lista.Contains(a.IdDormente.Value) && (a.Status == StatusAlerta.Aberto || a.Status == StatusAlerta.Confirmado))
                .Select(a => new { IdDormente = a.IdDormente!.Value, a.Data, a.Tipo, a.Status }).ToListAsync())
            .GroupBy(a => a.IdDormente).ToDictionary(g => g.Key, g => g.MaxBy(a => a.Data)!);

        // Posição: a linha da estação de início do trecho e a ordem da peça no trecho pelo km.
        var naVia = await db.Set<Dormente>().AsNoTracking().Where(d => lista.Contains(d.IdDormente) && d.IdTrecho != null && d.KmProjeto != null)
            .Select(d => new { d.IdDormente, IdTrecho = d.IdTrecho!.Value }).ToListAsync();
        var trechos = naVia.Select(d => d.IdTrecho).Distinct().ToList();
        var ordem = (await db.Set<Dormente>().AsNoTracking().Where(d => d.IdTrecho != null && trechos.Contains(d.IdTrecho.Value) && d.KmProjeto != null && d.RetiradaEm == null)
                .Select(d => new { d.IdDormente, IdTrecho = d.IdTrecho!.Value, Km = d.KmProjeto!.Value }).ToListAsync())
            .GroupBy(d => d.IdTrecho)
            .SelectMany(g => g.OrderBy(d => d.Km).ThenBy(d => d.IdDormente).Select((d, i) => (d.IdDormente, Numero: i + 1)))
            .ToDictionary(x => x.IdDormente, x => x.Numero);
        var linhas = await (from t in db.Set<Trecho>().AsNoTracking()
                            join e in db.Set<Estacao>().AsNoTracking() on t.IdEstacaoInicio equals e.IdEstacao
                            where trechos.Contains(t.IdTrecho)
                            select new { t.IdTrecho, e.Linha }).ToDictionaryAsync(x => x.IdTrecho, x => x.Linha);
        var trechoDa = naVia.ToDictionary(d => d.IdDormente, d => d.IdTrecho);

        return lista.Distinct().ToDictionary(id => id, id =>
        {
            qualidade.TryGetValue(id, out var q);
            inspecoes.TryGetValue(id, out var i);
            alertas.TryGetValue(id, out var a);
            var linha = trechoDa.TryGetValue(id, out var t) ? linhas.GetValueOrDefault(t) : null;
            return new SinaisDaPeca(q?.Data, q?.Resultado, q?.Rachadura, q?.AtaqueBiologico, i?.FinalizadoEm, i?.EstadoConservacao, i?.Resultado,
                a?.Data, a?.Tipo, a?.Status, linha, ordem.TryGetValue(id, out var n) ? n : null);
        });
    }
}
