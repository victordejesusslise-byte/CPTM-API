namespace ViaPermanente.Dominio.Models.Chegada;

/// <summary>OCR_CAMPO: cada campo lido, a confiança e o que o operador confirmou.</summary>
public class OcrCampo
{
    public int IdCampo { get; set; }
    public int IdLeituraOcr { get; set; }
    public CampoOcr Campo { get; set; }
    public string? ValorLido { get; set; }
    public decimal? Confianca { get; set; }
    /// <summary>Nulo: não se aplica.</summary>
    public bool? ConferePedido { get; set; }
    public string? ValorConfirmado { get; set; }
    public bool? Corrigido { get; set; }
    public int? CaixaX { get; set; }
    public int? CaixaY { get; set; }
    public int? CaixaLargura { get; set; }
    public int? CaixaAltura { get; set; }
    public int? IdModeloTreino { get; set; }
}