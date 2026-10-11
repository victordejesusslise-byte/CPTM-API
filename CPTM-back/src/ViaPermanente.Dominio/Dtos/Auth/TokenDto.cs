using ViaPermanente.Dominio.Dtos.Usuarios;

namespace ViaPermanente.Dominio.Dtos.Auth;

/// <summary>O que o login devolve: o token JWT, quando vence e quem entrou.</summary>
public record TokenDto(string Token, DateTime ExpiraEm, UsuarioDto Usuario);