using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O pátio lê a peça compatível na pilha (alvo único) e a separa para a troca.</summary>
public class SepararTrocaDto
{
    public int IdDormente { get; init; }
    [Required] public LeituraDto Leitura { get; init; } = new();
}