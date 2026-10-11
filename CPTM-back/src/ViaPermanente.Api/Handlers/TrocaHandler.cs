using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Trocas;
using ViaPermanente.Dominio.Models.Campo;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>Etapas 11 a 13: a troca em quatro passos (técnico pede, supervisor aprova, pátio separa, via troca).</summary>
[ApiController]
[Route("api/trocas")]
public class TrocaHandler(TrocaService trocas) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TrocaDto>> Listar([FromQuery] StatusTroca? status) => trocas.Listar(status);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrocaDto>> Buscar(int id)
    {
        var troca = await trocas.BuscarPorId(id);
        return troca is null ? NotFound() : troca;
    }

    [HttpPost]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<TrocaDto>> Pedir(NovaTrocaDto dto)
        => StatusCode(StatusCodes.Status201Created, await trocas.Pedir(dto, TokenHelper.IdUsuario(User)));

    /// <summary>Aprovar ou reprovar: o cargo que aprova vem de PARAMETRO troca_quem_aprova (hoje, supervisor).</summary>
    [HttpPost("{id:int}/decisao")]
    public async Task<ActionResult<TrocaDto>> Decidir(int id, DecisaoTrocaDto dto)
    {
        var cargo = Enum.Parse<Cargo>(User.FindFirstValue(TokenHelper.ClaimCargo)!);
        var troca = await trocas.Decidir(id, dto, cargo, TokenHelper.IdUsuario(User));
        return troca is null ? NotFound() : troca;
    }

    /// <summary>As peças compatíveis; a primeira é a sugerida (armazém mais perto, a da frente).</summary>
    [HttpGet("{id:int}/compativeis")]
    [Authorize(Roles = $"{nameof(Cargo.OperadorPatio)},{nameof(Cargo.Supervisor)}")]
    public async Task<ActionResult<IReadOnlyList<CompativelDto>>> Compativeis(int id)
    {
        var lista = await trocas.ListarCompativeis(id);
        return lista is null ? NotFound() : Ok(lista);
    }

    [HttpPost("{id:int}/separacao")]
    [Authorize(Roles = nameof(Cargo.OperadorPatio))]
    public async Task<ActionResult<TrocaDto>> Separar(int id, SepararTrocaDto dto)
    {
        var troca = await trocas.Separar(id, dto, TokenHelper.IdUsuario(User));
        return troca is null ? NotFound() : troca;
    }

    [HttpPost("{id:int}/execucao")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    public async Task<ActionResult<TrocaDto>> Executar(int id, ExecutarTrocaDto dto)
    {
        var troca = await trocas.Executar(id, dto, TokenHelper.IdUsuario(User));
        return troca is null ? NotFound() : troca;
    }

    [HttpPost("{id:int}/recusa")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    public async Task<ActionResult<TrocaDto>> Recusar(int id, RecusarTrocaDto dto)
    {
        var troca = await trocas.Recusar(id, dto, TokenHelper.IdUsuario(User));
        return troca is null ? NotFound() : troca;
    }

    /// <summary>O técnico mantém (com resposta) ou aceita a recusa.</summary>
    [HttpPost("{id:int}/recusa/decisao")]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<TrocaDto>> DecidirRecusa(int id, DecidirRecusaDto dto)
    {
        var troca = await trocas.DecidirRecusa(id, dto, TokenHelper.IdUsuario(User));
        return troca is null ? NotFound() : troca;
    }
}