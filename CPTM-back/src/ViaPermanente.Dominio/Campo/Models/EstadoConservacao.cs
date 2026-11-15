namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Checklist da inspeção.</summary>
public enum EstadoConservacao
{
    /// <summary>peça em bom estado.</summary>
    Bom,
    /// <summary>peça com desgaste, mas ainda serve.</summary>
    Regular,
    /// <summary>peça em mau estado: reprova.</summary>
    Ruim
}
