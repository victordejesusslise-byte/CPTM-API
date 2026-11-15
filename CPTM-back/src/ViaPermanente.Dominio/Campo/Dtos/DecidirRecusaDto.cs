using ViaPermanente.Dominio.Campo.Models;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O técnico responde à recusa: mantém (com resposta; a troca volta para aprovada) ou aceita (a troca é cancelada).</summary>
public class DecidirRecusaDto
{
    /// <summary>Manter (a troca ou o lote volta para a via) ou Aceitar (a troca ou o lote é cancelado).</summary>
    public DecisaoRecusa Decisao { get; init; }
    /// <summary>a resposta do técnico à via; obrigatória para manter.</summary>
    public string? Resposta { get; init; }
}