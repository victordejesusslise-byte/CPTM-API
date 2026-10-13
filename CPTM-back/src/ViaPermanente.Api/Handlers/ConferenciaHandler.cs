using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Armazens;
using ViaPermanente.Dominio.Armazens.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Handlers;

/// <summary>A conferência do armazém com o leitor, pilha a pilha.</summary>
[ApiController]
[Route("api")]
[Authorize(Roles = $"{nameof(Cargo.OperadorPatio)},{nameof(Cargo.Supervisor)},{nameof(Cargo.Especialista)}")]
public class ConferenciaHandler(ConferenciaService conferencias) : ControllerBase
{
    /// <summary>Abre (ou devolve a aberta) a conferência do armazém.</summary>
    [HttpPost("armazens/{idArmazem:int}/conferencias")]
    public async Task<ActionResult<ConferenciaDto>> Iniciar(int idArmazem, [FromQuery] int? idLeitor)
    {
        var c = await conferencias.Iniciar(idArmazem, idLeitor, TokenHelper.IdUsuario(User));
        return c is null ? NotFound() : c;
    }

    [HttpGet("conferencias/{id:int}")]
    public async Task<ActionResult<ConferenciaDto>> Buscar(int id)
    {
        var c = await conferencias.BuscarPorId(id);
        return c is null ? NotFound() : c;
    }

    /// <summary>As tags que responderam numa pilha.</summary>
    [HttpPost("conferencias/{id:int}/pilhas")]
    public async Task<ActionResult<ConferenciaDto>> LerPilha(int id, LerPilhaDto dto)
    {
        var c = await conferencias.LerPilha(id, dto, TokenHelper.IdUsuario(User));
        return c is null ? NotFound() : c;
    }

    /// <summary>Conclui: só com todas as pilhas lidas.</summary>
    [HttpPost("conferencias/{id:int}/conclusao")]
    public async Task<ActionResult<ConferenciaDto>> Concluir(int id)
    {
        var c = await conferencias.Concluir(id);
        return c is null ? NotFound() : c;
    }
}