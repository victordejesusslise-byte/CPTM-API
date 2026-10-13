using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Handlers;

/// <summary>A oferta do fornecedor (m³ por espécie e tamanho), vista ao montar o pedido.</summary>
[ApiController]
[Route("api/ofertas")]
[Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
public class OfertaHandler(OfertaService ofertas) : ControllerBase
{
    /// <summary>As ofertas que valem hoje (a mais recente de cada espécie e tamanho).</summary>
    [HttpGet]
    public Task<IReadOnlyList<OfertaDto>> Listar([FromQuery] int? idEmpresa) => ofertas.ListarVigentes(idEmpresa);

    /// <summary>Registra uma oferta nova; a anterior fica no histórico.</summary>
    [HttpPost]
    public async Task<ActionResult<OfertaDto>> Registrar(NovaOfertaDto nova)
        => StatusCode(StatusCodes.Status201Created, await ofertas.Registrar(nova));
}