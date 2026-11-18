using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Armazens.Dtos;
using ViaPermanente.Dominio.Armazens;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Pecas;

/// <summary>
/// A peça: cadastro (03), verificação de qualidade (04) e entrada no armazém (06). Rotas em /api/dormentes.
/// Consultar: qualquer cargo logado. Cadastrar e guardar: Operador de pátio e Supervisor. Verificar: Especialista e Supervisor.
/// </summary>
[ApiController]
[Route("api/dormentes")]
public class DormenteHandler(PecaService pecas, QualidadeService qualidade, ArmazemService armazem, HistoricoService historico) : ControllerBase
{
    /// <summary>O histórico da peça, da chegada até hoje, em ordem de data.</summary>
    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IReadOnlyList<EventoDaPecaDto>>> Historico(int id)
    {
        var eventos = await historico.DaPeca(id);
        return eventos is null ? NotFound() : Ok(eventos);
    }

    /// <summary>As peças de um feixe (o andamento do cadastro).</summary>
    [HttpGet]
    public Task<IReadOnlyList<PecaDto>> DoFeixe([FromQuery] int idFeixe) => pecas.DoFeixe(idFeixe);

    /// <summary>A peça pelo código impresso nela (DM-E01-L02-0034): é como o técnico acha a peça no campo.</summary>
    [HttpGet("codigo/{codigo}")]
    public async Task<ActionResult<PecaDto>> BuscarPorCodigo(string codigo)
    {
        var peca = await pecas.BuscarPorCodigo(codigo);
        return peca is null ? NotFound() : peca;
    }

    /// <summary>A peça pelo id (→ DORMENTE); 404 se não existe.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PecaDto>> Buscar(int id)
    {
        var peca = await pecas.BuscarPorId(id);
        return peca is null ? NotFound() : peca;
    }

    /// <summary>
    /// Cadastra a peça com a tag cravada. 201 com a peça quando a gravação vale; 409 com o resultado
    /// (Bloqueada, Falhou, Divergente) quando não vincula — a tentativa fica registrada.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    public async Task<ActionResult<CadastroPecaDto>> Cadastrar(NovaPecaDto nova)
    {
        var resultado = await pecas.Cadastrar(nova, TokenHelper.IdUsuario(User));
        // "Repetido" (o registro já tinha chegado antes) volta 200; sem peça, 409; com peça, 201.
        return resultado.Peca is null && resultado.Resultado != "Repetido"
            ? Conflict(resultado)
            : StatusCode(resultado.Resultado == "Repetido" ? StatusCodes.Status200OK : StatusCodes.Status201Created, resultado);
    }

    /// <summary>Verificação de qualidade com a peça lida. Reprovada: motivo e foto; a tag é desativada.</summary>
    [HttpPost("{id:int}/verificacao")]
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.Especialista)}")]
    public async Task<ActionResult<PecaDto>> Verificar(int id, VerificarPecaDto dto)
    {
        var peca = await qualidade.Verificar(id, dto, TokenHelper.IdUsuario(User));
        return peca is null ? NotFound() : peca;
    }

    /// <summary>Muda a peça de pilha (do mesmo tamanho e classe), lendo a peça.</summary>
    [HttpPost("{id:int}/pilha")]
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    public async Task<ActionResult<PecaDto>> TrocarPilha(int id, TrocarPilhaDto dto)
    {
        var peca = await armazem.TrocarPilha(id, dto, TokenHelper.IdUsuario(User));
        return peca is null ? NotFound() : peca;
    }

    /// <summary>Guarda a peça aprovada numa pilha do tamanho e da classe dela (a sugerida, se não escolher).</summary>
    [HttpPost("{id:int}/armazem")]
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    public async Task<ActionResult<PecaDto>> Guardar(int id, GuardarPecaDto dto)
    {
        var peca = await armazem.Guardar(id, dto, TokenHelper.IdUsuario(User));
        return peca is null ? NotFound() : peca;
    }

    /// <summary>O lote da peça da tag lida, separado por tamanho e classe, para guardar tudo de uma vez (Victor, 2026-11-17). 404: tag sem peça.</summary>
    [HttpGet("lote-da-tag/{tag}")]
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    public async Task<ActionResult<LoteParaGuardarDto>> LoteDaTag(string tag)
    {
        var lote = await armazem.LoteDaTag(tag);
        return lote is null ? NotFound() : lote;
    }

    /// <summary>Guarda o lote: cada grupo (tamanho e classe) na pilha escolhida, todas no mesmo armazém; só as aprovadas sem pilha entram.</summary>
    [HttpPost("lote/armazem")]
    [Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
    public async Task<ActionResult<LoteParaGuardarDto>> GuardarLote(GuardarLoteDto dto)
        => await armazem.GuardarLote(dto, TokenHelper.IdUsuario(User));
}