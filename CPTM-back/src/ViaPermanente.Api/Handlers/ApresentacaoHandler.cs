using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Apresentacao;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>Para apresentar o sistema: o caminho da peça, etapa por etapa, com o que o banco tem.</summary>
[ApiController]
[Route("api/apresentacao")]
public class ApresentacaoHandler(ApresentacaoService apresentacao, IWebHostEnvironment ambiente) : ControllerBase
{
    /// <summary>As cinco etapas, do pedido ao armazém. Sem idPedido, conta o banco inteiro.</summary>
    [HttpGet("fluxo")]
    public async Task<ActionResult<IReadOnlyList<EtapaFluxoDto>>> Fluxo([FromQuery] int? idPedido)
    {
        var etapas = await apresentacao.Fluxo(idPedido);
        return etapas is null ? NotFound() : Ok(etapas);
    }

    /// <summary>Cria um pedido de exemplo que passa pelas cinco etapas. Só em desenvolvimento e só o supervisor.</summary>
    [HttpPost("exemplo")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<ExemploCriadoDto>> CriarExemplo()
    {
        if (!ambiente.IsDevelopment())
            return NotFound();
        return await apresentacao.CriarExemplo(TokenHelper.IdUsuario(User));
    }
}