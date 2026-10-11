using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Notas;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>As notas fiscais do pedido, conferidas com ele na hora.</summary>
[ApiController]
[Route("api/pedidos/{idPedido:int}/notas")]
[Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
public class NotaFiscalHandler(NotaFiscalService notas) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotaDto>>> Listar(int idPedido)
    {
        var lista = await notas.Listar(idPedido);
        return lista is null ? NotFound() : Ok(lista);
    }

    /// <summary>Registra a nota digitada. Volta com Confere ou Diverge, as divergências e os avisos.</summary>
    [HttpPost]
    public async Task<ActionResult<NotaDto>> Registrar(int idPedido, NovaNotaDto nova)
    {
        var nota = await notas.Registrar(idPedido, nova, TokenHelper.IdUsuario(User));
        return nota is null ? NotFound() : StatusCode(StatusCodes.Status201Created, nota);
    }
}