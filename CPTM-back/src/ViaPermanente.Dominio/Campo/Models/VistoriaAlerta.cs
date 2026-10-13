using ViaPermanente.Dominio.Comum;
namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>VISTORIA_ALERTA: uma linha por ida ao local; não fecha o alerta.</summary>
public class VistoriaAlerta : IRegistroDeCampo
{
    public int IdVistoria { get; set; }
    public int IdAlerta { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Data { get; set; }
    /// <summary>Sim: confirmo o defeito. Não: não vi defeito.</summary>
    public bool Viu { get; set; }
    /// <summary>Quando o alerta não tinha peça.</summary>
    public string? TagLida { get; set; }
    public string FotoArquivo { get; set; } = "";
    public string? Observacao { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}