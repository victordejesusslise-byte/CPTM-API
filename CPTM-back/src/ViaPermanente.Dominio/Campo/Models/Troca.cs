namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>TROCA: o técnico pede, o supervisor aprova, o pátio separa e a via troca. Uma troca aberta por peça.</summary>
public class Troca
{
    /// <summary>id da troca (chave de TROCA).</summary>
    public int IdTroca { get; set; }
    /// <summary>a peça que sai da via (→ DORMENTE). Nula: um dormente antigo, sem cadastro, que saiu num lote pelo km (Victor, 2026-11-17).</summary>
    public int? IdDormenteSaiu { get; set; }
    /// <summary>a peça que sai nunca teve tag (Victor, 2026-11-17): a troca seguiu sem ler a peça e sem gravar falha de tag.</summary>
    public bool SaiuNuncaTeveTag { get; set; }
    /// <summary>Gravado na separação.</summary>
    public int? IdDormenteEntrou { get; set; }
    /// <summary>a inspeção reprovada que pediu a troca (→ INSPECAO).</summary>
    public int? IdInspecao { get; set; }
    /// <summary>o trecho da troca (→ TRECHO).</summary>
    public int? IdTrecho { get; set; }
    /// <summary>o técnico que pediu (→ USUARIO).</summary>
    public int IdUsuarioPediu { get; set; }
    /// <summary>quem aprovou ou reprovou (→ USUARIO).</summary>
    public int? IdUsuarioAprovou { get; set; }
    /// <summary>quando foi aprovada ou reprovada.</summary>
    public DateTime? DataAprovacao { get; set; }
    /// <summary>por que foi reprovada.</summary>
    public string? MotivoReprovacao { get; set; }
    /// <summary>o operador de pátio que separou a peça (→ USUARIO).</summary>
    public int? IdUsuarioSeparou { get; set; }
    /// <summary>quando a peça foi separada.</summary>
    public DateTime? DataSeparacao { get; set; }
    /// <summary>o operador da via que fez a troca (→ USUARIO).</summary>
    public int? IdUsuarioFez { get; set; }
    /// <summary>o passo em que a troca está.</summary>
    public StatusTroca Status { get; set; }
    /// <summary>Inspecao, Requisicao ou Lote.</summary>
    public OrigemTroca Origem { get; set; }
    /// <summary>a observação do pedido.</summary>
    public string? Observacao { get; set; }
    /// <summary>Obrigatória na execução.</summary>
    public string? FotoArquivo { get; set; }
    /// <summary>por que a troca foi cancelada.</summary>
    public string? Cancelamento { get; set; }
    /// <summary>quando a troca foi pedida.</summary>
    public DateTime DataPedido { get; set; }
    /// <summary>Data da execução.</summary>
    public DateTime? Data { get; set; }
    /// <summary>a tag da peça que entrou, gravada na troca feita.</summary>
    public string? TagEntrou { get; set; }
    /// <summary>o km da troca, em km com 3 casas.</summary>
    public decimal? Km { get; set; }
    /// <summary>a equipe de campo que fez a troca.</summary>
    public string? Equipe { get; set; }
    /// <summary>por que trocar.</summary>
    public string? Motivo { get; set; }
    /// <summary>A noite em que a troca entra no plano (gravada na aprovação; ver PlanoDaNoite).</summary>
    public DateOnly? NoiteDoPlano { get; set; }
    /// <summary>Aprovada depois do corte para entrar na mesma noite.</summary>
    public bool Urgente { get; set; }
    /// <summary>O ID que o celular gerou para a execução: a mesma troca enviada duas vezes (sem rede, depois com rede) entra uma vez.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>Quando a execução chegou ao servidor. A hora do campo fica em Data.</summary>
    public DateTime? RecebidoEm { get; set; }
    /// <summary>O lote de troca em que esta troca foi feita (origem Lote).</summary>
    public int? IdLoteTroca { get; set; }
}