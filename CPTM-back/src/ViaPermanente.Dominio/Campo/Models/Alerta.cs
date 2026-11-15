namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>ALERTA: a máquina aponta, o técnico decide.</summary>
public class Alerta
{
    /// <summary>id do alerta (chave de ALERTA).</summary>
    public int IdAlerta { get; set; }
    /// <summary>Sensor (o carro mediu) ou Imagem (a câmera viu).</summary>
    public OrigemAlerta Origem { get; set; }
    /// <summary>Referência à LEITURA_VIA, sem chave estrangeira (a leitura é descartada após 90 dias). Nulo na origem imagem.</summary>
    public long? IdLeituraVia { get; set; }
    /// <summary>Nulo na origem sensor.</summary>
    public int? IdDeteccao { get; set; }
    /// <summary>Nulo enquanto a peça do alerta de imagem não for identificada.</summary>
    public int? IdDormente { get; set; }
    /// <summary>quem decidiu o alerta (→ USUARIO).</summary>
    public int? IdUsuarioDecisao { get; set; }
    /// <summary>o defeito: Rachadura, Posicao, TagMuda, Fenda ou Quebra.</summary>
    public TipoAlerta Tipo { get; set; }
    /// <summary>o desvio de posição medido pelo carro, em mm.</summary>
    public decimal? DesvioMm { get; set; }
    /// <summary>quando o carro viu o problema.</summary>
    public DateTime Data { get; set; }
    /// <summary>Aberto, Confirmado ou Descartado.</summary>
    public StatusAlerta Status { get; set; }
    /// <summary>quando o alerta foi decidido.</summary>
    public DateTime? DataDecisao { get; set; }
    /// <summary>por que o alerta foi descartado.</summary>
    public string? MotivoDescarte { get; set; }
}