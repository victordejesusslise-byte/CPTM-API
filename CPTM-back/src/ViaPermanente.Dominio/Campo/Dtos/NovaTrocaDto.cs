namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// O técnico pede a troca de uma peça da via: a partir da inspeção reprovada (origem inspeção)
/// ou por requisição, com motivo. Uma troca aberta por peça.
/// </summary>
public class NovaTrocaDto
{
    /// <summary>id da peça da via que vai sair (→ DORMENTE).</summary>
    public int IdDormente { get; init; }
    /// <summary>a inspeção reprovada desta peça que pede a troca (→ INSPECAO); nula na troca por requisição.</summary>
    public int? IdInspecao { get; init; }
    /// <summary>por que trocar; obrigatório na troca por requisição.</summary>
    public string? Motivo { get; init; }
    /// <summary>a observação do pedido.</summary>
    public string? Observacao { get; init; }
    /// <summary>Sem trecho na peça, informe o trecho da troca (dá a classe exigida).</summary>
    public int? IdTrecho { get; init; }
    /// <summary>o km da troca, em km com 3 casas; nulo: o km da peça na via.</summary>
    public decimal? Km { get; init; }
}