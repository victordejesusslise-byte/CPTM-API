using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Notas;

/// <summary>Uma linha da nota, como o fornecedor declarou.</summary>
public class NovaNotaItemDto
{
    [Range(1, int.MaxValue)] public int IdEspecie { get; init; }
    [Range(1, int.MaxValue)] public int IdTamanho { get; init; }
    [Range(1, 2)] public byte Classe { get; init; }
    [Range(1, int.MaxValue)] public int Quantidade { get; init; }
    [Range(0, 100000)] public decimal VolumeM3 { get; init; }
}