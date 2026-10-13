namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Aprovar ou reprovar o pedido de troca. Reprovar exige motivo.</summary>
public class DecisaoTrocaDto
{
    public bool Aprovar { get; init; }
    public string? Motivo { get; init; }
}