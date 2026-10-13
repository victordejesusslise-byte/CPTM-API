using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Catalogos.Dtos;

/// <summary>O código de material do ERP, por tamanho e classe.</summary>
public record CodigoMaterialDto(int Id, int IdTamanho, decimal ComprimentoM, byte Classe, string CodigoErp);

public class NovoCodigoMaterialDto
{
    public int IdTamanho { get; init; }
    [Range(1, 2)] public byte Classe { get; init; }
    [Required] public string CodigoErp { get; init; } = "";
}