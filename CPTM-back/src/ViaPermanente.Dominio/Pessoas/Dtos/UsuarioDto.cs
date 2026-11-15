namespace ViaPermanente.Dominio.Pessoas.Dtos;

/// <summary>O usuário como sai da API. Nunca leva a senha nem o hash. EsperarAte: a conta está esperando por senhas erradas seguidas.</summary>
/// <param name="Id">id do usuário (→ USUARIO).</param>
/// <param name="Nome">o nome da pessoa.</param>
/// <param name="Email">o e-mail, que é o login.</param>
/// <param name="Cargo">o cargo em texto (Supervisor, Tecnico, Especialista, OperadorPatio, OperadorVia).</param>
/// <param name="Ativo">falso: a conta foi desativada e não entra.</param>
/// <param name="EsperarAte">até quando a conta espera depois de senhas erradas seguidas; nulo se não está esperando.</param>
public record UsuarioDto(int Id, string Nome, string Email, string Cargo, bool Ativo, DateTime? EsperarAte = null);
