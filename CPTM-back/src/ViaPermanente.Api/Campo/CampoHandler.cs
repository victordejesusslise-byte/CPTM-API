using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;

namespace ViaPermanente.Api.Campo;

/// <summary>O trabalho da noite que o celular guarda para abrir sem rede (app Android, Etapa 4). Qualquer cargo logado, como a lista de trocas.</summary>
[ApiController]
[Route("api/campo")]
public class CampoHandler(PlanoCampoService plano, LinhaCampoService linha, OperadorCampoService operador) : ControllerBase
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

    /// <summary>O pacote do operador da via para o celular guardar: trechos, alertas, trocas e passagens dos últimos ?dias= (7 ou 30).</summary>
    [HttpGet("operador")]
    public Task<PacoteDoOperadorDto> Operador([FromQuery] int dias = 7) => operador.Montar(dias, DateTime.Now);
}
