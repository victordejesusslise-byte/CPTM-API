using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Parametros;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

[ApiController]
[Route("api/parametros")]
public class ParametroHandler(ParametroService parametros) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ParametroDto>> Listar() => parametros.Listar();

    /// <summary>Só o supervisor muda parâmetro.</summary>
    [HttpPut("{chave}")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<ParametroDto>> Alterar(string chave, AlterarParametroDto dto)
    {
        var alterado = await parametros.Alterar(chave, dto.Valor, TokenHelper.IdUsuario(User));
        return alterado is null ? NotFound() : alterado;
    }
}