using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>
/// O que um passo do lote de troca muda no banco, gravado tudo junto numa transação: o lote, as movimentações,
/// as trocas feitas, as leituras, as peças, os pedidos avulsos fechados pelo lote, as recusas e os alertas das peças que saíram.
/// </summary>
public class MudancasDoLote
{
    /// <summary>o lote novo (no pedido).</summary>
    public LoteTroca? LoteNovo { get; init; }
    /// <summary>o lote que mudou de passo.</summary>
    public LoteTroca? LoteAlterado { get; init; }
    /// <summary>Sem IdTroca, a movimentação é do lote; com IdTroca, é de um pedido avulso que o lote fechou.</summary>
    public List<(Movimentacao Movimentacao, LeituraDormente? Leitura)> Movimentacoes { get; } = [];
    /// <summary>as trocas feitas no lote: a troca, a saída da peça nova do armazém e a leitura dela.</summary>
    public List<(Troca Troca, Movimentacao Saida, LeituraDormente LeituraEntrou)> TrocasFeitas { get; } = [];
    /// <summary>as leituras das peças que saem da via.</summary>
    public List<LeituraDormente> Leituras { get; } = [];
    /// <summary>o que muda em cada peça (pilha, trecho, km, retirada).</summary>
    public List<(int IdDormente, Action<Dormente> Mudar)> Pecas { get; } = [];
    /// <summary>os pedidos avulsos que o lote fechou ou recusou.</summary>
    public List<Troca> AvulsasAlteradas { get; } = [];
    /// <summary>a recusa nova do lote pela via.</summary>
    public RecusaTroca? RecusaNova { get; init; }
    /// <summary>As recusas das obrigatórias que não deu para trocar no lote (da troca avulsa, não do lote).</summary>
    public List<RecusaTroca> RecusasDeAvulsas { get; } = [];
    /// <summary>a recusa do lote com a decisão do técnico.</summary>
    public RecusaTroca? RecusaAlterada { get; init; }
    /// <summary>as peças que saíram: os alertas abertos delas fecham.</summary>
    public List<(int IdDormente, int IdUsuario, DateTime Data)> FecharAlertas { get; } = [];
}
