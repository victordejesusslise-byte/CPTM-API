using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Api.Via;

/// <summary>
/// A partição semanal das leituras do carro: ver as semanas e rodar a manutenção agora (ela também roda sozinha todo dia).
/// Rotas em /api/manutencao/leituras-via; quem chama: só o Supervisor (decisão: 90 dias de passagem).
/// </summary>
[ApiController]
[Route("api/manutencao/leituras-via")]
[Authorize(Roles = nameof(Cargo.Supervisor))]
public class ManutencaoHandler(ManutencaoLeituraViaService manutencao) : ControllerBase
{
    /// <summary>GET: as semanas guardadas, com quantas leituras cada uma tem.</summary>
    [HttpGet]
    public Task<IReadOnlyList<ParticaoLeituraVia>> Particoes() => manutencao.Particoes();

    /// <summary>POST: roda a manutenção agora (abre as próximas semanas e apaga as mais velhas que 90 dias).</summary>
    [HttpPost]
    public Task<ManutencaoLeituraViaDto> Executar() => manutencao.Executar(DateTime.Now);
}