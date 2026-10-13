namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>INSPECAO_CORRECAO: correção não é retrabalho; os retratos são JSON.</summary>
public class InspecaoCorrecao
{
    public int IdCorrecao { get; set; }
    public int IdInspecao { get; set; }
    public int IdUsuario { get; set; }
    public DateTime Data { get; set; }
    public string RetratoAntes { get; set; } = "";
    public string RetratoDepois { get; set; } = "";
    public string Motivo { get; set; } = "";
    /// <summary>Nulo: a correção não mexeu na troca.</summary>
    public int? IdTrocaCancelada { get; set; }
}