using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Dtos.Usuarios;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

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