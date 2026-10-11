using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Varredura;

/// <summary>A passagem noturna do carro: as leituras das tags e as detecções da câmera.</summary>
public class NovaPassagemDto
{
    [Required] public string Codigo { get; init; } = "";
    public string? Veiculo { get; init; }
    public DateTime DataInicio { get; init; }
    public DateTime? DataFim { get; init; }
    public decimal? KmInicio { get; init; }
    public decimal? KmFim { get; init; }
    public List<NovaLeituraViaDto> Leituras { get; init; } = [];
    public List<NovaDeteccaoDto> Deteccoes { get; init; } = [];
}