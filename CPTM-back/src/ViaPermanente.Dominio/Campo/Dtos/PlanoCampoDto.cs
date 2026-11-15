using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>
/// O trabalho da noite que o celular guarda: as trocas do plano, os alertas abertos e as peças deles (a que sai em cada troca e a de
/// cada alerta, para a inspeção abrir sem rede), como estavam em GeradoEm. E os lotes de troca da noite, com a faixa e as separadas.
/// </summary>
/// <param name="Noite">a noite do plano que está valendo (antes das 06:00 ainda é a de ontem).</param>
/// <param name="GeradoEm">a hora em que o plano foi montado.</param>
/// <param name="Trocas">as trocas da noite que ainda não foram feitas.</param>
/// <param name="Alertas">os alertas abertos.</param>
/// <param name="Pecas">as peças das trocas e dos alertas, para abrir sem rede.</param>
/// <param name="Lotes">os lotes de troca da noite.</param>
public record PlanoCampoDto(DateOnly Noite, DateTime GeradoEm, IReadOnlyList<TrocaDto> Trocas, IReadOnlyList<AlertaDto> Alertas, IReadOnlyList<PecaDto> Pecas,
    IReadOnlyList<LoteTrocaDto>? Lotes = null);
