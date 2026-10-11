using System.ComponentModel.DataAnnotations;

namespace ViaPermanente.Dominio.Dtos.Auth;

/// <summary>O que o Angular manda para entrar.</summary>
public class LoginDto
{
    [Required] public string Email { get; init; } = "";
    [Required] public string Senha { get; init; } = "";
}