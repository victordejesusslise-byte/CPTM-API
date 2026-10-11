namespace ViaPermanente.Dominio.Models.Campo;

/// <summary>TROCA: o técnico pede, o supervisor aprova, o pátio separa e a via troca. Uma troca aberta por peça.</summary>
public class Troca
{
    public int IdTroca { get; set; }
    public int IdDormenteSaiu { get; set; }
    /// <summary>Gravado na separação.</summary>
    public int? IdDormenteEntrou { get; set; }
    public int? IdInspecao { get; set; }
    public int? IdTrecho { get; set; }
    public int IdUsuarioPediu { get; set; }
    public int? IdUsuarioAprovou { get; set; }
    public DateTime? DataAprovacao { get; set; }
    public string? MotivoReprovacao { get; set; }
    public int? IdUsuarioSeparou { get; set; }
    public DateTime? DataSeparacao { get; set; }
    public int? IdUsuarioFez { get; set; }
    public StatusTroca Status { get; set; }
    public OrigemTroca Origem { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Obrigatória na execução.</summary>
    public string? FotoArquivo { get; set; }
    public string? Cancelamento { get; set; }
    public DateTime DataPedido { get; set; }
    /// <summary>Data da execução.</summary>
    public DateTime? Data { get; set; }
    public string? TagEntrou { get; set; }
    public decimal? Km { get; set; }
    public string? Equipe { get; set; }
    public string? Motivo { get; set; }
}