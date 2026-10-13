using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Catalogos.Dtos;

/// <summary>Um leitor de tag: pistola, módulo do celular (reconhecido pelo número de série) ou carro.</summary>
public class NovoLeitorDto
{
    [Range(1, int.MaxValue)] public int IdTipoLeitor { get; init; }
    [Required] public string Codigo { get; init; } = "";
    public string? NumeroSerie { get; init; }
    public int? IdSetor { get; init; }
}