namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Aprovar ou reprovar o pedido de troca. Reprovar exige motivo.</summary>
public class DecisaoTrocaDto
{
    public bool Aprovar { get; init; }
    public string? Motivo { get; init; }
    /// <summary>Depois do corte (20:00), marcar urgente faz a troca entrar no plano desta noite; sem isso, vai para a noite seguinte.</summary>
    public bool Urgente { get; init; }
}