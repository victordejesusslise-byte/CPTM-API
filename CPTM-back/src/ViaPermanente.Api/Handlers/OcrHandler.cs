using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Handlers;

/// <summary>A placa do feixe lida pela foto (OCR com o Tesseract, no servidor). Quem faz: o operador de pátio, na chegada.</summary>
[ApiController]
[Route("api/ocr")]
[Authorize(Roles = $"{nameof(Cargo.OperadorPatio)},{nameof(Cargo.Supervisor)}")]
public class OcrHandler(OcrPlacaService ocr) : ControllerBase
{
    /// <summary>Lê a foto da placa (campo "foto"). Com idPedido, cada campo vem conferido com o pedido.</summary>
    [HttpPost("placas")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(15_000_000)]
    public async Task<ActionResult<LeituraOcrDto>> Ler(IFormFile foto, [FromForm] int? idPedido, [FromForm] Guid? idCliente)
    {
        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria);
        return StatusCode(StatusCodes.Status201Created, await ocr.Ler(memoria.ToArray(), Path.GetExtension(foto.FileName), idPedido, TokenHelper.IdUsuario(User), idCliente));
    }

    [HttpGet("placas/{id:int}")]
    public async Task<ActionResult<LeituraOcrDto>> Buscar(int id)
    {
        var leitura = await ocr.BuscarPorId(id);
        return leitura is null ? NotFound() : leitura;
    }

    /// <summary>Os valores que o operador confirmou (uma vez); o corrigido vira amostra de treino.</summary>
    [HttpPost("placas/{id:int}/confirmacao")]
    public async Task<ActionResult<LeituraOcrDto>> Confirmar(int id, ConfirmarPlacaDto dto)
    {
        var leitura = await ocr.Confirmar(id, dto);
        return leitura is null ? NotFound() : leitura;
    }

    [HttpGet("modelos")]
    public Task<IReadOnlyList<ModeloOcrDto>> Modelos() => ocr.Modelos();
}