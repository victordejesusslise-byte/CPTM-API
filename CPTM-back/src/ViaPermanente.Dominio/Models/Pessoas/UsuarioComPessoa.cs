namespace ViaPermanente.Dominio.Models.Pessoas;

/// <summary>O usuário junto com a pessoa dele (nome e e-mail).</summary>
public record UsuarioComPessoa(Usuario Usuario, Pessoa Pessoa);