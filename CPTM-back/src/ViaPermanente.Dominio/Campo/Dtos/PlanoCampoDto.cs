using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// O trabalho da noite que o celular guarda: as trocas do plano, os alertas abertos e as peças deles (a que sai em cada troca e a de
/// cada alerta, para a inspeção abrir sem rede), como estavam em GeradoEm.
/// </summary>
public record PlanoCampoDto(DateOnly Noite, DateTime GeradoEm, IReadOnlyList<TrocaDto> Trocas, IReadOnlyList<AlertaDto> Alertas, IReadOnlyList<PecaDto> Pecas);
