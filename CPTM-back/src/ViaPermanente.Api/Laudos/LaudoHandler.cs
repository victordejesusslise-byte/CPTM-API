using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Integracoes.Dtos;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Laudos.Models;

namespace ViaPermanente.Api.Laudos;

/// <summary>Os laudos em PDF, gerados na hora a partir das tabelas.</summary>
[ApiController]
[Route("api/laudos")]
public class LaudoHandler(LaudoService laudos) : ControllerBase
{
    /// <summary>BR: boletim de recebimento de uma chegada.</summary>
    [HttpGet("recebimento/{idRecebimento:int}")]
    public async Task<IActionResult> Recebimento(int idRecebimento) => Pdf(await laudos.BoletimRecebimento(idRecebimento));

    /// <summary>LI: laudo da inspeção.</summary>
    [HttpGet("inspecao/{idInspecao:int}")]
    public async Task<IActionResult> Inspecao(int idInspecao) => Pdf(await laudos.LaudoInspecao(idInspecao));

    /// <summary>LB: liberação (a ordem de troca), depois da aprovação.</summary>
    [HttpGet("liberacao/{idTroca:int}")]
    public async Task<IActionResult> Liberacao(int idTroca) => Pdf(await laudos.Liberacao(idTroca));

    /// <summary>LD: laudo da troca, com todos os passos até agora.</summary>
    [HttpGet("troca/{idTroca:int}")]
    public async Task<IActionResult> Troca(int idTroca) => Pdf(await laudos.LaudoTroca(idTroca));

    /// <summary>O laudo como documento (título, código, seções e tabelas), para a tela mostrar antes de baixar o PDF.</summary>
    [HttpGet("{tipo}/{id:int}/documento")]
    public async Task<ActionResult<DocumentoPdf>> Documento(string tipo, int id) => await laudos.Documento(tipo, id) is { } d ? d : NotFound();

    private IActionResult Pdf(ArquivoDto? arquivo) => arquivo is null ? NotFound() : File(arquivo.Conteudo, "application/pdf", arquivo.Nome);
}