using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Dtos.Catalogos;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

[ApiController]
[Route("api/catalogos")]
public class CatalogoHandler(CatalogoService catalogo) : ControllerBase
{
    [HttpGet("tamanhos")]
    public Task<IReadOnlyList<TamanhoDto>> Tamanhos() => catalogo.ListarTamanhos();

    [HttpGet("especies")]
    public Task<IReadOnlyList<EspecieDto>> Especies() => catalogo.ListarEspecies();

    [HttpGet("modos-leitura")]
    public Task<IReadOnlyList<ModoLeituraDto>> Modos() => catalogo.ListarModos();

    [HttpGet("leitores")]
    public Task<IReadOnlyList<LeitorDto>> Leitores() => catalogo.ListarLeitores();

    /// <summary>Cadastra um leitor (pistola, módulo do celular ou carro). Só o supervisor.</summary>
    [HttpPost("leitores")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<LeitorDto>> RegistrarLeitor(NovoLeitorDto novo)
        => StatusCode(StatusCodes.Status201Created, await catalogo.RegistrarLeitor(novo));

    /// <summary>Os armazéns, cada um com as pilhas e quantas peças estão nelas.</summary>
    [HttpGet("armazens")]
    public Task<IReadOnlyList<ArmazemDto>> Armazens() => catalogo.ListarArmazens();
}