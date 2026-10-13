using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;

namespace ViaPermanente.Api.Via;

/// <summary>A tela do trecho: as peças assentadas com o ID de posição e a nota, por faixa de km.</summary>
[ApiController]
[Route("api/via/trechos")]
[Authorize]
public class TrechoMapaHandler(TrechoMapaService mapa) : ControllerBase
{
    /// <summary>Os trechos, com quantas peças estão assentadas e quantas em Ruim ou Crítico.</summary>
    [HttpGet]
    public Task<IReadOnlyList<TrechoDoMapaDto>> Listar() => mapa.Trechos();

    /// <summary>O mapa do trecho em faixas de km (?faixaM=, padrão 100 m, de 50 a 5000).</summary>
    [HttpGet("{id:int}/mapa")]
    public async Task<ActionResult<MapaDoTrechoDto>> Mapa(int id, [FromQuery] int faixaM = TrechoMapaService.FaixaPadraoM)
    {
        var resultado = await mapa.Mapa(id, faixaM);
        return resultado is null ? NotFound() : resultado;
    }
}
