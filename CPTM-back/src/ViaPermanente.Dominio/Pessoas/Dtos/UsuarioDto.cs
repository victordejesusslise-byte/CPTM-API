namespace ViaPermanente.Dominio.Pessoas.Dtos;

/// <summary>O usuário como sai da API. Nunca leva a senha nem o hash. EsperarAte: a conta está esperando por senhas erradas seguidas.</summary>
public record UsuarioDto(int Id, string Nome, string Email, string Cargo, bool Ativo, DateTime? EsperarAte = null);
