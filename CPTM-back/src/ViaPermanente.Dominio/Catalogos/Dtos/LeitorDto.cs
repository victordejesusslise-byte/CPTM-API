namespace ViaPermanente.Dominio.Catalogos.Dtos;

public record LeitorDto(int Id, string Codigo, int IdTipoLeitor, string Tipo, string? NumeroSerie, bool Ativo);