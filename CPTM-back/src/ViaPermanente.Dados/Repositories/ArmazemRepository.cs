using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class ArmazemRepository(ViaPermanenteContext db) : IArmazemRepository
{
    public Task<ConferenciaArmazem?> BuscarConferencia(int idConferencia)
        => db.Set<ConferenciaArmazem>().AsNoTracking().FirstOrDefaultAsync(c => c.IdConferencia == idConferencia);

    public Task<ConferenciaArmazem?> ConferenciaAberta(int idArmazem)
        => db.Set<ConferenciaArmazem>().AsNoTracking().FirstOrDefaultAsync(c => c.IdArmazem == idArmazem && c.ConcluidaEm == null);

    public async Task CriarConferencia(ConferenciaArmazem conferencia)
    {
        db.Add(conferencia);
        await db.SaveChangesAsync();
    }

    public async Task ConcluirConferencia(ConferenciaArmazem conferencia)
    {
        db.Update(conferencia);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<LeituraDormente>> LeiturasDaConferencia(int idConferencia)
        => await db.Set<LeituraDormente>().AsNoTracking().Where(l => l.IdConferencia == idConferencia).OrderBy(l => l.IdLeitura).ToListAsync();

    public async Task RegistrarLeituras(IReadOnlyList<LeituraDormente> leituras)
    {
        db.AddRange(leituras);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<(int IdDormente, int IdPilha, string? Tag)>> PecasNoArmazem(int idArmazem)
        => (await (from d in db.Set<Dormente>().AsNoTracking()
                   join p in db.Set<Pilha>() on d.IdPilha equals p.IdPilha
                   where p.IdArmazem == idArmazem
                   select new { d.IdDormente, p.IdPilha, Tag = db.Set<Rfid>().Where(r => r.IdDormente == d.IdDormente && r.Ativo).Select(r => r.TagRfid).FirstOrDefault() })
                .ToListAsync())
            .Select(x => (x.IdDormente, x.IdPilha, (string?)x.Tag)).ToList();

    public async Task<IReadOnlyDictionary<string, int>> DonosDasTags(IReadOnlyCollection<string> tags)
        => await db.Set<Rfid>().AsNoTracking().Where(r => r.Ativo && tags.Contains(r.TagRfid)).ToDictionaryAsync(r => r.TagRfid, r => r.IdDormente);

    public async Task TrocarPilha(int idDormente, int idPilhaNova, Movimentacao movimentacao, LeituraDormente leitura)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(leitura);
        await db.SaveChangesAsync();
        movimentacao.IdLeitura = leitura.IdLeitura;
        db.Add(movimentacao);
        await db.SaveChangesAsync();
        await db.Set<Dormente>().Where(d => d.IdDormente == idDormente).ExecuteUpdateAsync(s => s.SetProperty(d => d.IdPilha, idPilhaNova));
        await transacao.CommitAsync();
    }

    public async Task<IReadOnlyList<LeituraDormente>> AvisosPendentes()
        => await db.Set<LeituraDormente>().AsNoTracking()
            .Where(l => l.Aviso && l.IdDormente == null && l.TagLida != null && !db.Set<Rfid>().Any(r => r.TagRfid == l.TagLida))
            .OrderByDescending(l => l.DataLeitura).ToListAsync();

    public async Task Vincular(Rfid nova, int? idRfidAntiga, LeituraDormente leitura)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (idRfidAntiga is int idAntiga)
            await db.Set<Rfid>().Where(r => r.IdRfid == idAntiga).ExecuteUpdateAsync(s => s.SetProperty(r => r.Ativo, false));
        db.Add(nova);
        db.Add(leitura);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }
}