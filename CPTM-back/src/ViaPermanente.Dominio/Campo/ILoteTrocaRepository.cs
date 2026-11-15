using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>O acesso ao banco dos lotes de troca (LOTE_TROCA). Usado pelo LoteTrocaService.</summary>
public interface ILoteTrocaRepository
{
    /// <summary>Os lotes, filtrados pelo status ou por um id.</summary>
    Task<IReadOnlyList<LoteTroca>> Listar(StatusLoteTroca? status = null, int? idLote = null);
    /// <summary>As movimentações do lote (separada, saída na troca, devolvida), na ordem: a última de cada peça diz onde ela está.</summary>
    Task<IReadOnlyList<Movimentacao>> Movimentacoes(int idLote);
    /// <summary>As recusas do lote pela via, da mais antiga para a mais nova.</summary>
    Task<IReadOnlyList<RecusaTroca>> Recusas(int idLote);
    /// <summary>Grava tudo o que o passo muda, numa transação.</summary>
    Task Gravar(MudancasDoLote mudancas);
}
