using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Dtos.Varredura;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Api.Handlers;

/// <summary>Etapa 08: a passagem noturna. O carro manda com a chave dele (X-Chave-Carro); o supervisor também pode enviar.</summary>
[ApiController]
[Route("api/passagens")]
[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ChaveCarroHandler.Esquema}", Roles = $"{nameof(Cargo.Supervisor)},{ChaveCarroHandler.Papel}")]
public class PassagemHandler(VarreduraService varredura) : ControllerBase
{
    /// <summary>Grava a passagem e devolve o resumo: leituras, sem resposta, fora de posição e os alertas abertos.</summary>
    [HttpPost]
    public async Task<ActionResult<PassagemDto>> Registrar(NovaPassagemDto nova)
        => StatusCode(StatusCodes.Status201Created, await varredura.Registrar(nova));
}