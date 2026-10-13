using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// A via confere as duas peças com o módulo: a que sai (a do pedido, não a vizinha; se não responde, marca)
/// e a que entra (a que o pátio separou). Equipe e foto da peça nova no lugar são obrigatórias.
/// </summary>
public class ExecutarTrocaDto
{
    public LeituraDto? LeituraSaiu { get; init; }
    public bool TagSaiuNaoResponde { get; init; }
    [Required] public LeituraDto LeituraEntrou { get; init; } = new();
    public string Equipe { get; init; } = "";
    public string FotoArquivo { get; init; } = "";
    public decimal? Km { get; init; }
}