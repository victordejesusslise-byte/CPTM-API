using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O pátio lê a peça compatível na pilha (alvo único) e a separa para a troca.</summary>
public class SepararTrocaDto
{
    /// <summary>id da peça do estoque que vai entrar na via (→ DORMENTE); tem de estar na lista das compatíveis.</summary>
    public int IdDormente { get; init; }
    /// <summary>a leitura da tag dessa peça na pilha, com alvo único; obrigatória.</summary>
    [Required] public LeituraDto Leitura { get; init; } = new();
}