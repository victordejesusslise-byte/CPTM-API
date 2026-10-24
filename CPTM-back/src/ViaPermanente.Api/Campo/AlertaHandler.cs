using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Campo;

/// <summary>Etapa 09: a fila de alertas. Técnico e operador da via veem e vão ao local; o supervisor só vê; só o técnico decide.</summary>
[ApiController]
[Route("api/alertas")]
[Authorize(Roles = $"{nameof(Cargo.Tecnico)},{nameof(Cargo.OperadorVia)},{nameof(Cargo.Supervisor)}")]
public class AlertaHandler(AlertaService alertas, AvisoAoVivo aoVivo) : ControllerBase
{
    /// <summary>A fila. origem: Imagem ou Sensor (as duas abas). status: Aberto, Confirmado ou Descartado.</summary>
    [HttpGet]
    public Task<IReadOnlyList<AlertaDto>> Listar([FromQuery] OrigemAlerta? origem, [FromQuery] StatusAlerta? status) => alertas.Listar(origem, status);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AlertaDto>> Buscar(int id)
    {
        var alerta = await alertas.BuscarPorId(id);
        return alerta is null ? NotFound() : alerta;
    }

    /// <summary>O que viu no local, com foto e a tag lida. Não fecha o alerta.</summary>
    [HttpPost("{id:int}/vistoria")]
    [Authorize(Roles = $"{nameof(Cargo.Tecnico)},{nameof(Cargo.OperadorVia)}")]
    public async Task<ActionResult<AlertaDto>> Vistoriar(int id, NovaVistoriaDto dto)
    {
        var alerta = await alertas.Vistoriar(id, dto, TokenHelper.IdUsuario(User));
        return alerta is null ? NotFound() : alerta;
    }

    /// <summary>A foto da vistoria: devolve o caminho, que vai no fotoArquivo da vistoria.</summary>
    [HttpPost("{id:int}/foto")]
    [Authorize(Roles = $"{nameof(Cargo.Tecnico)},{nameof(Cargo.OperadorVia)}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FotoPecaService.TamanhoMaximo + 100_000)]
    public async Task<ActionResult<object>> EnviarFoto(int id, IFormFile foto, [FromServices] FotoCampoService fotos)
    {
        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria);
        var arquivo = await fotos.DaVistoria(id, memoria.ToArray(), Path.GetExtension(foto.FileName));
        return arquivo is null ? NotFound() : StatusCode(StatusCodes.Status201Created, new { arquivo });
    }

    /// <summary>"Não confere": descarta, com motivo. "Confere" é abrir a inspeção (POST /api/inspecoes com o alerta).</summary>
    [HttpPost("{id:int}/descarte")]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<AlertaDto>> Descartar(int id, DescartarAlertaDto dto)
    {
        var alerta = await alertas.Descartar(id, dto.Motivo, TokenHelper.IdUsuario(User));
        if (alerta is not null)
            await aoVivo.Avisar(AvisoAoVivo.AlertaFechado, id);
        return alerta is null ? NotFound() : alerta;
    }
}