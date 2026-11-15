using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Pessoas.Dtos;

/// <summary>O que o supervisor manda para cadastrar alguém.</summary>
public class NovoUsuarioDto
{
    /// <summary>O nome da pessoa.</summary>
    [Required] public string Nome { get; init; } = "";
    /// <summary>O e-mail da pessoa, que vira o login (único).</summary>
    [Required] public string Email { get; init; } = "";
    /// <summary>A senha inicial, com pelo menos 8 caracteres; o banco guarda só o hash.</summary>
    [Required] public string Senha { get; init; } = "";
    /// <summary>O cargo, que vira a permissão do token.</summary>
    [Required] public Cargo? Cargo { get; init; }
}