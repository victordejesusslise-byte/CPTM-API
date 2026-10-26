using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dados.Pecas;

public class PecaRepository(ViaPermanenteContext db) : IPecaRepository
{
    private static readonly ResultadoGravacao[] NumeroGasto = [ResultadoGravacao.Ok, ResultadoGravacao.Divergente];

    public Task<Feixe?> BuscarFeixe(int idFeixe) => db.Set<Feixe>().AsNoTracking().FirstOrDefaultAsync(f => f.IdFeixe == idFeixe);

    public Task<Recebimento?> BuscarRecebimento(int idRecebimento)
        => db.Set<Recebimento>().AsNoTracking().FirstOrDefaultAsync(r => r.IdRecebimento == idRecebimento);

    public Task<int> PecasNoFeixe(int idFeixe) => db.Set<Dormente>().CountAsync(d => d.IdFeixe == idFeixe);

    public Task<Lote?> LoteDoPedido(int idPedido)
        => db.Set<Lote>().AsNoTracking().OrderBy(l => l.IdLote).FirstOrDefaultAsync(l => l.IdPedido == idPedido);

    public async Task<bool> TagEmUso(string tag)
        => await db.Set<Rfid>().AnyAsync(r => r.TagRfid == tag)
           || await db.Set<GravacaoTag>().AnyAsync(g => g.TagGravada == tag && NumeroGasto.Contains(g.Resultado));

    public async Task<string?> MaiorTag()
    {
        var nasTags = await db.Set<Rfid>().MaxAsync(r => (string?)r.TagRfid);
        var nasGravacoes = await db.Set<GravacaoTag>().Where(g => NumeroGasto.Contains(g.Resultado)).MaxAsync(g => (string?)g.TagGravada);
        return string.CompareOrdinal(nasTags, nasGravacoes) >= 0 ? nasTags : nasGravacoes;
    }

    public Task<GravacaoTag?> GravacaoPorCliente(Guid idCliente)
        => db.Set<GravacaoTag>().AsNoTracking().FirstOrDefaultAsync(g => g.IdCliente == idCliente);

    public async Task RegistrarGravacao(GravacaoTag gravacao)
    {
        db.Add(gravacao);
        await db.SaveChangesAsync();
    }

    public async Task Cadastrar(Dormente dormente, Rfid tag, ClassificacaoMadeira classificacao, GravacaoTag? gravacao, LeituraDormente? leitura)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(dormente);
        await db.SaveChangesAsync();
        tag.IdDormente = dormente.IdDormente;
        classificacao.IdDormente = dormente.IdDormente;
        db.Add(tag);
        db.Add(classificacao);
        await db.SaveChangesAsync();
        if (gravacao is not null)
        {
            gravacao.IdRfid = tag.IdRfid;
            db.Add(gravacao);
        }
        if (leitura is not null)
        {
            leitura.IdDormente = dormente.IdDormente;
            db.Add(leitura);
        }
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    public Task<Dormente?> Buscar(int idDormente) => db.Set<Dormente>().AsNoTracking().FirstOrDefaultAsync(d => d.IdDormente == idDormente);

    public Task<Rfid?> TagAtiva(int idDormente)
        => db.Set<Rfid>().AsNoTracking().FirstOrDefaultAsync(r => r.IdDormente == idDormente && r.Ativo);

    public Task<Rfid?> BuscarTag(string numero)
        => db.Set<Rfid>().AsNoTracking().OrderByDescending(r => r.Ativo).FirstOrDefaultAsync(r => r.TagRfid == numero);

    public Task<VerificacaoQualidade?> Verificacao(int idDormente)
        => db.Set<VerificacaoQualidade>().AsNoTracking().OrderBy(v => v.IdVerificacao).FirstOrDefaultAsync(v => v.IdDormente == idDormente);

    public async Task RegistrarLeitura(LeituraDormente leitura)
    {
        db.Add(leitura);
        await db.SaveChangesAsync();
    }

