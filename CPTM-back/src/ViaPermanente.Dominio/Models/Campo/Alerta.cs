namespace ViaPermanente.Dominio.Models.Campo;

/// <summary>ALERTA: a máquina aponta, o técnico decide.</summary>
public class Alerta
{
    public int IdAlerta { get; set; }
    public OrigemAlerta Origem { get; set; }
    /// <summary>Referência à LEITURA_VIA, sem chave estrangeira (a leitura é descartada após 90 dias). Nulo na origem imagem.</summary>
    public long? IdLeituraVia { get; set; }
    /// <summary>Nulo na origem sensor.</summary>
    public int? IdDeteccao { get; set; }
    /// <summary>Nulo enquanto a peça do alerta de imagem não for identificada.</summary>
    public int? IdDormente { get; set; }
    public int? IdUsuarioDecisao { get; set; }
    public TipoAlerta Tipo { get; set; }
    public decimal? DesvioMm { get; set; }
    public DateTime Data { get; set; }
    public StatusAlerta Status { get; set; }
    public DateTime? DataDecisao { get; set; }
    public string? MotivoDescarte { get; set; }
}