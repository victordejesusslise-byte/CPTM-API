namespace ViaPermanente.Dominio.Ambiental.Models;

/// <summary>INTEGRACAO_LOG: cada planilha importada ou exportada; só insere.</summary>
public class IntegracaoLog
{
    public int IdLog { get; set; }
    public SistemaIntegracao Sistema { get; set; }
    public DirecaoIntegracao Direcao { get; set; }
    public TipoIntegracao Tipo { get; set; }
    public DateTime Data { get; set; }
    public StatusIntegracao Status { get; set; }
    public string? Mensagem { get; set; }
    public string? Arquivo { get; set; }
}