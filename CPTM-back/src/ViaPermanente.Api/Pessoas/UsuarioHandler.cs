using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Pessoas;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = nameof(Cargo.Supervisor))]
public class UsuarioHandler(UsuarioService usuarios) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UsuarioDto>> Listar() => usuarios.Listar();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Buscar(int id)
    {
        var usuario = await usuarios.BuscarPorId(id);
        return usuario is null ? NotFound() : usuario;
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(NovoUsuarioDto novo)
    {
        var criado = await usuarios.Criar(novo);
        return CreatedAtAction(nameof(Buscar), new { id = criado.Id }, criado);
    }
}