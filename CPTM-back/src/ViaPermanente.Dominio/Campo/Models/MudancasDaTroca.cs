using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>
/// O que um passo da troca (ou a correção da inspeção) muda no banco, gravado tudo junto numa transação:
/// a troca, as leituras, as movimentações (cada uma com a leitura que identificou a peça), as peças, a recusa,
/// a correção da inspeção e os alertas da peça que saiu.
/// </summary>
public class MudancasDaTroca
{
    public Troca? TrocaNova { get; init; }
    public Troca? TrocaAlterada { get; init; }
    public List<LeituraDormente> Leituras { get; } = [];
    public List<(Movimentacao Movimentacao, LeituraDormente? Leitura)> Movimentacoes { get; } = [];
    public List<(int IdDormente, Action<Dormente> Mudar)> Pecas { get; } = [];
    public RecusaTroca? RecusaNova { get; init; }
    public RecusaTroca? RecusaAlterada { get; init; }
    public Inspecao? InspecaoAlterada { get; init; }
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