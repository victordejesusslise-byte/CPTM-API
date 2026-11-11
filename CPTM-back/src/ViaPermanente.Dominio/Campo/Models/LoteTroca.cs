namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>
/// LOTE_TROCA (Victor, 2026-11-11): várias trocas de uma vez. O técnico pede o trecho, a faixa de km, a quantidade e o comprimento, sem
/// escolher as peças; quem aprova aprova o lote inteiro (pode baixar a quantidade, com o porquê); o pátio separa; na noite, a via lê a tag
/// de cada dormente que sai e de cada um que entra, e cada par vira uma TROCA normal ligada ao lote.
/// </summary>
public class LoteTroca
{
    public int IdLoteTroca { get; set; }
    public int IdTrecho { get; set; }
    public decimal KmInicio { get; set; }
    public decimal KmFim { get; set; }
    /// <summary>O comprimento das peças do lote (TAMANHO_DORMENTE).</summary>
    public int IdTamanho { get; set; }
    public int QuantidadePedida { get; set; }
    /// <summary>Gravada na aprovação; pode ser menor que a pedida (com o porquê em MotivoMenos).</summary>
    public int? QuantidadeAprovada { get; set; }
    public string Motivo { get; set; } = "";
    public string? Observacao { get; set; }
    public StatusLoteTroca Status { get; set; }
    public int IdUsuarioPediu { get; set; }
    public DateTime DataPedido { get; set; }
    public int? IdUsuarioAprovou { get; set; }
    public DateTime? DataAprovacao { get; set; }
    public string? MotivoReprovacao { get; set; }
    /// <summary>Por que aprovou menos peças que as pedidas.</summary>
    public string? MotivoMenos { get; set; }
    /// <summary>A noite em que o lote entra no plano (gravada na aprovação; ver PlanoDaNoite).</summary>
    public DateOnly? NoiteDoPlano { get; set; }
    public bool Urgente { get; set; }
    public int? IdUsuarioFez { get; set; }
    /// <summary>Quando a via fechou o lote (a hora do campo).</summary>
    public DateTime? Data { get; set; }
    public string? Equipe { get; set; }
    /// <summary>O ID que o celular gerou para o fechamento: o lote mandado duas vezes entra uma vez.</summary>
    public Guid? IdCliente { get; set; }
    public DateTime? RecebidoEm { get; set; }
    public string? Cancelamento { get; set; }
}
