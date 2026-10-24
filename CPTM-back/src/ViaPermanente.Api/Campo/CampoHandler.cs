using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;

namespace ViaPermanente.Api.Campo;

/// <summary>O trabalho da noite que o celular guarda para abrir sem rede (app Android, Etapa 4). Qualquer cargo logado, como a lista de trocas.</summary>
[ApiController]
[Route("api/campo")]
public class CampoHandler(PlanoCampoService plano) : ControllerBase
{
    [HttpGet("plano")]
    public Task<PlanoCampoDto> Plano() => plano.Montar(DateTime.Now);
}
