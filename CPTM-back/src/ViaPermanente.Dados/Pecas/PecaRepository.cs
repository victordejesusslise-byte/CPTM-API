using System.Linq.Expressions;
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

    public async Task<PedidoDoFeixe?> PedidoDoFeixe(int idFeixe)
    {
        var pedido = await (
            from f in db.Set<Feixe>().AsNoTracking()
            join r in db.Set<Recebimento>() on f.IdRecebimento equals r.IdRecebimento
            join p in db.Set<Pedido>() on r.IdPedido equals p.IdPedido
            join e in db.Set<Empresa>() on p.IdEmpresa equals e.IdEmpresa
            where f.IdFeixe == idFeixe
            select new { p.IdPedido, p.Codigo, e.Sigla }).FirstOrDefaultAsync();
        if (pedido is null)
            return null;
        // As peças do pedido inteiro (todos os feixes e chegadas): a peça vem do lote, e o lote é do pedido.
        var cadastradas = await db.Set<Dormente>().CountAsync(d => db.Set<Lote>().Any(l => l.IdLote == d.IdLote && l.IdPedido == pedido.IdPedido));
        return new PedidoDoFeixe(pedido.IdPedido, pedido.Codigo, pedido.Sigla, cadastradas);
    }

    // Na GRAVACAO_TAG, a Ligada e a Desligada ficam com ok: o número delas também está gasto.
    public Task<bool> TagEmUso(string tag)
        => db.Set<GravacaoTag>().AnyAsync(g => g.TagGravada == tag && NumeroGasto.Contains(g.Resultado));

    public Task<string?> MaiorTag()
        => db.Set<GravacaoTag>().Where(g => NumeroGasto.Contains(g.Resultado)).MaxAsync(g => (string?)g.TagGravada);

    public Task<GravacaoTag?> GravacaoPorCliente(Guid idCliente)
        => db.Set<GravacaoTag>().AsNoTracking().FirstOrDefaultAsync(g => g.IdCliente == idCliente);

    public async Task RegistrarGravacao(GravacaoTag gravacao)
    {
        db.Add(gravacao);
        await db.SaveChangesAsync();
    }

    public async Task Cadastrar(Dormente dormente, GravacaoTag gravacao, ClassificacaoMadeira classificacao)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(dormente);
        await db.SaveChangesAsync();
        // O código da peça, logo que o número dela existe, na mesma transação.
        var idEmpresa = await db.Set<Lote>().Where(l => l.IdLote == dormente.IdLote).Select(l => l.IdEmpresa).SingleAsync();
        dormente.Codigo = Dormente.MontarCodigo(idEmpresa, dormente.IdLote, dormente.IdDormente);
        gravacao.IdDormente = dormente.IdDormente;
        classificacao.IdDormente = dormente.IdDormente;
        db.Add(gravacao);
        db.Add(classificacao);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }

    public Task<Dormente?> Buscar(int idDormente) => db.Set<Dormente>().AsNoTracking().FirstOrDefaultAsync(d => d.IdDormente == idDormente);

    public Task<int?> IdPeloCodigo(string codigo)
        => db.Set<Dormente>().Where(d => d.Codigo == codigo).Select(d => (int?)d.IdDormente).FirstOrDefaultAsync();

    /// <summary>A linha da GRAVACAO_TAG vista como a etiqueta da peça (o EF não filtra depois do construtor: filtre antes).</summary>
    private static readonly Expression<Func<GravacaoTag, TagDaPeca>> ComoEtiqueta = g => new TagDaPeca(g.IdGravacao, g.IdDormente!.Value, g.TagGravada, g.Vigente);

    public Task<TagDaPeca?> TagVigente(int idDormente)
        => db.Set<GravacaoTag>().AsNoTracking().Where(g => g.IdDormente == idDormente && g.Vigente).Select(ComoEtiqueta).FirstOrDefaultAsync();

    /// <summary>As etiquetas são a Gravada ok e a Ligada, com a peça; a Desligada é só o registro de que uma deixou de valer.</summary>
    public Task<TagDaPeca?> BuscarTag(string numero)
        => db.Set<GravacaoTag>().AsNoTracking()
            .Where(g => g.TagGravada == numero && g.IdDormente != null
                && (g.Tipo == TipoGravacao.Ligada || (g.Tipo == TipoGravacao.Gravada && g.Resultado == ResultadoGravacao.Ok)))
            .OrderByDescending(g => g.Vigente).ThenByDescending(g => g.IdGravacao)
            .Select(ComoEtiqueta).FirstOrDefaultAsync();

    public Task<VerificacaoQualidade?> Verificacao(int idDormente)
        => db.Set<VerificacaoQualidade>().AsNoTracking().OrderBy(v => v.IdVerificacao).FirstOrDefaultAsync(v => v.IdDormente == idDormente);

    public async Task RegistrarLeitura(LeituraDormente leitura)
    {
        db.Add(leitura);
        await db.SaveChangesAsync();
    }

    public async Task Verificar(VerificacaoQualidade verificacao, LeituraDormente leitura, GravacaoTag? desligada)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(leitura);
        db.Add(verificacao);
        await db.SaveChangesAsync();
        if (desligada is { IdDormente: int idDormente })
        {
            // A única mudança numa linha antiga da GRAVACAO_TAG: a marca vigente passa de 1 para 0.
            await db.Set<GravacaoTag>().Where(g => g.IdDormente == idDormente && g.Vigente).ExecuteUpdateAsync(s => s.SetProperty(g => g.Vigente, false));
            db.Add(desligada);
            await db.SaveChangesAsync();
        }
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

    public async Task GuardarVarias(IReadOnlyList<Movimentacao> entradas, DateTime entradaEm, LeituraDormente leitura)
    {
        // Uma transação só: o lote entra inteiro ou nada entra.
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(leitura);
        await db.SaveChangesAsync();
        // Só a movimentação da peça lida leva a leitura; as outras peças não tiveram a tag lida.
        foreach (var entrada in entradas)
            entrada.IdLeitura = entrada.IdDormente == leitura.IdDormente ? leitura.IdLeitura : null;
        db.AddRange(entradas);
        await db.SaveChangesAsync();
        // A pilha de agora fica na peça: uma atualização por pilha, com todas as peças que vão para ela.
        foreach (var pilha in entradas.GroupBy(e => e.IdPilhaDestino!.Value))
        {
            var ids = pilha.Select(e => e.IdDormente).ToList();
            await db.Set<Dormente>().Where(d => ids.Contains(d.IdDormente))
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IdPilha, pilha.Key).SetProperty(d => d.EntradaArmazemEm, entradaEm));
        }
        await transacao.CommitAsync();
    }

    public async Task<IReadOnlyList<int>> IdsDaLinha(DateTime? desde, int depoisDe, int limite)
    {
        var naVia = db.Set<Dormente>().AsNoTracking().Where(d => d.IdTrecho != null && d.IdDormente > depoisDe);
        naVia = desde is DateTime d0
            ? naVia.Where(d => d.DataAlteracao > d0)
            : naVia.Where(d => d.RetiradaEm == null && db.Set<GravacaoTag>().Any(g => g.IdDormente == d.IdDormente && g.Vigente));
        return await naVia.OrderBy(d => d.IdDormente).Select(d => d.IdDormente).Take(limite).ToListAsync();
    }

    public async Task<IReadOnlyList<PecaResumo>> Resumos(int? idDormente = null, int? idFeixe = null, bool soDisponiveis = false, IReadOnlyCollection<int>? ids = null, int? idLote = null)
    {
        var dormentes = db.Set<Dormente>().AsNoTracking()
            .Where(d => idDormente == null || d.IdDormente == idDormente)
            .Where(d => idFeixe == null || d.IdFeixe == idFeixe)
            .Where(d => idLote == null || d.IdLote == idLote);
        if (ids is not null)
            dormentes = dormentes.Where(d => ids.Contains(d.IdDormente));
        if (soDisponiveis)
            dormentes = dormentes.Where(d => d.RetiradaEm == null && d.IdTrecho == null
                && db.Set<GravacaoTag>().Any(g => g.IdDormente == d.IdDormente && g.Vigente)
                && db.Set<VerificacaoQualidade>().Any(v => v.IdDormente == d.IdDormente && v.Resultado == ResultadoVerificacao.Aprovada));

        // Filtra antes de montar o registro: o EF não filtra depois do construtor.
        return await (
            from d in dormentes.OrderBy(d => d.IdDormente)
            join f in db.Set<Feixe>() on d.IdFeixe equals f.IdFeixe
            join r in db.Set<Recebimento>() on f.IdRecebimento equals r.IdRecebimento
            join l in db.Set<Lote>() on d.IdLote equals l.IdLote
            select new PecaResumo(
                d.IdDormente, d.Codigo, d.IdLote, l.IdEmpresa, l.IdPedido,
                f.IdFeixe, f.NumeroFeixe, f.IdTamanho, r.IdArmazem,
                d.TagFornecedor, d.IdPilha, d.EntradaArmazemEm, d.RetiradaEm, d.IdTrecho, d.KmProjeto,
                db.Set<GravacaoTag>().Where(g => g.IdDormente == d.IdDormente && g.Vigente).Select(g => g.TagGravada).FirstOrDefault(),
                db.Set<ClassificacaoMadeira>().Where(c => c.IdDormente == d.IdDormente)
                    .OrderByDescending(c => c.IdClassificacao).Select(c => (int?)c.IdEspecie).FirstOrDefault() ?? f.IdEspecie,
                db.Set<VerificacaoQualidade>().Where(v => v.IdDormente == d.IdDormente)
                    .OrderBy(v => v.IdVerificacao).Select(v => (ResultadoVerificacao?)v.Resultado).FirstOrDefault(),
                f.Classe)
        ).ToListAsync();
    }
}