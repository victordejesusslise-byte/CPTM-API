using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Chegadas;

/// <summary>Um feixe da carga, com os dados da placa (que ficam no feixe, não em cada peça).</summary>
public class NovoFeixeDto
{
    [Required] public string NumeroFeixe { get; init; } = "";
    [Range(1, 1000)] public int Quantidade { get; init; }
    [Range(1, int.MaxValue)] public int IdEspecie { get; init; }
    [Range(1, int.MaxValue)] public int IdTamanho { get; init; }
    public string? Tratamento { get; init; }
    public int? IdEmpresaTratamento { get; init; }
    public int? IdEmpresaInspecao { get; init; }
    public short? AnoInspecao { get; init; }
}