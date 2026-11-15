using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Catalogos;

/// <summary>
/// As listas fixas que as telas usam (tamanhos, espécies, modos de leitura, leitores e armazéns). Rotas em /api/catalogos.
/// Consultar: qualquer cargo logado; cadastrar leitor: só o Supervisor.
/// </summary>
[ApiController]
[Route("api/catalogos")]
public class CatalogoHandler(CatalogoService catalogo) : ControllerBase
{
    /// <summary>Os comprimentos de dormente da lista (em m).</summary>
    [HttpGet("tamanhos")]
    public Task<IReadOnlyList<TamanhoDto>> Tamanhos() => catalogo.ListarTamanhos();

    /// <summary>As espécies de madeira aceitas.</summary>
    [HttpGet("especies")]
    public Task<IReadOnlyList<EspecieDto>> Especies() => catalogo.ListarEspecies();

    /// <summary>Os modos de leitura do módulo (Armazém, Normal, Via), com a potência de cada um.</summary>
    [HttpGet("modos-leitura")]
    public Task<IReadOnlyList<ModoLeituraDto>> Modos() => catalogo.ListarModos();

    /// <summary>Os leitores cadastrados (pistola, módulo do celular ou carro).</summary>
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