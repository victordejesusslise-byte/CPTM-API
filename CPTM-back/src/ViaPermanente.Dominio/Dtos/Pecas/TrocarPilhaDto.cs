using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Pecas;

/// <summary>Mudar a peça de pilha, lendo a peça (alvo único). A pilha nova tem de ser do tamanho e da classe dela.</summary>
public class TrocarPilhaDto
{
    public int IdPilha { get; init; }
    [Required] public LeituraDto Leitura { get; init; } = new();
}