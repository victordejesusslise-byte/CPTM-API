using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dados.Armazens;

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
                   select new { d.IdDormente, p.IdPilha, Tag = db.Set<GravacaoTag>().Where(g => g.IdDormente == d.IdDormente && g.Vigente).Select(g => g.TagGravada).FirstOrDefault() })
                .ToListAsync())
            .Select(x => (x.IdDormente, x.IdPilha, (string?)x.Tag)).ToList();

    public async Task<IReadOnlyDictionary<string, int>> DonosDasTags(IReadOnlyCollection<string> tags)
        => await db.Set<GravacaoTag>().AsNoTracking().Where(g => g.Vigente && tags.Contains(g.TagGravada)).ToDictionaryAsync(g => g.TagGravada, g => g.IdDormente!.Value);

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
            .Where(l => l.Aviso && l.IdDormente == null && l.TagLida != null && !db.Set<GravacaoTag>().Any(g => g.TagGravada == l.TagLida && g.IdDormente != null))
            .OrderByDescending(l => l.DataLeitura).ToListAsync();

    public async Task Vincular(GravacaoTag? desligada, GravacaoTag ligada, LeituraDormente leitura)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (desligada is not null)
        {
            // A única mudança numa linha antiga da GRAVACAO_TAG: a marca vigente passa de 1 para 0. A Desligada entra antes da Ligada.
            await db.Set<GravacaoTag>().Where(g => g.IdDormente == ligada.IdDormente && g.Vigente).ExecuteUpdateAsync(s => s.SetProperty(g => g.Vigente, false));
            db.Add(desligada);
            await db.SaveChangesAsync();
        }
        db.Add(ligada);
        db.Add(leitura);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }
}