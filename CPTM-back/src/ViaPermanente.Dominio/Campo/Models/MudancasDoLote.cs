using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>
/// O que um passo do lote de troca muda no banco, gravado tudo junto numa transação: o lote, as movimentações do lote (separada, devolvida),
/// as trocas feitas no lote (cada uma com a saída da peça nova e a leitura dela), as leituras das peças que saem, as peças, os pedidos
/// avulsos fechados pelo lote, a recusa e os alertas das peças que saíram.
/// </summary>
public class MudancasDoLote
{
    public LoteTroca? LoteNovo { get; init; }
    public LoteTroca? LoteAlterado { get; init; }
    /// <summary>Sem IdTroca, a movimentação é do lote; com IdTroca, é de um pedido avulso que o lote fechou.</summary>
    public List<(Movimentacao Movimentacao, LeituraDormente? Leitura)> Movimentacoes { get; } = [];
    public List<(Troca Troca, Movimentacao Saida, LeituraDormente LeituraEntrou)> TrocasFeitas { get; } = [];
    public List<LeituraDormente> Leituras { get; } = [];
    public List<(int IdDormente, Action<Dormente> Mudar)> Pecas { get; } = [];
    public List<Troca> AvulsasAlteradas { get; } = [];
    public RecusaTroca? RecusaNova { get; init; }
    /// <summary>As recusas das obrigatórias que não deu para trocar no lote (da troca avulsa, não do lote).</summary>
    public List<RecusaTroca> RecusasDeAvulsas { get; } = [];
    public RecusaTroca? RecusaAlterada { get; init; }
    public List<(int IdDormente, int IdUsuario, DateTime Data)> FecharAlertas { get; } = [];
}
