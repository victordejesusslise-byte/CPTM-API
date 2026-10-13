namespace ViaPermanente.Dominio.Catalogos.Dtos;

/// <summary>O armazém com as pilhas dele.</summary>
public record ArmazemDto(int Id, string Nome, string Linha, decimal? Km, int Pecas, IReadOnlyList<PilhaDto> Pilhas);