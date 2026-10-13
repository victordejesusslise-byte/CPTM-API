using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dados.Pedidos;

public class PedidoRepository(ViaPermanenteContext db) : IPedidoRepository
{
    public Task<Empresa?> BuscarEmpresa(int idEmpresa)
        => db.Set<Empresa>().AsNoTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);

    public Task<Contrato?> BuscarContrato(int idContrato)
        => db.Set<Contrato>().AsNoTracking().FirstOrDefaultAsync(c => c.IdContrato == idContrato);

    public Task<bool> CodigoEmUso(string codigo) => db.Set<Pedido>().AnyAsync(p => p.Codigo == codigo);

    public Task<Pedido?> BuscarPorCodigo(string codigo) => db.Set<Pedido>().AsNoTracking().FirstOrDefaultAsync(p => p.Codigo == codigo);

    public async Task Criar(Pedido pedido, IReadOnlyList<PedidoItem> itens)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(pedido);
        await db.SaveChangesAsync();
        foreach (var item in itens)
            item.IdPedido = pedido.IdPedido;
        db.AddRange(itens);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    public async Task<IReadOnlyList<Pedido>> Listar(int? idPedido = null)
        => await db.Set<Pedido>().AsNoTracking()
            .Where(p => idPedido == null || p.IdPedido == idPedido)
            .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.IdPedido)
            .ToListAsync();

    public async Task<IReadOnlyList<PedidoItem>> Itens(IReadOnlyCollection<int> idsPedido)
        => await db.Set<PedidoItem>().AsNoTracking()
            .Where(i => idsPedido.Contains(i.IdPedido))
            .OrderBy(i => i.IdPedidoItem)
            .ToListAsync();

    public async Task<IReadOnlyDictionary<int, int>> PecasRecebidas(IReadOnlyCollection<int> idsPedido)
        => await db.Set<Recebimento>().AsNoTracking()
            .Where(r => idsPedido.Contains(r.IdPedido))
            .GroupBy(r => r.IdPedido)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(r => r.QuantidadeRecebida));

    public Task<bool> TemRecebimento(int idPedido) => db.Set<Recebimento>().AnyAsync(r => r.IdPedido == idPedido);

    public async Task<IReadOnlyDictionary<int, string>> NomesEmpresas(IReadOnlyCollection<int> ids)
        => await db.Set<Empresa>().AsNoTracking().Where(e => ids.Contains(e.IdEmpresa)).ToDictionaryAsync(e => e.IdEmpresa, e => e.Nome);

    public async Task<IReadOnlyDictionary<int, string>> NumerosContratos(IReadOnlyCollection<int> ids)
        => await db.Set<Contrato>().AsNoTracking().Where(c => ids.Contains(c.IdContrato)).ToDictionaryAsync(c => c.IdContrato, c => c.NumeroContrato);

    public async Task Corrigir(Pedido pedido, IReadOnlyList<PedidoItem> itens, PedidoHistorico historico)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        await db.Set<PedidoItem>().Where(i => i.IdPedido == pedido.IdPedido).ExecuteDeleteAsync();
        await db.Set<Pedido>().Where(p => p.IdPedido == pedido.IdPedido).ExecuteUpdateAsync(s => s
            .SetProperty(p => p.IdContrato, pedido.IdContrato).SetProperty(p => p.DataCompra, pedido.DataCompra).SetProperty(p => p.Quantidade, pedido.Quantidade));
        foreach (var item in itens)
            item.IdPedido = pedido.IdPedido;
        db.AddRange(itens);
        db.Add(historico);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    public async Task<IReadOnlyList<PedidoHistorico>> Historico(int idPedido)
        => await db.Set<PedidoHistorico>().AsNoTracking().Where(h => h.IdPedido == idPedido).OrderBy(h => h.IdHistorico).ToListAsync();

    public async Task Cancelar(int idPedido, PedidoHistorico historico)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        await db.Set<Pedido>().Where(p => p.IdPedido == idPedido)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, StatusPedido.Cancelado));
        db.Add(historico);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }
}