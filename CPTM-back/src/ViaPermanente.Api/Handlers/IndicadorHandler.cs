using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Helpers;
using ViaPermanente.Dominio.Indicadores;
using ViaPermanente.Dominio.Indicadores.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Api.Handlers;

/// <summary>Os indicadores do painel, só os do cargo de quem pede (o supervisor vê os 18). Sem datas, valem os últimos 30 dias.</summary>
[ApiController]
[Route("api/indicadores")]
public class IndicadorHandler(IndicadorService indicadores) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<IndicadorDto>> Calcular([FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var cargo = Enum.Parse<Cargo>(User.FindFirstValue(TokenHelper.ClaimCargo)!);
        return (await indicadores.Calcular(de, ate, DateTime.Now)).Where(i => IndicadoresPorCargo.Ve(cargo, i.Chave)).ToList();
    }
}
