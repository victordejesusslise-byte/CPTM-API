using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Campo;

/// <summary>
/// O lote de troca (Victor, 2026-11-11): o técnico pede, quem aprova aprova o lote inteiro, o pátio separa e a via fecha o lote, ou recusa.
/// Rotas em /api/lotes-troca; cada passo tem o seu cargo e avisa os celulares ao vivo (decisão: Lote de troca).
/// </summary>
[ApiController]
[Route("api/lotes-troca")]
public class LoteTrocaHandler(LoteTrocaService lotes, AvisoAoVivo aoVivo) : ControllerBase
{
    /// <summary>Os lotes (qualquer cargo logado), com filtro opcional por status.</summary>
    [HttpGet]
    public Task<IReadOnlyList<LoteTrocaDto>> Listar([FromQuery] StatusLoteTroca? status) => lotes.Listar(status);

    /// <summary>Ao pedir o lote: as trocas avulsas já aprovadas na faixa, que entram nele como obrigatórias (Victor, 2026-11-12).</summary>
    [HttpGet("obrigatorias")]
    public Task<IReadOnlyList<ObrigatoriaDto>> Obrigatorias([FromQuery] int idTrecho, [FromQuery] decimal kmInicio, [FromQuery] decimal kmFim, [FromQuery] int idTamanho)
        => lotes.Obrigatorias(idTrecho, kmInicio, kmFim, idTamanho);

    /// <summary>Um lote pelo id (→ LOTE_TROCA); 404 se não existe.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoteTrocaDto>> Buscar(int id) => await lotes.BuscarPorId(id) is { } lote ? lote : NotFound();

    /// <summary>Só o Técnico: pede o lote (trecho, faixa de km, quantidade e tamanho).</summary>
    [HttpPost]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<LoteTrocaDto>> Pedir(NovoLoteTrocaDto dto)
    {
        var lote = await lotes.Pedir(dto, TokenHelper.IdUsuario(User));
        await aoVivo.Avisar(AvisoAoVivo.LotePedido, lote.Id); // quem aprova vê o pedido chegar
        return StatusCode(StatusCodes.Status201Created, lote);
    }

    /// <summary>Aprovar (o lote inteiro, podendo baixar a quantidade) ou reprovar: os cargos que aprovam vêm de PARAMETRO troca_quem_aprova.</summary>
    [HttpPost("{id:int}/decisao")]
    public async Task<ActionResult<LoteTrocaDto>> Decidir(int id, DecisaoLoteTrocaDto dto)
    {
        // Sem [Authorize] de cargo aqui: o service confere o cargo do token com o parâmetro.
        var cargo = Enum.Parse<Cargo>(User.FindFirstValue(TokenHelper.ClaimCargo)!);
        return await Avisado(await lotes.Decidir(id, dto, cargo, TokenHelper.IdUsuario(User)));
    }

    /// <summary>As peças que servem no lote; a primeira é a sugerida.</summary>
    [HttpGet("{id:int}/compativeis")]
    [Authorize(Roles = $"{nameof(Cargo.OperadorPatio)},{nameof(Cargo.Supervisor)}")]
    public async Task<ActionResult<IReadOnlyList<CompativelDto>>> Compativeis(int id)
        => await lotes.ListarCompativeis(id) is { } lista ? Ok(lista) : NotFound();

    /// <summary>Só o Operador de pátio: separa as peças novas do lote.</summary>
    [HttpPost("{id:int}/separacao")]
    [Authorize(Roles = nameof(Cargo.OperadorPatio))]
    public async Task<ActionResult<LoteTrocaDto>> Separar(int id, SepararTrocaDto dto) => await Avisado(await lotes.Separar(id, dto, TokenHelper.IdUsuario(User)));

    /// <summary>A foto de um dormente do lote (ou a da recusa): devolve o caminho, que vai no fotoArquivo do par ou da recusa.</summary>
    [HttpPost("{id:int}/foto")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FotoPecaService.TamanhoMaximo + 100_000)]
    public async Task<ActionResult<object>> EnviarFoto(int id, IFormFile foto, [FromServices] FotoCampoService fotos)
    {
        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria);
        var arquivo = await fotos.DoLote(id, memoria.ToArray(), Path.GetExtension(foto.FileName));
        return arquivo is null ? NotFound() : StatusCode(StatusCodes.Status201Created, new { arquivo });
    }

    /// <summary>Só o Operador da via: fecha o lote com os pares lidos (a peça que sai e a que entra) e as fotos.</summary>
    [HttpPost("{id:int}/fechamento")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    public async Task<ActionResult<LoteTrocaDto>> Fechar(int id, FecharLoteTrocaDto dto) => await Avisado(await lotes.Fechar(id, dto, TokenHelper.IdUsuario(User)));

    /// <summary>Só o Operador da via: recusa o lote no local, com motivo e foto.</summary>
    [HttpPost("{id:int}/recusa")]
    [Authorize(Roles = nameof(Cargo.OperadorVia))]
    public async Task<ActionResult<LoteTrocaDto>> Recusar(int id, RecusarTrocaDto dto) => await Avisado(await lotes.Recusar(id, dto, TokenHelper.IdUsuario(User)));

    /// <summary>O técnico mantém (com resposta) ou aceita a recusa do lote.</summary>
    [HttpPost("{id:int}/recusa/decisao")]
    [Authorize(Roles = nameof(Cargo.Tecnico))]
    public async Task<ActionResult<LoteTrocaDto>> DecidirRecusa(int id, DecidirRecusaDto dto)
        => await Avisado(await lotes.DecidirRecusa(id, dto, TokenHelper.IdUsuario(User)));

    /// <summary>Avisa ao vivo que o lote mudou e devolve o lote (404 se não existe).</summary>
    private async Task<ActionResult<LoteTrocaDto>> Avisado(LoteTrocaDto? lote)
    {
        await aoVivo.Lote(lote);
        return lote is null ? NotFound() : lote;
    }
}
