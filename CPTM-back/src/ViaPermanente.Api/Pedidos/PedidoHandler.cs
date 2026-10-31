using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Pedidos;

/// <summary>
/// Etapa 01: pedido de compra. Quem faz: operador de pátio e supervisor. O especialista só lê a lista e o pedido, para as cargas e
/// os boletins da qualidade (Victor, 2026-10-31: a tela ficava carregando); as ações de cada rota que grava pedem o cargo de novo.
/// </summary>
[ApiController]
[Route("api/pedidos")]
[Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)},{nameof(Cargo.Especialista)}")]
public class PedidoHandler(PedidoService pedidos) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PedidoDto>> Listar() => pedidos.Listar();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDto>> Buscar(int id)
    {
        var pedido = await pedidos.BuscarPorId(id);
        return pedido is null ? NotFound() : pedido;
    }

    /// <summary>Registra o pedido. Acima do limite de 2ª classe, devolve 400 com a mensagem.</summary>
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    [HttpPost]
    public async Task<ActionResult<PedidoDto>> Criar(NovoPedidoDto novo)
    {
        var criado = await pedidos.Criar(novo, TokenHelper.IdUsuario(User));
        return CreatedAtAction(nameof(Buscar), new { id = criado.Id }, criado);
    }

    /// <summary>Confere a 2ª classe e a oferta do fornecedor sem gravar (o aviso da tela).</summary>
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    [HttpPost("verificacao")]
    public Task<VerificacaoPedidoDto> Verificar(VerificarPedidoDto pedido) => pedidos.Verificar(pedido);

    /// <summary>Corrige contrato, data e itens, com motivo, só antes da primeira chegada; grava o antes e o depois.</summary>
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    [HttpPost("{id:int}/correcao")]
    public async Task<ActionResult<PedidoDto>> Corrigir(int id, CorrigirPedidoDto dto)
    {
        var pedido = await pedidos.Corrigir(id, dto, TokenHelper.IdUsuario(User));
        return pedido is null ? NotFound() : pedido;
    }

    /// <summary>As correções e o cancelamento, com os retratos de antes e de depois.</summary>
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IReadOnlyList<PedidoHistoricoDto>>> Historico(int id)
    {
        var historico = await pedidos.Historico(id);
        return historico is null ? NotFound() : Ok(historico);
    }

    /// <summary>Cancela com motivo, só antes da primeira chegada; grava o histórico.</summary>
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    [HttpPost("{id:int}/cancelamento")]
    public async Task<ActionResult<PedidoDto>> Cancelar(int id, CancelarPedidoDto dto)
    {
        var pedido = await pedidos.Cancelar(id, dto.Motivo, TokenHelper.IdUsuario(User));
        return pedido is null ? NotFound() : pedido;
    }
}