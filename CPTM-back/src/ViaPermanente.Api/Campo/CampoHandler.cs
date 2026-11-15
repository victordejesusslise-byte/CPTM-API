using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;

namespace ViaPermanente.Api.Campo;

/// <summary>
/// O trabalho da noite que o celular guarda para abrir sem rede (app Android, Etapa 4). Qualquer cargo logado, como a lista de trocas.
/// Rotas em /api/campo (decisão: Sem internet, o celular guarda e envia depois).
/// </summary>
[ApiController]
[Route("api/campo")]
public class CampoHandler(PlanoCampoService plano) : ControllerBase
{
    /// <summary>O plano da noite: as trocas e os lotes que a via tem para fazer.</summary>
    [HttpGet("plano")]
    public Task<PlanoCampoDto> Plano() => plano.Montar(DateTime.Now);
}
