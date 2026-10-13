using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pedidos.Dtos;

/// <summary>A nota fiscal digitada (origem manual). A importação da planilha vem na Tarefa 19.</summary>
public class NovaNotaDto
{
    [Required] public string Numero { get; init; } = "";
    /// <summary>Sem data, vale a de hoje.</summary>
    public DateOnly? DataEmissao { get; init; }
    public decimal? Valor { get; init; }
    public string? Arquivo { get; init; }
    [Required] public List<NovaNotaItemDto> Itens { get; init; } = [];
}