using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Api.Pecas;

/// <summary>
/// O estoque: peças aprovadas, com tag ativa, fora da via. Novos na frente.
/// Rota: GET /api/estoque; quem chama: qualquer cargo logado.
/// </summary>
[ApiController]
[Route("api/estoque")]
public class EstoqueHandler(PecaService pecas) : ControllerBase
{
    /// <summary>As peças em estoque, com filtros opcionais por armazém, tamanho e classe (1 ou 2).</summary>
    [HttpGet]
    public Task<IReadOnlyList<PecaDto>> Listar([FromQuery] int? idArmazem, [FromQuery] int? idTamanho, [FromQuery] byte? classe)
        => pecas.Estoque(idArmazem, idTamanho, classe);
}