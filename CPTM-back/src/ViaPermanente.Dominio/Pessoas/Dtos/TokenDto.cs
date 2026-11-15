
namespace ViaPermanente.Dominio.Pessoas.Dtos;

/// <summary>O que o login devolve: o token JWT, quando vence e quem entrou.</summary>
/// <param name="Token">o token JWT assinado, que o front manda no cabeçalho Authorization.</param>
/// <param name="ExpiraEm">quando o token vence, em UTC (8 horas depois do login).</param>
/// <param name="Usuario">quem entrou, com o cargo.</param>
public record TokenDto(string Token, DateTime ExpiraEm, UsuarioDto Usuario);