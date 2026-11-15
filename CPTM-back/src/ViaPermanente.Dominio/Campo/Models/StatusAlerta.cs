namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Quem fecha o alerta é o checklist do técnico.</summary>
public enum StatusAlerta
{
    /// <summary>esperando a decisão do técnico.</summary>
    Aberto,
    /// <summary>o problema existe: a inspeção reprovou ou a peça foi trocada.</summary>
    Confirmado,
    /// <summary>o problema não existe: "Não confere" ou a inspeção aprovou.</summary>
    Descartado
}
