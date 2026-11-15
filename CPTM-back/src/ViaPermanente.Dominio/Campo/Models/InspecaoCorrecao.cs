namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>INSPECAO_CORRECAO: correção não é retrabalho; os retratos são JSON.</summary>
public class InspecaoCorrecao
{
    /// <summary>id da correção (chave de INSPECAO_CORRECAO).</summary>
    public int IdCorrecao { get; set; }
    /// <summary>a inspeção corrigida (→ INSPECAO).</summary>
    public int IdInspecao { get; set; }
    /// <summary>quem corrigiu (→ USUARIO): o mesmo técnico que fez.</summary>
    public int IdUsuario { get; set; }
    /// <summary>quando foi corrigida.</summary>
    public DateTime Data { get; set; }
    /// <summary>as respostas antes da correção, em JSON.</summary>
    public string RetratoAntes { get; set; } = "";
    /// <summary>as respostas depois da correção, em JSON.</summary>
    public string RetratoDepois { get; set; } = "";
    /// <summary>por que corrigiu (obrigatório).</summary>
    public string Motivo { get; set; } = "";
    /// <summary>Nulo: a correção não mexeu na troca.</summary>
    public int? IdTrocaCancelada { get; set; }
}