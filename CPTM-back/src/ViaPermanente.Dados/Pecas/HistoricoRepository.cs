using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dados.Pecas;

public class HistoricoRepository(ViaPermanenteContext db) : IHistoricoRepository
{
    public async Task<HistoricoDaPeca> Buscar(int idDormente)
    {
        var chegada = await (from d in db.Set<Dormente>() join f in db.Set<Feixe>() on d.IdFeixe equals f.IdFeixe
                             join r in db.Set<Recebimento>() on f.IdRecebimento equals r.IdRecebimento
                             where d.IdDormente == idDormente select r).AsNoTracking().FirstOrDefaultAsync();
        var tags = await db.Set<Rfid>().AsNoTracking().Where(r => r.IdDormente == idDormente).ToListAsync();
        var idsTags = tags.Select(t => t.IdRfid).ToList();
        var alertas = await db.Set<Alerta>().AsNoTracking().Where(a => a.IdDormente == idDormente).ToListAsync();
        var idsAlertas = alertas.Select(a => a.IdAlerta).ToList();
        return new HistoricoDaPeca(chegada, tags,
            await db.Set<GravacaoTag>().AsNoTracking().Where(g => g.IdRfid != null && idsTags.Contains(g.IdRfid.Value)).ToListAsync(),
            await db.Set<ClassificacaoMadeira>().AsNoTracking().Where(c => c.IdDormente == idDormente).ToListAsync(),
            await db.Set<VerificacaoQualidade>().AsNoTracking().Where(v => v.IdDormente == idDormente).ToListAsync(),
            await db.Set<Movimentacao>().AsNoTracking().Where(m => m.IdDormente == idDormente).ToListAsync(),
            await db.Set<LeituraDormente>().AsNoTracking().Where(l => l.IdDormente == idDormente).ToListAsync(),
            alertas,
            await db.Set<VistoriaAlerta>().AsNoTracking().Where(v => idsAlertas.Contains(v.IdAlerta)).ToListAsync(),
            await db.Set<Inspecao>().AsNoTracking().Where(i => i.IdDormente == idDormente).ToListAsync(),
            await db.Set<Troca>().AsNoTracking().Where(t => t.IdDormenteSaiu == idDormente || t.IdDormenteEntrou == idDormente).ToListAsync());
    }
}