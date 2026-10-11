namespace ViaPermanente.Dominio.Dtos.Pecas;

/// <summary>O operador da via avisa uma tag sem cadastro achada em campo, para o técnico resolver.</summary>
public class NovoAvisoTagDto
{
    public string Tag { get; init; } = "";
    public decimal? Km { get; init; }
    public string? Observacao { get; init; }
    public int? IdLeitor { get; init; }
    public int? IdModo { get; init; }
}

public record AvisoTagDto(int IdLeitura, string Tag, decimal? Km, string? Observacao, DateTime Data, int IdUsuario);

/// <summary>
/// O técnico ou o supervisor vincula a tag achada em campo à peça, lendo a tag no local com alvo único.
/// A tag velha da peça fica inativa; a nova entra ativa, com quem e quando vinculou.
/// </summary>
public class VincularTagDto
{
    public int IdDormente { get; init; }
    public LeituraDto Leitura { get; init; } = new();
}