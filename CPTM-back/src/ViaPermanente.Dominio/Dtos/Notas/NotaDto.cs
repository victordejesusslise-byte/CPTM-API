namespace ViaPermanente.Dominio.Dtos.Notas;

/// <summary>A nota com a conferência: o status gravado e, recalculados, as divergências e os avisos.</summary>
public record NotaDto(int Id, int IdPedido, string Numero, DateOnly DataEmissao, decimal? Valor, string? Arquivo,
    string Origem, string StatusConferencia, int? IdUsuarioConferiu, DateTime? DataConferencia,
    IReadOnlyList<NotaItemDto> Itens, IReadOnlyList<string> Divergencias, IReadOnlyList<string> Avisos);