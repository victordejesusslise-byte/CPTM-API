using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>"Não confere": o técnico descarta na hora, com motivo.</summary>
public class DescartarAlertaDto
{
    [Required] public string Motivo { get; init; } = "";
}