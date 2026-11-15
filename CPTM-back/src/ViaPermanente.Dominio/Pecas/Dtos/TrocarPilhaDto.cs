using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>Mudar a peça de pilha, lendo a peça (alvo único). A pilha nova tem de ser do tamanho e da classe dela.</summary>
public class TrocarPilhaDto
{
    /// <summary>Id da pilha nova (→ PILHA).</summary>
    public int IdPilha { get; init; }
    /// <summary>A leitura da tag da peça, com uma tag só no alcance.</summary>
    [Required] public LeituraDto Leitura { get; init; } = new();
}