using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Pecas;

/// <summary>As fotos da peça: a da entrada (enviada na verificação de qualidade) e a lista para comparar com a câmera da via.</summary>
[ApiController]
[Route("api")]
public class FotoHandler(FotoPecaService fotos) : ControllerBase
{
    /// <summary>Guarda a foto da entrada da peça e devolve o caminho, que vai no fotoArquivo da verificação.</summary>
    [HttpPost("dormentes/{id:int}/fotos")]
    [Authorize(Roles = $"{nameof(Cargo.Especialista)},{nameof(Cargo.Supervisor)}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FotoPecaService.TamanhoMaximo + 100_000)]
    public async Task<ActionResult<object>> Enviar(int id, IFormFile foto)
    {
        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria);
        return StatusCode(StatusCodes.Status201Created, new { arquivo = await fotos.SalvarEntrada(id, memoria.ToArray(), Path.GetExtension(foto.FileName)) });
    }

    /// <summary>A foto da peça na inspeção (técnico): devolve o caminho, que vai no fotoArquivo da inspeção.</summary>
    [HttpPost("dormentes/{id:int}/fotos/inspecao")]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FotoPecaService.TamanhoMaximo + 100_000)]
    public async Task<ActionResult<object>> EnviarDaInspecao(int id, IFormFile foto, [FromServices] FotoCampoService campo)
    {
        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria);
        var arquivo = await campo.DaInspecao(id, memoria.ToArray(), Path.GetExtension(foto.FileName));
        return arquivo is null ? NotFound() : StatusCode(StatusCodes.Status201Created, new { arquivo });
    }

    [HttpGet("dormentes/{id:int}/fotos")]
    public async Task<ActionResult<FotosDaPecaDto>> DaPeca(int id) => await fotos.DaPeca(id) is { } lista ? lista : NotFound();

    /// <summary>O arquivo da foto pelo caminho guardado no banco (?caminho=pecas/12/entrada/2026/10/x.jpg).</summary>
    [HttpGet("fotos")]
    public IActionResult Arquivo([FromQuery] string caminho)
    {
        var conteudo = fotos.Arquivo(caminho);
        if (conteudo is null) return NotFound();
        var tipo = Path.GetExtension(caminho).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
        return File(conteudo, tipo);
    }
}
