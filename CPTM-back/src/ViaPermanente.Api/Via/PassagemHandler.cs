using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Api.Campo;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;

namespace ViaPermanente.Api.Via;

/// <summary>Etapa 08: a passagem noturna. O carro manda com a chave dele (X-Chave-Carro); o supervisor também pode enviar.</summary>
[ApiController]
[Route("api/passagens")]
[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ChaveCarroHandler.Esquema}", Roles = $"{nameof(Cargo.Supervisor)},{ChaveCarroHandler.Papel}")]
public class PassagemHandler(VarreduraService varredura, AvisoAoVivo aoVivo) : ControllerBase
{
    /// <summary>Grava a passagem e devolve o resumo: leituras, sem resposta, fora de posição e os alertas abertos.</summary>
    [HttpPost]
    public async Task<ActionResult<PassagemDto>> Registrar(NovaPassagemDto nova)
    {
        var passagem = await varredura.Registrar(nova);
        if (passagem.AlertasSensor + passagem.AlertasImagem > 0)
            await aoVivo.Avisar(AvisoAoVivo.AlertaNovo, null);
        return StatusCode(StatusCodes.Status201Created, passagem);
    }
}