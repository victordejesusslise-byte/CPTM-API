using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>
/// O que um passo da troca (ou a correção da inspeção) muda no banco, gravado tudo junto numa transação:
/// a troca, as leituras, as movimentações, as peças, a recusa, a correção da inspeção e os alertas da peça que saiu.
/// </summary>
public class MudancasDaTroca
{
    /// <summary>a troca nova (no pedido).</summary>
    public Troca? TrocaNova { get; init; }
    /// <summary>a troca que mudou de passo.</summary>
    public Troca? TrocaAlterada { get; init; }
    /// <summary>as leituras de tag a gravar (→ LEITURA_DORMENTE).</summary>
    public List<LeituraDormente> Leituras { get; } = [];
    /// <summary>as movimentações da peça (separada, saída, devolvida), cada uma com a leitura que achou a peça.</summary>
    public List<(Movimentacao Movimentacao, LeituraDormente? Leitura)> Movimentacoes { get; } = [];
    /// <summary>o que muda em cada peça (pilha, trecho, km, retirada).</summary>
    public List<(int IdDormente, Action<Dormente> Mudar)> Pecas { get; } = [];
    /// <summary>a recusa nova da via.</summary>
    public RecusaTroca? RecusaNova { get; init; }
    /// <summary>a recusa com a decisão do técnico.</summary>
    public RecusaTroca? RecusaAlterada { get; init; }
    /// <summary>a inspeção corrigida.</summary>
    public Inspecao? InspecaoAlterada { get; init; }
    /// <summary>o registro da correção (antes e depois).</summary>
    public InspecaoCorrecao? Correcao { get; init; }
    /// <summary>Fecha (confirma) os alertas abertos desta peça: a troca feita fecha os alertas da peça que saiu.</summary>
    public (int IdDormente, int IdUsuario, DateTime Data)? FecharAlertas { get; init; }

    /// <summary>Junta as leituras, movimentações e mudanças de peça de outro conjunto a este.</summary>
    public MudancasDaTroca Com(MudancasDaTroca outra)
    {
        Leituras.AddRange(outra.Leituras);
        Movimentacoes.AddRange(outra.Movimentacoes);
        Pecas.AddRange(outra.Pecas);
        return this;
    }
}