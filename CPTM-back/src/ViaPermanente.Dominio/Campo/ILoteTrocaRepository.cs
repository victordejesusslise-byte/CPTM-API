using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo;

public interface ILoteTrocaRepository
{
    Task<IReadOnlyList<LoteTroca>> Listar(StatusLoteTroca? status = null, int? idLote = null);
    /// <summary>As movimentações do lote (separada, saída na troca, devolvida), na ordem: a última de cada peça diz onde ela está.</summary>
    Task<IReadOnlyList<Movimentacao>> Movimentacoes(int idLote);
    Task<IReadOnlyList<RecusaTroca>> Recusas(int idLote);
    /// <summary>Grava tudo o que o passo muda, numa transação.</summary>
    Task Gravar(MudancasDoLote mudancas);
}