    public async Task Verificar(VerificacaoQualidade verificacao, LeituraDormente leitura, int? idRfidDesativar)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(leitura);
        db.Add(verificacao);
        await db.SaveChangesAsync();
        if (idRfidDesativar is int idRfid)
            await db.Set<Rfid>().Where(r => r.IdRfid == idRfid).ExecuteUpdateAsync(s => s.SetProperty(r => r.Ativo, false));
        await transacao.CommitAsync();
    }

    public async Task Guardar(int idDormente, int idPilha, DateTime entradaEm, Movimentacao movimentacao, LeituraDormente leitura)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(leitura);
        await db.SaveChangesAsync();
        movimentacao.IdLeitura = leitura.IdLeitura;
        db.Add(movimentacao);
        await db.SaveChangesAsync();
        await db.Set<Dormente>().Where(d => d.IdDormente == idDormente)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IdPilha, idPilha).SetProperty(d => d.EntradaArmazemEm, entradaEm));
        await transacao.CommitAsync();
    }

    public async Task<IReadOnlyList<int>> IdsDaLinha(DateTime? desde, int depoisDe, int limite)
    {
        var naVia = db.Set<Dormente>().AsNoTracking().Where(d => d.IdTrecho != null && d.IdDormente > depoisDe);
        naVia = desde is DateTime d0
            ? naVia.Where(d => d.DataAlteracao > d0)
            : naVia.Where(d => d.RetiradaEm == null && db.Set<Rfid>().Any(t => t.IdDormente == d.IdDormente && t.Ativo));
        return await naVia.OrderBy(d => d.IdDormente).Select(d => d.IdDormente).Take(limite).ToListAsync();
    }

    public async Task<IReadOnlyList<PecaResumo>> Resumos(int? idDormente = null, int? idFeixe = null, bool soDisponiveis = false, IReadOnlyCollection<int>? ids = null)
    {
        var dormentes = db.Set<Dormente>().AsNoTracking()
            .Where(d => idDormente == null || d.IdDormente == idDormente)
            .Where(d => idFeixe == null || d.IdFeixe == idFeixe);
        if (ids is not null)
            dormentes = dormentes.Where(d => ids.Contains(d.IdDormente));
        if (soDisponiveis)
            dormentes = dormentes.Where(d => d.RetiradaEm == null && d.IdTrecho == null
                && db.Set<Rfid>().Any(t => t.IdDormente == d.IdDormente && t.Ativo)
                && db.Set<VerificacaoQualidade>().Any(v => v.IdDormente == d.IdDormente && v.Resultado == ResultadoVerificacao.Aprovada));

        // Filtra antes de montar o registro: o EF não filtra depois do construtor.
        return await (
            from d in dormentes.OrderBy(d => d.IdDormente)
            join f in db.Set<Feixe>() on d.IdFeixe equals f.IdFeixe
            join r in db.Set<Recebimento>() on f.IdRecebimento equals r.IdRecebimento
            join l in db.Set<Lote>() on d.IdLote equals l.IdLote
            select new PecaResumo(
                d.IdDormente, d.IdLote, l.IdEmpresa, l.IdPedido,
                f.IdFeixe, f.NumeroFeixe, f.IdTamanho, r.IdArmazem,
                d.TagFornecedor, d.IdPilha, d.EntradaArmazemEm, d.RetiradaEm, d.IdTrecho, d.KmProjeto,
                db.Set<Rfid>().Where(t => t.IdDormente == d.IdDormente && t.Ativo).Select(t => t.TagRfid).FirstOrDefault(),
                db.Set<ClassificacaoMadeira>().Where(c => c.IdDormente == d.IdDormente)
                    .OrderByDescending(c => c.IdClassificacao).Select(c => (int?)c.IdEspecie).FirstOrDefault() ?? f.IdEspecie,
                db.Set<VerificacaoQualidade>().Where(v => v.IdDormente == d.IdDormente)
                    .OrderBy(v => v.IdVerificacao).Select(v => (ResultadoVerificacao?)v.Resultado).FirstOrDefault(),
                f.Classe)
        ).ToListAsync();
    }
}