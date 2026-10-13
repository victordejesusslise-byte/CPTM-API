namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Um alerta da fila (aba Imagem ou Sensores), com a peça, se já se sabe qual é.</summary>
public record AlertaDto(int Id, string Origem, string Tipo, string Status, DateTime Data, decimal? DesvioMm, decimal? Km,
    int? IdDormente, string? CodigoPeca, string? Tag, DeteccaoDto? Deteccao, int Vistorias,
    int? IdUsuarioDecisao, DateTime? DataDecisao, string? MotivoDescarte, string? NotaPeca = null);