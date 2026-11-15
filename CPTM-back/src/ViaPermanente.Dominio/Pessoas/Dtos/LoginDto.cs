using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Pessoas.Dtos;

/// <summary>O que o Angular manda para entrar.</summary>
public class LoginDto
{
    /// <summary>O e-mail da pessoa (o login); maiúsculas e espaços não importam.</summary>
    [Required] public string Email { get; init; } = "";
    /// <summary>A senha digitada, conferida com o hash guardado no banco.</summary>
    [Required] public string Senha { get; init; } = "";
}