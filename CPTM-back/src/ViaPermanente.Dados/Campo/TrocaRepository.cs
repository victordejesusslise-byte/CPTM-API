using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Campo;

public class TrocaRepository(ViaPermanenteContext db) : ITrocaRepository
{
    private static readonly StatusTroca[] Abertas = [StatusTroca.Pedida, StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada, StatusTroca.Recusada];

    public async Task<IReadOnlyList<Troca>> Listar(StatusTroca? status = null, int? idTroca = null, int? idDormenteSaiu = null)
        => await db.Set<Troca>().AsNoTracking()
            .Where(t => (status == null || t.Status == status) && (idTroca == null || t.IdTroca == idTroca) && (idDormenteSaiu == null || t.IdDormenteSaiu == idDormenteSaiu))
            .OrderByDescending(t => t.DataPedido).ThenByDescending(t => t.IdTroca)
            .ToListAsync();

    public async Task<IReadOnlyCollection<int>> PecasReservadas()
        => await db.Set<Troca>().AsNoTracking().Where(t => t.IdDormenteEntrou != null && Abertas.Contains(t.Status))
            .Select(t => t.IdDormenteEntrou!.Value).ToListAsync();

    public Task<Trecho?> BuscarTrecho(int idTrecho) => db.Set<Trecho>().AsNoTracking().FirstOrDefaultAsync(t => t.IdTrecho == idTrecho);

    public async Task<IReadOnlyList<RecusaTroca>> Recusas(IReadOnlyCollection<int> idsTroca)
        => await db.Set<RecusaTroca>().AsNoTracking().Where(r => idsTroca.Contains(r.IdTroca)).OrderBy(r => r.IdRecusa).ToListAsync();

    public Task<int?> PilhaDaSeparacao(int idTroca, int idDormente)
        => db.Set<Movimentacao>().AsNoTracking()
            .Where(m => m.IdTroca == idTroca && m.IdDormente == idDormente && m.Tipo == TipoMovimentacao.Separada)
            .OrderByDescending(m => m.IdMovimentacao).Select(m => m.IdPilhaOrigem).FirstOrDefaultAsync();

    public async Task Gravar(MudancasDaTroca m)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (m.TrocaNova is not null) db.Add(m.TrocaNova);
        if (m.TrocaAlterada is not null) db.Update(m.TrocaAlterada);
        if (m.InspecaoAlterada is not null) db.Update(m.InspecaoAlterada);
        db.AddRange(m.Leituras);
        foreach (var (_, leitura) in m.Movimentacoes)
            if (leitura is not null) db.Add(leitura);
        await db.SaveChangesAsync();

        var idTroca = m.TrocaNova?.IdTroca ?? m.TrocaAlterada?.IdTroca;
        foreach (var (movimentacao, leitura) in m.Movimentacoes)
        {
            movimentacao.IdTroca ??= idTroca;
            movimentacao.IdLeitura = leitura?.IdLeitura;
            db.Add(movimentacao);
        }
        foreach (var (idDormente, mudar) in m.Pecas)
            mudar((await db.Set<Dormente>().FindAsync(idDormente))!);
        if (m.RecusaNova is not null) db.Add(m.RecusaNova);
        if (m.RecusaAlterada is not null) db.Update(m.RecusaAlterada);
        if (m.Correcao is not null) db.Add(m.Correcao);
        await db.SaveChangesAsync();

        if (m.FecharAlertas is { } fechar)
            await db.Set<Alerta>().Where(a => a.IdDormente == fechar.IdDormente && a.Status == StatusAlerta.Aberto)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, StatusAlerta.Confirmado).SetProperty(a => a.IdUsuarioDecisao, fechar.IdUsuario)
                    .SetProperty(a => a.DataDecisao, fechar.Data));
        await transacao.CommitAsync();
    }
}