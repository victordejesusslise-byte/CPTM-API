using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Catalogos;

/// <summary>
/// Os parâmetros do sistema (ex.: troca_quem_aprova, o limite de 2ª classe). Rotas em /api/parametros.
/// Ver: qualquer cargo logado; mudar: só o Supervisor.
/// </summary>
[ApiController]
[Route("api/parametros")]
public class ParametroHandler(ParametroService parametros) : ControllerBase
{
    /// <summary>Todos os parâmetros, com o valor atual.</summary>
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