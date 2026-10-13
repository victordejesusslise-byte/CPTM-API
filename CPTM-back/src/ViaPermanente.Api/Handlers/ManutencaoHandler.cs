using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Api.Handlers;

/// <summary>A partição semanal das leituras do carro: ver as semanas e rodar a manutenção agora (ela também roda sozinha todo dia).</summary>
[ApiController]
[Route("api/manutencao/leituras-via")]
[Authorize(Roles = nameof(Cargo.Supervisor))]
public class ManutencaoHandler(ManutencaoLeituraViaService manutencao) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ParticaoLeituraVia>> Particoes() => manutencao.Particoes();

    [HttpPost]
    public Task<ManutencaoLeituraViaDto> Executar() => manutencao.Executar(DateTime.Now);
}