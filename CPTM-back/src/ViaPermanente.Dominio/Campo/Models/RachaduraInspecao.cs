namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Checklist da inspeção.</summary>
public enum RachaduraInspecao
{
    /// <summary>sem rachadura.</summary>
    Nenhuma,
    /// <summary>rachadura pequena.</summary>
    Leve,
    /// <summary>rachadura média.</summary>
    Media,
    /// <summary>rachadura grave: reprova a peça.</summary>
    Grave
}
