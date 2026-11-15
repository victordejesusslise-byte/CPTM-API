namespace ViaPermanente.Dominio.Campo.Models;

/// <summary>O técnico mantém a troca ou aceita a recusa.</summary>
public enum DecisaoRecusa
{
    /// <summary>a troca continua: volta para a via.</summary>
    Manter,
    /// <summary>a recusa vale: a troca é cancelada.</summary>
    Aceitar
}
