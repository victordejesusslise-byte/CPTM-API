using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Pessoas;

/// <summary>
/// As contas de acesso, em /api/usuarios; só o Supervisor chama. Lista, busca, cria e desativa ou reativa.
/// As regras (e-mail único, senha de 8 caracteres ou mais, ninguém se desativa) ficam no UsuarioService.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = nameof(Cargo.Supervisor))]
public class UsuarioHandler(UsuarioService usuarios) : ControllerBase
{
    /// <summary>GET /api/usuarios: todas as contas.</summary>
    [HttpGet]
    public Task<IReadOnlyList<UsuarioDto>> Listar() => usuarios.Listar();

    /// <summary>GET /api/usuarios/{id}: uma conta (404 se não existe).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Buscar(int id)
    {
        var usuario = await usuarios.BuscarPorId(id);
        return usuario is null ? NotFound() : usuario;
    }

    /// <summary>POST /api/usuarios: cria a conta e devolve 201 com o endereço dela.</summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(NovoUsuarioDto novo)
    {
        var criado = await usuarios.Criar(novo);
        return CreatedAtAction(nameof(Buscar), new { id = criado.Id }, criado);
    }

    /// <summary>Desativa ou reativa a conta ({ "ativo": false }). Desativar derruba na hora as sessões abertas dela.</summary>
    [HttpPut("{id:int}/ativo")]
    public async Task<ActionResult<UsuarioDto>> DefinirAtivo(int id, AtivoDto dto)
        => await usuarios.DefinirAtivo(id, dto.Ativo, TokenHelper.IdUsuario(User)) is { } usuario ? usuario : NotFound();
}

/// <summary>O corpo do PUT /api/usuarios/{id}/ativo.</summary>
/// <param name="Ativo">verdadeiro reativa a conta; falso desativa e derruba as sessões abertas.</param>
public record AtivoDto(bool Ativo);