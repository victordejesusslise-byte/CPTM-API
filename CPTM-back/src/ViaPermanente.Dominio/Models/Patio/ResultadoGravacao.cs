namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>Ok só quando a releitura bate e havia uma tag só.</summary>
public enum ResultadoGravacao
{
    Ok,
    Divergente,
    Bloqueada,
    Falhou
}
