using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Catalogos.Dtos;

public class AlterarParametroDto
{
    [Required] public string Valor { get; init; } = "";
}