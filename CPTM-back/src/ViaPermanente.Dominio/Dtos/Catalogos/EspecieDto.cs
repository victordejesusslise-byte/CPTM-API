namespace ViaPermanente.Dominio.Dtos.Catalogos;

public record EspecieDto(int Id, string NomeCientifico, string Familia, string? NomePopular, byte Classe);