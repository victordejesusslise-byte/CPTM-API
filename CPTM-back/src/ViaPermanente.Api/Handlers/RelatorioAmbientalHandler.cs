using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Ambiental;
using ViaPermanente.Dominio.Dtos.Integracoes;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>O relatório ambiental mensal para o IBAMA. Quem faz: o especialista (e o supervisor).</summary>
[ApiController]
[Route("api/relatorios-ambientais")]
[Authorize(Roles = $"{nameof(Cargo.Especialista)},{nameof(Cargo.Supervisor)}")]
public class RelatorioAmbientalHandler(RelatorioAmbientalService relatorios, ExportacaoService exportacao) : ControllerBase
{
    /// <summary>A planilha do relatório para o site do IBAMA (modelo provisório).</summary>
    [HttpGet("{id:int}/planilha")]
    public async Task<IActionResult> Planilha(int id)
        => await exportacao.Ibama(id) is { } arquivo ? File(arquivo.Conteudo, ArquivoDto.TipoXlsx, arquivo.Nome) : NotFound();

    [HttpGet]
    public Task<IReadOnlyList<RelatorioDto>> Listar() => relatorios.Listar();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RelatorioDto>> Buscar(int id)
    {
        var relatorio = await relatorios.BuscarPorId(id);
        return relatorio is null ? NotFound() : relatorio;
    }

    /// <summary>Gera (ou gera de novo) o rascunho do mês. Ex.: ?mes=2026-10-01.</summary>
    [HttpPost]
    public async Task<ActionResult<RelatorioDto>> Gerar([FromQuery] DateOnly mes)
        => StatusCode(StatusCodes.Status201Created, await relatorios.Gerar(mes, TokenHelper.IdUsuario(User)));

    /// <summary>Envia: o relatório fica congelado.</summary>
    [HttpPost("{id:int}/envio")]
    public async Task<ActionResult<RelatorioDto>> Enviar(int id)
    {
        var relatorio = await relatorios.Enviar(id);
        return relatorio is null ? NotFound() : relatorio;
    }
}