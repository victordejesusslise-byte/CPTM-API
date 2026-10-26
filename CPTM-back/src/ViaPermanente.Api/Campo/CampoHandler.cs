using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;

namespace ViaPermanente.Api.Campo;

/// <summary>O trabalho da noite que o celular guarda para abrir sem rede (app Android, Etapa 4). Qualquer cargo logado, como a lista de trocas.</summary>
[ApiController]
[Route("api/campo")]
public class CampoHandler(PlanoCampoService plano, LinhaCampoService linha) : ControllerBase
{
    [HttpGet("plano")]
    public Task<PlanoCampoDto> Plano() => plano.Montar(DateTime.Now);

    /// <summary>
    /// A cópia das linhas para o celular (Entrega 2), em pedaços: depoisDe = o "proximo" da página anterior; desde = o "geradoEm" da última
    /// cópia (sem desde, vem tudo). Vai compactada (gzip/brotli).
    /// </summary>
    [HttpGet("linha")]
    public Task<PaginaDaLinhaDto> Linha([FromQuery] DateTime? desde, [FromQuery] int depoisDe = 0, [FromQuery] int limite = 2000)
        => linha.Pagina(desde, depoisDe, limite, DateTime.Now);
}
