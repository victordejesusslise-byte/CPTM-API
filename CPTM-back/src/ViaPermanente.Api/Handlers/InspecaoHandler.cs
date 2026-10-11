using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Inspecoes;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>Etapa 10: a inspeção (checklist v2). Quem faz: o técnico.</summary>
[ApiController]
[Route("api/inspecoes")]
public class InspecaoHandler(InspecaoService inspecoes) : ControllerBase
{
    /// <summary>Registra o checklist com a peça lida e a foto; fecha os alertas abertos da peça.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<InspecaoDto>> Registrar(NovaInspecaoDto dto)
        => StatusCode(StatusCodes.Status201Created, await inspecoes.Registrar(dto, TokenHelper.IdUsuario(User)));

    /// <summary>Correção: só quem fez, com motivo. Se passa a aprovar, cancela a troca aberta pela inspeção.</summary>
    [HttpPost("{id:int}/correcao")]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<InspecaoDto>> Corrigir(int id, CorrigirInspecaoDto dto)
    {
        var inspecao = await inspecoes.Corrigir(id, dto, TokenHelper.IdUsuario(User));
        return inspecao is null ? NotFound() : inspecao;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InspecaoDto>> Buscar(int id)
    {
        var inspecao = await inspecoes.BuscarPorId(id);
        return inspecao is null ? NotFound() : inspecao;
    }

    /// <summary>As inspeções da peça, da mais nova para a mais antiga.</summary>
    [HttpGet]
    public Task<IReadOnlyList<InspecaoDto>> DaPeca([FromQuery] int idDormente) => inspecoes.DaPeca(idDormente);
}