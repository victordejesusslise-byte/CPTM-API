namespace ViaPermanente.Dominio.Models.Chegada;

/// <summary>LEITURA_OCR: uma linha por foto da placa; só insere.</summary>
public class LeituraOcr : IRegistroDeCampo
{
    public int IdLeituraOcr { get; set; }
    public OrigemLeituraOcr Origem { get; set; }
    public string FotoArquivo { get; set; } = "";
    public string? TextoLido { get; set; }
    public decimal? ConfiancaMedia { get; set; }
    public int IdModelo { get; set; }
    public int? IdRecebimento { get; set; }
    public int? IdFeixe { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Data { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}