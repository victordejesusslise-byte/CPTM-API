using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Pecas;

/// <summary>A tag UHF: o próximo número a gravar e abrir a peça pela tag.</summary>
[ApiController]
[Route("api/tags")]
public class TagHandler(PecaService pecas, VinculoTagService vinculos) : ControllerBase
{
    /// <summary>Tag sem cadastro achada em campo: o operador da via avisa o técnico.</summary>
    [HttpPost("avisos")]
    [Authorize(Roles = $"{nameof(Cargo.OperadorVia)},{nameof(Cargo.Tecnico)}")]
    public async Task<ActionResult<AvisoTagDto>> Avisar(NovoAvisoTagDto dto)
        => StatusCode(StatusCodes.Status201Created, await vinculos.Avisar(dto, TokenHelper.IdUsuario(User)));

    [HttpGet("avisos")]
    [Authorize(Roles = $"{nameof(Cargo.Tecnico)},{nameof(Cargo.Supervisor)}")]
    public Task<IReadOnlyList<AvisoTagDto>> Avisos() => vinculos.Pendentes();

    /// <summary>Vincula a tag lida no local à peça; a tag velha fica inativa.</summary>
    [HttpPost("vinculos")]
    [Authorize(Roles = $"{nameof(Cargo.Tecnico)},{nameof(Cargo.Supervisor)}")]
    public Task<PecaDto> Vincular(VincularTagDto dto) => vinculos.Vincular(dto, TokenHelper.IdUsuario(User));

    /// <summary>O próximo número (BR e a sequência) para o módulo gravar.</summary>
    [HttpGet("proxima")]
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    public async Task<object> Proxima() => new { tag = await pecas.ProximaTag() };

    /// <summary>Abre a peça pela tag. 404: tag não associada.</summary>
    [HttpGet("{numero}")]
    public async Task<ActionResult<PecaDto>> Buscar(string numero)
    {
        var peca = await pecas.BuscarPorTag(numero);
        return peca is null ? Problem($"A tag {numero} não está associada a nenhuma peça.", statusCode: StatusCodes.Status404NotFound) : peca;
    }
}