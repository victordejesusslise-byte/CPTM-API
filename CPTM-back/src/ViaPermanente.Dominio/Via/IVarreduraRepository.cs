using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Via;

public interface IVarreduraRepository
{
    Task<bool> CodigoEmUso(string codigo);
    /// <summary>A peça dona de cada tag ativa da lista.</summary>
    Task<IReadOnlyDictionary<string, int>> DonosDasTags(IReadOnlyCollection<string> tags);
    Task<IReadOnlyDictionary<int, PosicaoAtual>> Posicoes(IReadOnlyCollection<int> idsDormente);
    /// <summary>Os alertas abertos das peças (para não abrir outro igual).</summary>
    Task<IReadOnlyList<Alerta>> AlertasAbertos(IReadOnlyCollection<int> idsDormente);
    /// <summary>Grava a passagem, as leituras e as detecções; cada uma volta com o id.</summary>
    Task GravarPassagem(Passagem passagem, IReadOnlyList<LeituraVia> leituras, IReadOnlyList<DeteccaoImagem> deteccoes);
    Task GravarResultado(IReadOnlyList<Alerta> alertas, IReadOnlyList<PosicaoAtual> posicoesNovas,
        IReadOnlyList<PosicaoAtual> posicoesAlteradas, IReadOnlyList<EventoPosicao> eventos);
    Task EmTransacao(Func<Task> acao);
}