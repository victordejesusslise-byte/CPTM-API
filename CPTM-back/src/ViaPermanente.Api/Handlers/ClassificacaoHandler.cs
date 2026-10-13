using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Handlers;

/// <summary>Classificação da madeira (etapa 07): o especialista confirma ou corrige a espécie da placa.</summary>
[ApiController]
[Route("api/classificacoes")]
[Authorize(Roles = $"{nameof(Cargo.Especialista)},{nameof(Cargo.Supervisor)}")]
public class ClassificacaoHandler(ClassificacaoService classificacoes) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(Cargo.Especialista))]
    public async Task<ActionResult<ClassificacaoDto>> Classificar(NovaClassificacaoDto dto)
        => StatusCode(StatusCodes.Status201Created, await classificacoes.Classificar(dto, TokenHelper.IdUsuario(User)));

    /// <summary>?idDormente= para as da peça (a mais nova primeiro); sem ele, as feitas nos últimos ?dias= (padrão 30).</summary>
    [HttpGet]
    public Task<IReadOnlyList<ClassificacaoDto>> Listar([FromQuery] int? idDormente, [FromQuery] int dias = 30)
        => idDormente is { } id ? classificacoes.DaPeca(id) : classificacoes.Recentes(dias);
}
