using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Dtos.Catalogos;
using ViaPermanente.Dominio.Dtos.Integracoes;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>Integração só por planilha: importar as notas da planilha que a API deles gera e ver o log.</summary>
[ApiController]
[Route("api/integracoes")]
[Authorize(Roles = $"{nameof(Cargo.Supervisor)},{nameof(Cargo.OperadorPatio)}")]
public class IntegracaoHandler(ImportacaoNotaService importacao, ExportacaoService exportacao) : ControllerBase
{
    /// <summary>O estoque peça a peça, com o código de material, para o ERP.</summary>
    [HttpGet("erp/estoque")]
    public async Task<FileContentResult> Erp()
    {
        var arquivo = await exportacao.Erp();
        return File(arquivo.Conteudo, ArquivoDto.TipoXlsx, arquivo.Nome);
    }

    [HttpGet("codigos-material")]
    public Task<IReadOnlyList<CodigoMaterialDto>> CodigosMaterial() => exportacao.CodigosMaterial();

    /// <summary>O código de material do ERP para um tamanho e uma classe. Só o supervisor.</summary>
    [HttpPost("codigos-material")]
    [Authorize(Roles = nameof(Cargo.Supervisor))]
    public async Task<ActionResult<CodigoMaterialDto>> NovoCodigoMaterial(NovoCodigoMaterialDto dto)
        => StatusCode(StatusCodes.Status201Created, await exportacao.NovoCodigoMaterial(dto));

    /// <summary>Importa a planilha (.xlsx) de notas. Volta as notas que entraram, já conferidas, e os erros.</summary>
    [HttpPost("notas")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ImportacaoNotasDto>> ImportarNotas(IFormFile arquivo)
    {
        await using var conteudo = arquivo.OpenReadStream();
        return await importacao.Importar(conteudo, Path.GetFileName(arquivo.FileName), TokenHelper.IdUsuario(User));
    }

    /// <summary>A planilha em branco, com as colunas do modelo provisório.</summary>
    [HttpGet("notas/modelo")]
    public FileContentResult Modelo()
        => File(importacao.Modelo(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "modelo-notas.xlsx");

    [HttpGet("log")]
    public Task<IReadOnlyList<IntegracaoLogDto>> Log() => importacao.Log();
}