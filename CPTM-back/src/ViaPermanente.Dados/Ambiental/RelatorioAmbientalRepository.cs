using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Ambiental;
using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dados.Ambiental;

public class RelatorioAmbientalRepository(ViaPermanenteContext db) : IRelatorioAmbientalRepository
{
    /// <summary>As peças aprovadas, com o m³ de cada uma: comprimento do feixe × o padrão de medida do fornecedor que o feixe usa.</summary>
    public async Task<IReadOnlyList<PecaParaRelatorio>> Pecas()
        => (await (
            from d in db.Set<Dormente>().AsNoTracking()
            where db.Set<VerificacaoQualidade>().Any(v => v.IdDormente == d.IdDormente && v.Resultado == ResultadoVerificacao.Aprovada)
            join f in db.Set<Feixe>() on d.IdFeixe equals f.IdFeixe
            join m in db.Set<MedidaFornecedor>() on f.IdMedida equals m.IdMedida
            select new
            {
                IdEspecie = db.Set<ClassificacaoMadeira>().Where(c => c.IdDormente == d.IdDormente).OrderByDescending(c => c.IdClassificacao)
                    .Select(c => (int?)c.IdEspecie).FirstOrDefault() ?? f.IdEspecie,
                f.IdTamanho, m.LarguraM, m.AlturaM, d.EntradaArmazemEm,
                Saida = db.Set<Movimentacao>().Where(x => x.IdDormente == d.IdDormente && x.Tipo == TipoMovimentacao.Saida)
                    .OrderBy(x => x.Data).Select(x => (DateTime?)x.Data).FirstOrDefault()
            }).ToListAsync())
            .Select(p => new PecaParaRelatorio(p.IdEspecie, VolumeHelper.M3(TamanhoDormente.Comprimento(p.IdTamanho), p.LarguraM, p.AlturaM), p.EntradaArmazemEm, p.Saida))
            .ToList();

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