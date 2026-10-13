using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>Quanto o fornecedor tem disponível, em m³, de uma espécie e um tamanho.</summary>
public class NovaOfertaDto
{
    [Range(1, int.MaxValue)] public int IdEmpresa { get; init; }
    [Range(1, int.MaxValue)] public int IdEspecie { get; init; }
    [Range(1, int.MaxValue)] public int IdTamanho { get; init; }
    [Range(0, 100000)] public decimal VolumeM3Disponivel { get; init; }
    /// <summary>Sem data, vale a de hoje.</summary>
    public DateOnly? Data { get; init; }
}