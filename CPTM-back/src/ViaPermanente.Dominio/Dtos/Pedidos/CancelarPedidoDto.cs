using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Pedidos;

public class CancelarPedidoDto
{
    [Required] public string Motivo { get; init; } = "";
}