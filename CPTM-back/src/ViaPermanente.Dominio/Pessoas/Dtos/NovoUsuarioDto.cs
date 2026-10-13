using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Pessoas.Dtos;

/// <summary>O que o supervisor manda para cadastrar alguém.</summary>
public class NovoUsuarioDto
{
    [Required] public string Nome { get; init; } = "";
    [Required] public string Email { get; init; } = "";
    [Required] public string Senha { get; init; } = "";
    [Required] public Cargo? Cargo { get; init; }
}