using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class NotaFiscalRepository(ViaPermanenteContext db) : INotaFiscalRepository
{
    public async Task<IReadOnlyList<NotaFiscal>> Notas(int idPedido)
        => await db.Set<NotaFiscal>().AsNoTracking().Where(n => n.IdPedido == idPedido).OrderBy(n => n.IdNota).ToListAsync();

    public async Task<IReadOnlyList<NotaFiscalItem>> Itens(IReadOnlyCollection<int> idsNota)
        => await db.Set<NotaFiscalItem>().AsNoTracking().Where(i => idsNota.Contains(i.IdNota)).OrderBy(i => i.IdNotaItem).ToListAsync();

    public Task<bool> NumeroEmUso(int idEmpresa, string numero)
        => db.Set<NotaFiscal>().AnyAsync(n => n.Numero == numero
            && db.Set<Pedido>().Any(p => p.IdPedido == n.IdPedido && p.IdEmpresa == idEmpresa));

    public async Task Criar(NotaFiscal nota, IReadOnlyList<NotaFiscalItem> itens)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(nota);
        await db.SaveChangesAsync();
        foreach (var item in itens)
            item.IdNota = nota.IdNota;
        db.AddRange(itens);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }
}