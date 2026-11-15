using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Catalogos;

/// <summary>
/// Cadastros de apoio. Quem está logado consulta; só o supervisor cadastra.
/// Rotas em /api: empresas (e as medidas de cada fornecedor), contratos, estações e trechos.
/// </summary>
[ApiController]
[Route("api")]
public class CadastroHandler(CadastroService cadastros) : ControllerBase
{
    /// <summary>As empresas, com filtro opcional pelo papel (fornecedor, tratadora ou inspetora).</summary>
    [HttpGet("empresas")]
    public Task<IReadOnlyList<EmpresaDto>> Empresas([FromQuery] PapelEmpresa? papel) => cadastros.Empresas(papel);

    /// <summary>Cadastra uma empresa.</summary>
    [HttpPost("empresas")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<EmpresaDto>> NovaEmpresa(NovaEmpresaDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovaEmpresa(dto));

    /// <summary>Ativa ou desativa (desativar não apaga).</summary>
    [HttpPut("empresas/{id:int}/ativo")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<IActionResult> Ativar(int id, [FromQuery] bool ativo) => await cadastros.AtivarEmpresa(id, ativo) ? NoContent() : NotFound();

    /// <summary>Muda a sigla da empresa (TG), que começa a tag do fornecedor; vazia, fica sem.</summary>
    [HttpPut("empresas/{id:int}/sigla")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<EmpresaDto>> MudarSigla(int id, AlterarSiglaDto dto)
        => await cadastros.MudarSigla(id, dto.Sigla) is { } empresa ? empresa : NotFound();

    /// <summary>Mais um padrão de medida (largura × altura) para o fornecedor.</summary>
    [HttpPost("empresas/{id:int}/medidas")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<MedidaDto>> NovaMedida(int id, NovaMedidaDto dto)
        => await cadastros.NovaMedida(id, dto) is { } medida ? StatusCode(StatusCodes.Status201Created, medida) : NotFound();

    /// <summary>Ativa ou desativa um padrão de medida (desativar não apaga: os pedidos que já usam continuam com ele).</summary>
    [HttpPut("medidas/{id:int}/ativo")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<IActionResult> AtivarMedida(int id, [FromQuery] bool ativo) => await cadastros.AtivarMedida(id, ativo) ? NoContent() : NotFound();

    /// <summary>Os contratos, com filtro opcional pela empresa.</summary>
    [HttpGet("contratos")]
    public Task<IReadOnlyList<ContratoDto>> Contratos([FromQuery] int? idEmpresa) => cadastros.Contratos(idEmpresa);

    /// <summary>Cadastra um contrato.</summary>
    [HttpPost("contratos")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<ContratoDto>> NovoContrato(NovoContratoDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovoContrato(dto));

    /// <summary>As estações.</summary>
    [HttpGet("estacoes")]
    public Task<IReadOnlyList<EstacaoDto>> Estacoes() => cadastros.Estacoes();

    /// <summary>Cadastra uma estação.</summary>
    [HttpPost("estacoes")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<EstacaoDto>> NovaEstacao(NovaEstacaoDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovaEstacao(dto));

    /// <summary>Os trechos da via.</summary>
    [HttpGet("trechos")]
    public Task<IReadOnlyList<TrechoDto>> Trechos() => cadastros.Trechos();

    /// <summary>Cadastra um trecho.</summary>
    [HttpPost("trechos")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<TrechoDto>> NovoTrecho(NovoTrechoDto dto) => StatusCode(StatusCodes.Status201Created, await cadastros.NovoTrecho(dto));
}