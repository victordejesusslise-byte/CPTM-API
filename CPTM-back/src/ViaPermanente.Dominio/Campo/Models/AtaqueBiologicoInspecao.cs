namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>Checklist da inspeção.</summary>
public enum AtaqueBiologicoInspecao
{
    /// <summary>sem sinal de fungo ou inseto.</summary>
    Nenhum,
    /// <summary>ataque começando.</summary>
    Inicio,
    /// <summary>ataque avançado: reprova a peça.</summary>
    Avancado
}
