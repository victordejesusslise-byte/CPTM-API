using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>"Não confere": o técnico descarta na hora, com motivo.</summary>
public class DescartarAlertaDto
{
    /// <summary>por que o alerta não confere; obrigatório (vai para ALERTA.motivo_descarte).</summary>
    [Required] public string Motivo { get; init; } = "";
}