using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dados.Chegadas;

public class LeituraOcrRepository(ViaPermanenteContext db) : ILeituraOcrRepository
{
    public async Task<IReadOnlyList<ModeloOcr>> Modelos() => await db.Set<ModeloOcr>().AsNoTracking().OrderBy(m => m.Versao).ToListAsync();

    public async Task Gravar(LeituraOcr leitura, IReadOnlyList<OcrCampo> campos)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(leitura);
        await db.SaveChangesAsync();
        foreach (var c in campos) c.IdLeituraOcr = leitura.IdLeituraOcr;
        db.AddRange(campos);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    public Task<LeituraOcr?> Buscar(int idLeitura) => db.Set<LeituraOcr>().AsNoTracking().FirstOrDefaultAsync(l => l.IdLeituraOcr == idLeitura);

    public async Task<IReadOnlyList<OcrCampo>> Campos(int idLeitura) => await db.Set<OcrCampo>().AsNoTracking().Where(c => c.IdLeituraOcr == idLeitura).ToListAsync();

    public async Task<IReadOnlyList<LeituraOcr>> PlacasSemFeixe(int idPedido)
        => await db.Set<LeituraOcr>().AsNoTracking()
            .Where(l => l.IdPedido == idPedido && l.IdFeixe == null
                && db.Set<OcrCampo>().Any(c => c.IdLeituraOcr == l.IdLeituraOcr && c.ValorConfirmado != null))
            .OrderBy(l => l.Data).ToListAsync();

    public async Task Confirmar(int idLeitura, IReadOnlyList<(int IdCampo, string Valor, bool Corrigido)> confirmacoes, int? idRecebimento, int? idFeixe)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        foreach (var (idCampo, valor, corrigido) in confirmacoes)
            await db.Set<OcrCampo>().Where(c => c.IdCampo == idCampo && c.ValorConfirmado == null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ValorConfirmado, valor).SetProperty(c => c.Corrigido, corrigido));
        if (idRecebimento is not null || idFeixe is not null)
            await db.Set<LeituraOcr>().Where(l => l.IdLeituraOcr == idLeitura)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.IdRecebimento, l => idRecebimento ?? l.IdRecebimento).SetProperty(l => l.IdFeixe, l => idFeixe ?? l.IdFeixe));
        await transacao.CommitAsync();
    }
}