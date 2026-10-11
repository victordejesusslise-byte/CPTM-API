using ViaPermanente.Dominio.Models.Campo;
using ViaPermanente.Dominio.Models.Via;

namespace ViaPermanente.Dominio.Repositories;

public interface ITrocaRepository
{
    Task<IReadOnlyList<Troca>> Listar(StatusTroca? status = null, int? idTroca = null, int? idDormenteSaiu = null);
    /// <summary>As peças separadas para trocas abertas (não estão mais disponíveis).</summary>
    Task<IReadOnlyCollection<int>> PecasReservadas();
    Task<Trecho?> BuscarTrecho(int idTrecho);
    Task<IReadOnlyList<RecusaTroca>> Recusas(IReadOnlyCollection<int> idsTroca);
    /// <summary>A pilha de onde a peça saiu na última separação para esta troca.</summary>
    Task<int?> PilhaDaSeparacao(int idTroca, int idDormente);
    /// <summary>Grava tudo o que o passo muda, numa transação.</summary>
    Task Gravar(MudancasDaTroca mudancas);
}