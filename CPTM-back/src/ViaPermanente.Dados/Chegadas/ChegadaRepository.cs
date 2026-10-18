using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dados.Chegadas;

public class ChegadaRepository(ViaPermanenteContext db) : IChegadaRepository
{
    public async Task<IReadOnlyList<Recebimento>> Listar(int idPedido)
        => await db.Set<Recebimento>().AsNoTracking().Where(r => r.IdPedido == idPedido).OrderBy(r => r.IdRecebimento).ToListAsync();

    public async Task<IReadOnlyList<Feixe>> Feixes(IReadOnlyCollection<int> idsRecebimento)
        => await db.Set<Feixe>().AsNoTracking().Where(f => idsRecebimento.Contains(f.IdRecebimento)).OrderBy(f => f.IdFeixe).ToListAsync();

    public async Task<IReadOnlyDictionary<int, int>> PecasCadastradas(IReadOnlyCollection<int> idsFeixe)
        => await db.Set<Dormente>().AsNoTracking()
            .Where(d => d.IdFeixe != null && idsFeixe.Contains(d.IdFeixe.Value))
            .GroupBy(d => d.IdFeixe!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

    public Task<Lote?> LoteDoPedido(int idPedido)
        => db.Set<Lote>().AsNoTracking().OrderBy(l => l.IdLote).FirstOrDefaultAsync(l => l.IdPedido == idPedido);

    public async Task Registrar(Recebimento recebimento, Lote? loteNovo, IReadOnlyList<Feixe> feixes, IReadOnlyList<int?> placas)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (loteNovo is not null)
            db.Add(loteNovo);
        db.Add(recebimento);
        await db.SaveChangesAsync();
        foreach (var feixe in feixes)
            feixe.IdRecebimento = recebimento.IdRecebimento;
        db.AddRange(feixes);
        await db.SaveChangesAsync();
        for (var i = 0; i < feixes.Count; i++)
        {
            if (placas.ElementAtOrDefault(i) is not int idLeitura) continue;
            var (idRecebimento, idFeixe) = (recebimento.IdRecebimento, feixes[i].IdFeixe);
            await db.Set<LeituraOcr>().Where(l => l.IdLeituraOcr == idLeitura)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.IdRecebimento, idRecebimento).SetProperty(l => l.IdFeixe, idFeixe));
        }
        await transacao.CommitAsync();
    }
}