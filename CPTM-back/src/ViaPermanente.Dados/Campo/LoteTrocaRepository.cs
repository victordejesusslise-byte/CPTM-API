using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dados.Campo;

public class LoteTrocaRepository(ViaPermanenteContext db) : ILoteTrocaRepository
{
    public async Task<IReadOnlyList<LoteTroca>> Listar(StatusLoteTroca? status = null, int? idLote = null)
        => await db.Set<LoteTroca>().AsNoTracking()
            .Where(l => (status == null || l.Status == status) && (idLote == null || l.IdLoteTroca == idLote))
            .OrderByDescending(l => l.DataPedido).ThenByDescending(l => l.IdLoteTroca)
            .ToListAsync();

    public async Task<IReadOnlyList<Movimentacao>> Movimentacoes(int idLote)
        => await db.Set<Movimentacao>().AsNoTracking().Where(m => m.IdLoteTroca == idLote)
            .OrderBy(m => m.IdMovimentacao).ToListAsync();

    public async Task<IReadOnlyList<RecusaTroca>> Recusas(int idLote)
        => await db.Set<RecusaTroca>().AsNoTracking().Where(r => r.IdLoteTroca == idLote).OrderBy(r => r.IdRecusa).ToListAsync();

    public async Task Gravar(MudancasDoLote m)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (m.LoteNovo is not null) db.Add(m.LoteNovo);
        if (m.LoteAlterado is not null) db.Update(m.LoteAlterado);
        foreach (var avulsa in m.AvulsasAlteradas) db.Update(avulsa);
        db.AddRange(m.Leituras);
        foreach (var (_, leitura) in m.Movimentacoes)
            if (leitura is not null) db.Add(leitura);
        foreach (var (troca, _, leitura) in m.TrocasFeitas)
        {
            db.Add(troca);
            db.Add(leitura);
        }
        await db.SaveChangesAsync();

        var idLote = (m.LoteNovo ?? m.LoteAlterado)!.IdLoteTroca;
        foreach (var (movimentacao, leitura) in m.Movimentacoes)
        {
            if (movimentacao.IdTroca is null) movimentacao.IdLoteTroca = idLote;
            movimentacao.IdLeitura = leitura?.IdLeitura;
            db.Add(movimentacao);
        }
        foreach (var (troca, saida, leitura) in m.TrocasFeitas)
        {
            (saida.IdTroca, saida.IdLoteTroca, saida.IdLeitura) = (troca.IdTroca, idLote, leitura.IdLeitura);
            db.Add(saida);
        }
        foreach (var (idDormente, mudar) in m.Pecas)
            await db.MudarPeca(idDormente, mudar);
        if (m.RecusaNova is not null)
        {
            m.RecusaNova.IdLoteTroca = idLote;
            db.Add(m.RecusaNova);
        }
        if (m.RecusaAlterada is not null) db.Update(m.RecusaAlterada);
        await db.SaveChangesAsync();

        foreach (var (idDormente, idUsuario, data) in m.FecharAlertas)
            await db.Set<Alerta>().Where(a => a.IdDormente == idDormente && a.Status == StatusAlerta.Aberto)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, StatusAlerta.Confirmado).SetProperty(a => a.IdUsuarioDecisao, idUsuario)
                    .SetProperty(a => a.DataDecisao, data));
        await transacao.CommitAsync();
    }
}
