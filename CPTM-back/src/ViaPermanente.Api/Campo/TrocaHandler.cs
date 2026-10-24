using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Campo;

/// <summary>Etapas 11 a 13: a troca em quatro passos (técnico pede, supervisor aprova, pátio separa, via troca).</summary>
[ApiController]
[Route("api/trocas")]
public class TrocaHandler(TrocaService trocas, AvisoAoVivo aoVivo) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TrocaDto>> Listar([FromQuery] StatusTroca? status) => trocas.Listar(status);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrocaDto>> Buscar(int id)
    {
        var troca = await trocas.BuscarPorId(id);
        return troca is null ? NotFound() : troca;
    }

    [HttpPost]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<TrocaDto>> Pedir(NovaTrocaDto dto)
        => StatusCode(StatusCodes.Status201Created, await trocas.Pedir(dto, TokenHelper.IdUsuario(User)));

    /// <summary>Aprovar ou reprovar: o cargo que aprova vem de PARAMETRO troca_quem_aprova (hoje, supervisor).</summary>
    [HttpPost("{id:int}/decisao")]
    public async Task<ActionResult<TrocaDto>> Decidir(int id, DecisaoTrocaDto dto)
    {
        var cargo = Enum.Parse<Cargo>(User.FindFirstValue(TokenHelper.ClaimCargo)!);
        var troca = await trocas.Decidir(id, dto, cargo, TokenHelper.IdUsuario(User));
        await aoVivo.Troca(troca);
        return troca is null ? NotFound() : troca;
    }

    /// <summary>As peças compatíveis; a primeira é a sugerida (armazém mais perto e, nele, a que entrou primeiro).</summary>
    [HttpGet("{id:int}/compativeis")]
    [Authorize(Roles = $"{nameof(Cargo.OperadorPatio)},{nameof(Cargo.Supervisor)}")]
    public async Task<ActionResult<IReadOnlyList<CompativelDto>>> Compativeis(int id)
    {
        var lista = await trocas.ListarCompativeis(id);
        return lista is null ? NotFound() : Ok(lista);
    }

    [HttpPost("{id:int}/separacao")]
    [Authorize(Roles = nameof(Cargo.OperadorPatio))]
    public async Task<ActionResult<TrocaDto>> Separar(int id, SepararTrocaDto dto)
    {
        var troca = await trocas.Separar(id, dto, TokenHelper.IdUsuario(User));
        await aoVivo.Troca(troca);
        return troca is null ? NotFound() : troca;
    }

    [HttpPost("{id:int}/execucao")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    public async Task<ActionResult<TrocaDto>> Executar(int id, ExecutarTrocaDto dto)
    {
        var troca = await trocas.Executar(id, dto, TokenHelper.IdUsuario(User));
        await aoVivo.Troca(troca);
        return troca is null ? NotFound() : troca;
    }

    /// <summary>A foto da troca (peça nova no lugar, ou a da recusa): devolve o caminho, que vai no fotoArquivo da execução ou da recusa.</summary>
    [HttpPost("{id:int}/foto")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FotoPecaService.TamanhoMaximo + 100_000)]
    public async Task<ActionResult<object>> EnviarFoto(int id, IFormFile foto, [FromServices] FotoTrocaService fotos)
    {
        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria);
        var arquivo = await fotos.Salvar(id, memoria.ToArray(), Path.GetExtension(foto.FileName));
        return arquivo is null ? NotFound() : StatusCode(StatusCodes.Status201Created, new { arquivo });
    }

    [HttpPost("{id:int}/recusa")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    public async Task<ActionResult<TrocaDto>> Recusar(int id, RecusarTrocaDto dto)
    {
        var troca = await trocas.Recusar(id, dto, TokenHelper.IdUsuario(User));
        await aoVivo.Troca(troca);
        return troca is null ? NotFound() : troca;
    }

    /// <summary>O técnico mantém (com resposta) ou aceita a recusa.</summary>
    [HttpPost("{id:int}/recusa/decisao")]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<TrocaDto>> DecidirRecusa(int id, DecidirRecusaDto dto)
    {
        var troca = await trocas.DecidirRecusa(id, dto, TokenHelper.IdUsuario(User));
        await aoVivo.Troca(troca);
        return troca is null ? NotFound() : troca;
    }
}