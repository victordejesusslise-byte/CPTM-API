using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;

namespace ViaPermanente.Api.Via;

/// <summary>Consulta das passagens (a gravação, do carro, fica no PassagemHandler).</summary>
[ApiController]
[Route("api/passagens")]
[Authorize(Roles = $"{nameof(Cargo.OperadorVia)},{nameof(Cargo.Tecnico)},{nameof(Cargo.Supervisor)}")]
public class PassagemConsultaHandler(PassagemConsultaService passagens) : ControllerBase
{
    /// <summary>As últimas passagens com o resumo (?quantas=, padrão 30).</summary>
    [HttpGet]
    public Task<IReadOnlyList<PassagemDto>> Listar([FromQuery] int quantas = 30) => passagens.Ultimas(quantas);

    /// <summary>A passagem com as leituras, as com problema primeiro.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PassagemDetalheDto>> Buscar(int id)
    {
        var passagem = await passagens.Buscar(id);
        return passagem is null ? NotFound() : passagem;
    }
}
