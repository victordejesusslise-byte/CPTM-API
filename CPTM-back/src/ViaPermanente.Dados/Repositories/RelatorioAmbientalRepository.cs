using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Ambiental;
using ViaPermanente.Dominio.Models.Catalogos;
using ViaPermanente.Dominio.Models.Chegada;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class RelatorioAmbientalRepository(ViaPermanenteContext db) : IRelatorioAmbientalRepository
{
    public async Task<IReadOnlyList<PecaParaRelatorio>> Pecas()
        => await (
            from d in db.Set<Dormente>().AsNoTracking()
            where db.Set<VerificacaoQualidade>().Any(v => v.IdDormente == d.IdDormente && v.Resultado == ResultadoVerificacao.Aprovada)
            join f in db.Set<Feixe>() on d.IdFeixe equals f.IdFeixe
            join t in db.Set<TamanhoDormente>() on f.IdTamanho equals t.IdTamanho
            select new PecaParaRelatorio(
                db.Set<ClassificacaoMadeira>().Where(c => c.IdDormente == d.IdDormente).OrderByDescending(c => c.IdClassificacao)
                    .Select(c => (int?)c.IdEspecie).FirstOrDefault() ?? f.IdEspecie,
                Math.Round(t.ComprimentoM * t.LarguraM * t.AlturaM, 5),
                d.EntradaArmazemEm,
                db.Set<Movimentacao>().Where(m => m.IdDormente == d.IdDormente && m.Tipo == TipoMovimentacao.Saida)
                    .OrderBy(m => m.Data).Select(m => (DateTime?)m.Data).FirstOrDefault())
        ).ToListAsync();

    public async Task<IReadOnlyList<RelatorioAmbiental>> Listar(int? idRelatorio = null, DateOnly? mes = null)
        => await db.Set<RelatorioAmbiental>().AsNoTracking()
            .Where(r => (idRelatorio == null || r.IdRelatorio == idRelatorio) && (mes == null || r.MesReferencia == mes))
            .OrderByDescending(r => r.MesReferencia).ThenByDescending(r => r.IdRelatorio).ToListAsync();

    public async Task<IReadOnlyList<RelatorioAmbientalItem>> Itens(IReadOnlyCollection<int> idsRelatorio)
        => await db.Set<RelatorioAmbientalItem>().AsNoTracking().Where(i => idsRelatorio.Contains(i.IdRelatorio)).OrderBy(i => i.IdEspecie).ToListAsync();

    public async Task Gravar(RelatorioAmbiental relatorio, IReadOnlyList<RelatorioAmbientalItem> itens, int? idRascunhoAnterior)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (idRascunhoAnterior is int idAnterior)
        {
            await db.Set<RelatorioAmbientalItem>().Where(i => i.IdRelatorio == idAnterior).ExecuteDeleteAsync();
            await db.Set<RelatorioAmbiental>().Where(r => r.IdRelatorio == idAnterior && r.Status == StatusRelatorio.Rascunho).ExecuteDeleteAsync();
        }
        db.Add(relatorio);
        await db.SaveChangesAsync();
        foreach (var item in itens) item.IdRelatorio = relatorio.IdRelatorio;
        db.AddRange(itens);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    public Task Enviar(int idRelatorio, DateTime enviadoEm)
        => db.Set<RelatorioAmbiental>().Where(r => r.IdRelatorio == idRelatorio && r.Status == StatusRelatorio.Rascunho)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, StatusRelatorio.Enviado).SetProperty(r => r.EnviadoEm, enviadoEm));
}