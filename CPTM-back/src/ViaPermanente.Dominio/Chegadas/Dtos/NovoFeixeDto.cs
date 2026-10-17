using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Chegadas.Dtos;

/// <summary>Um feixe da carga, com os dados da placa (que ficam no feixe, não em cada peça).</summary>
public class NovoFeixeDto
{
    [Required] public string NumeroFeixe { get; init; } = "";
    [Range(1, 1000)] public int Quantidade { get; init; }
    [Range(1, int.MaxValue)] public int IdEspecie { get; init; }
    [Range(1, int.MaxValue)] public int IdTamanho { get; init; }
    /// <summary>Opcional: sem ela, vale a classe do item do pedido; obrigatória quando o pedido tem 1ª e 2ª para o mesmo tamanho.</summary>
    [Range(1, 2)] public byte? Classe { get; init; }
    public string? Tratamento { get; init; }
    public int? IdEmpresaTratamento { get; init; }
    public int? IdEmpresaInspecao { get; init; }
    public short? AnoInspecao { get; init; }
}