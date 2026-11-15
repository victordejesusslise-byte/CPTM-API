using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Campo;

/// <summary>
/// Etapa 10: a inspeção (checklist v2). Quem faz: o técnico. Rotas em /api/inspecoes.
/// Registrar e corrigir: só o Técnico; consultar: qualquer cargo logado (decisão: Corrigir inspeção não é refazer).
/// </summary>
[ApiController]
[Route("api/inspecoes")]
public class InspecaoHandler(InspecaoService inspecoes, AvisoAoVivo aoVivo) : ControllerBase
{
    /// <summary>Registra o checklist com a peça lida e a foto; fecha os alertas abertos da peça.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<InspecaoDto>> Registrar(NovaInspecaoDto dto)
    {
        var inspecao = await inspecoes.Registrar(dto, TokenHelper.IdUsuario(User));
        // A inspeção fecha o alerta: avisa ao vivo (decisão: Confirmar alerta é fazer o checklist).
        await aoVivo.Avisar(AvisoAoVivo.AlertaFechado, dto.IdAlerta);
        return StatusCode(StatusCodes.Status201Created, inspecao);
    }

    /// <summary>Correção: só quem fez, com motivo. Se passa a aprovar, cancela a troca aberta pela inspeção.</summary>
    [HttpPost("{id:int}/correcao")]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<InspecaoDto>> Corrigir(int id, CorrigirInspecaoDto dto)
    {
        var inspecao = await inspecoes.Corrigir(id, dto, TokenHelper.IdUsuario(User));
        // Avisa sempre: se passou a aprovar, a troca aberta pela inspeção foi cancelada e sai do plano.
        await aoVivo.Avisar(AvisoAoVivo.TrocaNoPlano, null);
        return inspecao is null ? NotFound() : inspecao;
    }

    /// <summary>Uma inspeção pelo id (→ INSPECAO); 404 se não existe.</summary>
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