namespace ViaPermanente.Dominio.Pessoas.Models;

/// <summary>PESSOA: o e-mail é o login do usuário (único).</summary>
public class Pessoa
{
    public int IdPessoa { get; set; }
    public string Nome { get; set; } = "";
    public string? Email { get; set; }
    public DateOnly? DataNascimento { get; set; }
}