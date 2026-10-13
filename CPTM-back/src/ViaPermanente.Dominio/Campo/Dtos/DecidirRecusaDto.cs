using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O técnico responde à recusa: mantém (com resposta; a troca volta para aprovada) ou aceita (a troca é cancelada).</summary>
public class DecidirRecusaDto
{
    public DecisaoRecusa Decisao { get; init; }
    public string? Resposta { get; init; }
}