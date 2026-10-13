using ViaPermanente.Dominio.Comum;
namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>RECUSA_TROCA: uma linha por volta; o status da troca acompanha a última recusa.</summary>
public class RecusaTroca : IRegistroDeCampo
{
    public int IdRecusa { get; set; }
    public int IdTroca { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Data { get; set; }
    public string Motivo { get; set; } = "";
    public string? Descricao { get; set; }
    public string FotoArquivo { get; set; } = "";
    public DecisaoRecusa? Decisao { get; set; }
    public int? IdUsuarioDecisao { get; set; }
    public DateTime? DataDecisao { get; set; }
    /// <summary>Obrigatória para manter.</summary>
    public string? Resposta { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}