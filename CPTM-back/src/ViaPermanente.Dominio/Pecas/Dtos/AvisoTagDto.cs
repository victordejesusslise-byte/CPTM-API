namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>O operador da via avisa uma tag sem cadastro achada em campo, para o técnico resolver.</summary>
public class NovoAvisoTagDto
{
    /// <summary>O número lido da tag sem cadastro (BR e 10 dígitos).</summary>
    public string Tag { get; init; } = "";
    /// <summary>Onde a tag foi achada, em km com 3 casas, se o operador souber.</summary>
    public decimal? Km { get; init; }
    /// <summary>Texto livre do operador.</summary>
    public string? Observacao { get; init; }
    /// <summary>Id do leitor usado (→ LEITOR).</summary>
    public int? IdLeitor { get; init; }
    /// <summary>Id do modo de leitura (→ MODO_LEITURA).</summary>
    public int? IdModo { get; init; }
}

/// <summary>Um aviso de tag sem cadastro, como a lista de pendentes mostra.</summary>
/// <param name="IdLeitura">Id da leitura que guardou o aviso (→ LEITURA_DORMENTE).</param>
/// <param name="Tag">O número da tag avisada.</param>
/// <param name="Km">Onde foi achada, em km com 3 casas.</param>
/// <param name="Observacao">Texto livre do operador.</param>
/// <param name="Data">Quando foi avisada.</param>
/// <param name="IdUsuario">Id de quem avisou (→ USUARIO).</param>
public record AvisoTagDto(int IdLeitura, string Tag, decimal? Km, string? Observacao, DateTime Data, int IdUsuario);

/// <summary>O técnico ou o supervisor liga a tag achada em campo a uma peça, lendo a tag no local.
/// A tag antiga da peça é desligada; a nova passa a valer.</summary>
public class VincularTagDto
{
    /// <summary>Id da peça que recebe a tag (→ DORMENTE).</summary>
    public int IdDormente { get; init; }
    /// <summary>A leitura da tag no local, com o módulo e uma tag só no alcance.</summary>
    public LeituraDto Leitura { get; init; } = new();
}