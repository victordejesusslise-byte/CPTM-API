using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Catalogos.Dtos;

/// <summary>Um leitor de tag: pistola, módulo do celular (reconhecido pelo número de série) ou carro.</summary>
public class NovoLeitorDto
{
    /// <summary>1 Módulo RFID, 2 Pistola NIR, 3 Carro (lista fixa no código, TipoLeitor).</summary>
    [Range(1, 3)] public int IdTipoLeitor { get; init; }
    [Required] public string Codigo { get; init; } = "";
    public string? NumeroSerie { get; init; }
    public int? IdSetor { get; init; }
}