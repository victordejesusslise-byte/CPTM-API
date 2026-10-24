using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Catalogos.Dtos;

/// <summary>Um padrão de medida do fornecedor; sem espécie, vale para todas.</summary>
public record MedidaDto(int Id, int IdEmpresa, int? IdEspecie, string? Especie, decimal LarguraM, decimal AlturaM, bool Ativo, string Rotulo);

/// <summary>O padrão que o supervisor cadastra para o fornecedor (largura e altura em metros, de 5 cm a 1 m).</summary>
public class NovaMedidaDto
{
    public int? IdEspecie { get; init; }
    [Range(typeof(decimal), "0.05", "1")] public decimal LarguraM { get; init; }
    [Range(typeof(decimal), "0.05", "1")] public decimal AlturaM { get; init; }
}
