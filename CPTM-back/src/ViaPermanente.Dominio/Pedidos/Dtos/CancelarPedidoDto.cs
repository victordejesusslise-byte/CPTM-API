using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pedidos.Dtos;

public class CancelarPedidoDto
{
    [Required] public string Motivo { get; init; } = "";
}