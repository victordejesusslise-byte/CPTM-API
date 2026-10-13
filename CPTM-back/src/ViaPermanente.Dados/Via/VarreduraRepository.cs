using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Via;

public class VarreduraRepository(ViaPermanenteContext db) : IVarreduraRepository
{
    public Task<bool> CodigoEmUso(string codigo) => db.Set<Passagem>().AnyAsync(p => p.Codigo == codigo);

    public async Task<IReadOnlyDictionary<string, int>> DonosDasTags(IReadOnlyCollection<string> tags)
        => await db.Set<Rfid>().AsNoTracking().Where(r => r.Ativo && tags.Contains(r.TagRfid)).ToDictionaryAsync(r => r.TagRfid, r => r.IdDormente);

    public async Task<IReadOnlyDictionary<int, PosicaoAtual>> Posicoes(IReadOnlyCollection<int> idsDormente)
        => await db.Set<PosicaoAtual>().AsNoTracking().Where(p => idsDormente.Contains(p.IdDormente)).ToDictionaryAsync(p => p.IdDormente);

    public async Task<IReadOnlyList<Alerta>> AlertasAbertos(IReadOnlyCollection<int> idsDormente)
        => await db.Set<Alerta>().AsNoTracking()
            .Where(a => a.Status == StatusAlerta.Aberto && a.IdDormente != null && idsDormente.Contains(a.IdDormente.Value))
            .ToListAsync();

    public async Task GravarPassagem(Passagem passagem, IReadOnlyList<LeituraVia> leituras, IReadOnlyList<DeteccaoImagem> deteccoes)
    {
        db.Add(passagem);
        await db.SaveChangesAsync();
        foreach (var l in leituras) l.IdPassagem = passagem.IdPassagem;
        foreach (var d in deteccoes) d.IdPassagem = passagem.IdPassagem;
        db.AddRange(leituras);
        db.AddRange(deteccoes);
        await db.SaveChangesAsync();
    }

    public async Task GravarResultado(IReadOnlyList<Alerta> alertas, IReadOnlyList<PosicaoAtual> posicoesNovas,
        IReadOnlyList<PosicaoAtual> posicoesAlteradas, IReadOnlyList<EventoPosicao> eventos)
    {
        db.AddRange(alertas);
        db.AddRange(posicoesNovas);
        db.UpdateRange(posicoesAlteradas);
        db.AddRange(eventos);
        await db.SaveChangesAsync();
    }

    public async Task EmTransacao(Func<Task> acao)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        await acao();
        await transacao.CommitAsync();
    }
}