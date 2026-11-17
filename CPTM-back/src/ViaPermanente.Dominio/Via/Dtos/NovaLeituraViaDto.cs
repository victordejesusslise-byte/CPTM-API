namespace ViaPermanente.Dominio.Via.Dtos;

/// <summary>Uma leitura do carro: a tag que respondeu (ou a peça esperada no ponto, se não respondeu) e o que ele mediu.</summary>
public class NovaLeituraViaDto
{
    public string? Tag { get; init; }
    public int? IdDormente { get; init; }
    public bool Leitura { get; init; } = true;
    public decimal? KmMedido { get; init; }
    public decimal? DesvioMm { get; init; }
    public DateTime Data { get; init; }
    public decimal? Umidade { get; init; }
    public decimal? Temperatura { get; init; }
    public bool RachaduraSuspeita { get; init; }
    /// <summary>Latitude do GPS do veículo na hora da leitura. Com as duas estações da passagem, é por ela que o sistema calcula o km.</summary>
    public decimal? Latitude { get; init; }
    /// <summary>Longitude do GPS do veículo na hora da leitura.</summary>
    public decimal? Longitude { get; init; }
}