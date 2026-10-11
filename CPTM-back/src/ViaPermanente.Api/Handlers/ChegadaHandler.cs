using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Chegadas;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>Etapa 02: a chegada da carga do pedido. Quem faz: operador de pátio.</summary>
[ApiController]
[Route("api/pedidos/{idPedido:int}/chegadas")]
[Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
public class ChegadaHandler(ChegadaService chegadas) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChegadaDto>>> Listar(int idPedido)
    {
        var lista = await chegadas.Listar(idPedido);
        return lista is null ? NotFound() : Ok(lista);
    }

    /// <summary>Registra a chegada com os feixes. Volta com a conferência (nota × informado × feixes, e feixes × pedido).</summary>
    [HttpPost]
    public async Task<ActionResult<ChegadaDto>> Registrar(int idPedido, NovaChegadaDto nova)
    {
        var chegada = await chegadas.Registrar(idPedido, nova, TokenHelper.IdUsuario(User));
        return chegada is null ? NotFound() : StatusCode(StatusCodes.Status201Created, chegada);
    }
}