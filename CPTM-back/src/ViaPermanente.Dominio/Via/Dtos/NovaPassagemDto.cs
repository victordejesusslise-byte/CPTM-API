using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Via.Dtos;

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
    /// <summary>Id da estação onde a passagem começa, o bip 0 (→ ESTACAO). Vem junto com a de fim; o km delas vem do cadastro.</summary>
    public int? IdEstacaoInicio { get; init; }
    /// <summary>Id da estação onde a passagem termina (→ ESTACAO).</summary>
    public int? IdEstacaoFim { get; init; }
    /// <summary>O caminho do veículo: um ponto do GPS para cada foto (o bip), com a hora do GPS. O carro guarda tudo e manda depois.</summary>
    public List<NovoPontoGpsDto> Pontos { get; init; } = [];
}