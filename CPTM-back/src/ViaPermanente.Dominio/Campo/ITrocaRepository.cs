using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>O acesso ao banco das trocas (TROCA) e das recusas (RECUSA_TROCA). Usado pelo TrocaService e pelo lote de troca.</summary>
public interface ITrocaRepository
{
    /// <summary>As trocas, filtradas pelo status, pelo id, pela peça que sai ou pelo lote.</summary>
    Task<IReadOnlyList<Troca>> Listar(StatusTroca? status = null, int? idTroca = null, int? idDormenteSaiu = null, int? idLoteTroca = null);
    /// <summary>As peças separadas para trocas abertas (não estão mais disponíveis).</summary>
    Task<IReadOnlyCollection<int>> PecasReservadas();
    /// <summary>O trecho pelo id (dá a classe exigida); nulo se não existe.</summary>
    Task<Trecho?> BuscarTrecho(int idTrecho);
    /// <summary>As recusas das trocas, da mais antiga para a mais nova.</summary>
    Task<IReadOnlyList<RecusaTroca>> Recusas(IReadOnlyCollection<int> idsTroca);
    /// <summary>A pilha de onde a peça saiu na última separação para esta troca.</summary>
    Task<int?> PilhaDaSeparacao(int idTroca, int idDormente);
    /// <summary>Grava tudo o que o passo muda, numa transação.</summary>
    Task Gravar(MudancasDaTroca mudancas);
}