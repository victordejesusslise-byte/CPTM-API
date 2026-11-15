namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>
/// LOTE_TROCA (Victor, 2026-11-11): várias trocas de uma vez. O técnico pede o trecho, a faixa de km e a quantidade, sem escolher as peças.
/// Quem aprova aprova o lote inteiro, o pátio separa e, na noite, cada dormente trocado vira uma TROCA normal ligada ao lote.
/// </summary>
public class LoteTroca
{
    /// <summary>id do lote (chave de LOTE_TROCA); na tela, LT-0003.</summary>
    public int IdLoteTroca { get; set; }
    /// <summary>o trecho do lote (→ TRECHO).</summary>
    public int IdTrecho { get; set; }
    /// <summary>o começo da faixa, em km com 3 casas.</summary>
    public decimal KmInicio { get; set; }
    /// <summary>o fim da faixa, em km com 3 casas.</summary>
    public decimal KmFim { get; set; }
    /// <summary>O comprimento das peças do lote (TAMANHO_DORMENTE).</summary>
    public int IdTamanho { get; set; }
    /// <summary>quantas peças o técnico pediu.</summary>
    public int QuantidadePedida { get; set; }
    /// <summary>Gravada na aprovação; pode ser menor que a pedida (com o porquê em MotivoMenos).</summary>
    public int? QuantidadeAprovada { get; set; }
    /// <summary>por que o técnico pediu o lote.</summary>
    public string Motivo { get; set; } = "";
    /// <summary>a observação do pedido.</summary>
    public string? Observacao { get; set; }
    /// <summary>o passo em que o lote está.</summary>
    public StatusLoteTroca Status { get; set; }
    /// <summary>o técnico que pediu (→ USUARIO).</summary>
    public int IdUsuarioPediu { get; set; }
    /// <summary>quando o lote foi pedido.</summary>
    public DateTime DataPedido { get; set; }
    /// <summary>quem aprovou ou reprovou (→ USUARIO).</summary>
    public int? IdUsuarioAprovou { get; set; }
    /// <summary>quando foi aprovado ou reprovado.</summary>
    public DateTime? DataAprovacao { get; set; }
    /// <summary>por que foi reprovado.</summary>
    public string? MotivoReprovacao { get; set; }
    /// <summary>Por que aprovou menos peças que as pedidas.</summary>
    public string? MotivoMenos { get; set; }
    /// <summary>A noite em que o lote entra no plano (gravada na aprovação; ver PlanoDaNoite).</summary>
    public DateOnly? NoiteDoPlano { get; set; }
    /// <summary>aprovado depois do corte para entrar na mesma noite.</summary>
    public bool Urgente { get; set; }
    /// <summary>o operador da via que fechou o lote (→ USUARIO).</summary>
    public int? IdUsuarioFez { get; set; }
    /// <summary>Quando a via fechou o lote (a hora do campo).</summary>
    public DateTime? Data { get; set; }
    /// <summary>a equipe de campo que trocou.</summary>
    public string? Equipe { get; set; }
    /// <summary>O ID que o celular gerou para o fechamento: o lote mandado duas vezes entra uma vez.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>quando o fechamento chegou ao servidor.</summary>
    public DateTime? RecebidoEm { get; set; }
    /// <summary>por que o lote foi cancelado.</summary>
    public string? Cancelamento { get; set; }
}
