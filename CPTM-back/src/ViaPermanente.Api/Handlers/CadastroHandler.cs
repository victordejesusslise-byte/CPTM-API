using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Handlers;

/// <summary>Cadastros de apoio. Quem está logado consulta; só o supervisor cadastra.</summary>
[ApiController]
[Route("api")]
public class CadastroHandler(CadastroService cadastros) : ControllerBase
{
    [HttpGet("empresas")]
    public Task<IReadOnlyList<EmpresaDto>> Empresas([FromQuery] PapelEmpresa? papel) => cadastros.Empresas(papel);

    [HttpPost("empresas")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<EmpresaDto>> NovaEmpresa(NovaEmpresaDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovaEmpresa(dto));

    /// <summary>Ativa ou desativa (desativar não apaga).</summary>
    [HttpPut("empresas/{id:int}/ativo")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<IActionResult> Ativar(int id, [FromQuery] bool ativo) => await cadastros.AtivarEmpresa(id, ativo) ? NoContent() : NotFound();

    [HttpGet("contratos")]
    public Task<IReadOnlyList<ContratoDto>> Contratos([FromQuery] int? idEmpresa) => cadastros.Contratos(idEmpresa);

    [HttpPost("contratos")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<ContratoDto>> NovoContrato(NovoContratoDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovoContrato(dto));

    [HttpGet("estacoes")]
    public Task<IReadOnlyList<EstacaoDto>> Estacoes() => cadastros.Estacoes();

    [HttpPost("estacoes")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<EstacaoDto>> NovaEstacao(NovaEstacaoDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovaEstacao(dto));

    [HttpGet("trechos")]
    public Task<IReadOnlyList<TrechoDto>> Trechos() => cadastros.Trechos();

    [HttpPost("trechos")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<TrechoDto>> NovoTrecho(NovoTrechoDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovoTrecho(dto));
}