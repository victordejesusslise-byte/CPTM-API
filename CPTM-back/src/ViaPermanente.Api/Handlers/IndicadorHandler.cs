using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Dtos.Indicadores;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>Os 18 indicadores para o painel. Sem datas, valem os últimos 30 dias.</summary>
[ApiController]
[Route("api/indicadores")]
public class IndicadorHandler(IndicadorService indicadores) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<IndicadorDto>> Calcular([FromQuery] DateTime? de, [FromQuery] DateTime? ate) => indicadores.Calcular(de, ate, DateTime.Now);
}