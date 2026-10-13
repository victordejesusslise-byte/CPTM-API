using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Api.Pecas;

/// <summary>O estoque: peças aprovadas, com tag ativa, fora da via. Novos na frente.</summary>
[ApiController]
[Route("api/estoque")]
public class EstoqueHandler(PecaService pecas) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PecaDto>> Listar([FromQuery] int? idArmazem, [FromQuery] int? idTamanho, [FromQuery] byte? classe)
        => pecas.Estoque(idArmazem, idTamanho, classe);
}