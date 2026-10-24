namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O trabalho da noite que o celular guarda: as trocas do plano e os alertas abertos, como estavam em GeradoEm.</summary>
public record PlanoCampoDto(DateOnly Noite, DateTime GeradoEm, IReadOnlyList<TrocaDto> Trocas, IReadOnlyList<AlertaDto> Alertas);
