using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Apresentacao;
using ViaPermanente.Dominio.Models.Chegada;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class ApresentacaoRepository(ViaPermanenteContext db) : IApresentacaoRepository
{
    public async Task<ContagemFluxo> Contar(int? idPedido)
    {
        var pedidos = db.Set<Pedido>().AsNoTracking().Where(p => idPedido == null || p.IdPedido == idPedido);
        var idsPedido = pedidos.Select(p => p.IdPedido);
        var itens = db.Set<PedidoItem>().Where(i => idsPedido.Contains(i.IdPedido));
        var recebimentos = db.Set<Recebimento>().Where(r => idsPedido.Contains(r.IdPedido));
        var idsRecebimento = recebimentos.Select(r => r.IdRecebimento);
        var feixes = db.Set<Feixe>().Where(f => idsRecebimento.Contains(f.IdRecebimento));
        var lotes = db.Set<Lote>().Where(l => idsPedido.Contains(l.IdPedido));
        var idsLote = lotes.Select(l => l.IdLote);
        var dormentes = db.Set<Dormente>().Where(d => idsLote.Contains(d.IdLote));
        var idsDormente = dormentes.Select(d => d.IdDormente);
        var tags = db.Set<Rfid>().Where(r => idsDormente.Contains(r.IdDormente));
        var verificacoes = db.Set<VerificacaoQualidade>().Where(v => idsDormente.Contains(v.IdDormente));

        return new ContagemFluxo(
            Pedidos: await pedidos.CountAsync(),
            ItensPedido: await itens.CountAsync(),
            PecasPedidas: await itens.SumAsync(i => (int?)i.Quantidade) ?? 0,
            Notas: await db.Set<NotaFiscal>().CountAsync(n => idsPedido.Contains(n.IdPedido)),
            Recebimentos: await recebimentos.CountAsync(),
            Lotes: await lotes.CountAsync(),
            Feixes: await feixes.CountAsync(),
            PecasNosFeixes: await feixes.SumAsync(f => (int?)f.Quantidade) ?? 0,
            Dormentes: await dormentes.CountAsync(),
            TagsAtivas: await tags.CountAsync(r => r.Ativo),
            TagsDesativadas: await tags.CountAsync(r => !r.Ativo),
            Classificacoes: await db.Set<ClassificacaoMadeira>().CountAsync(c => idsDormente.Contains(c.IdDormente)),
            Aprovadas: await verificacoes.CountAsync(v => v.Resultado == ResultadoVerificacao.Aprovada),
            Reprovadas: await verificacoes.CountAsync(v => v.Resultado == ResultadoVerificacao.Reprovada),
            Movimentacoes: await db.Set<Movimentacao>().CountAsync(m => idsDormente.Contains(m.IdDormente)),
            EmPilha: await dormentes.CountAsync(d => d.IdPilha != null));
    }

    public Task<T?> Primeiro<T>(Expression<Func<T, bool>> filtro) where T : class
        => db.Set<T>().FirstOrDefaultAsync(filtro);

    public async Task<T> Adicionar<T>(T entidade) where T : class
    {
        db.Add(entidade);
        await db.SaveChangesAsync();
        return entidade;
    }

    public async Task AdicionarVarios<T>(IReadOnlyList<T> entidades) where T : class
    {
        db.AddRange(entidades);
        await db.SaveChangesAsync();
    }

    public Task Salvar() => db.SaveChangesAsync();

    public Task<string?> UltimaTag()
        => db.Set<Rfid>().AsNoTracking().OrderByDescending(r => r.TagRfid).Select(r => r.TagRfid).FirstOrDefaultAsync();

    public async Task EmTransacao(Func<Task> acao)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        await acao();
        await transacao.CommitAsync();
    }
}