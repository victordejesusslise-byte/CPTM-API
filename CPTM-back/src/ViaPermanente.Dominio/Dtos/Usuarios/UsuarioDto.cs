namespace ViaPermanente.Dominio.Dtos.Usuarios;

/// <summary>O usuário como sai da API. Nunca leva a senha nem o hash.</summary>
public record UsuarioDto(int Id, string Nome, string Email, string Cargo, bool Ativo);