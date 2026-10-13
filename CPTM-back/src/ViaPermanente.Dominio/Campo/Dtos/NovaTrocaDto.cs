namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// O técnico pede a troca de uma peça da via: a partir da inspeção reprovada (origem inspeção)
/// ou por requisição, com motivo. Uma troca aberta por peça.
/// </summary>
public class NovaTrocaDto
{
    public int IdDormente { get; init; }
    public int? IdInspecao { get; init; }
    public string? Motivo { get; init; }
    public string? Observacao { get; init; }
    /// <summary>Sem trecho na peça, informe o trecho da troca (dá a classe exigida).</summary>
    public int? IdTrecho { get; init; }
    public decimal? Km { get; init; }
}