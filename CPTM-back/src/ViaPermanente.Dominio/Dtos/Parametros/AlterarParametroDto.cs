using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Parametros;

public class AlterarParametroDto
{
    [Required] public string Valor { get; init; } = "";
}