namespace ViaPermanente.Dominio.Dtos.Catalogos;

public record LeitorDto(int Id, string Codigo, int IdTipoLeitor, string Tipo, string? NumeroSerie, bool Ativo);