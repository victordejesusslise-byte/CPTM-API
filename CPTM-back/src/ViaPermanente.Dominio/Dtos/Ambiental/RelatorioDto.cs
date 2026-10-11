namespace ViaPermanente.Dominio.Dtos.Ambiental;

public record ItemRelatorioDto(int IdEspecie, string Especie, decimal VolumeM3Estoque, decimal VolumeM3Entrada, decimal VolumeM3Saida, int IdadeMediaDias);

/// <summary>O relatório ambiental do mês. Enviado, fica congelado.</summary>
public record RelatorioDto(int Id, DateOnly MesReferencia, string Status, DateTime GeradoEm, DateTime? EnviadoEm, decimal VolumeM3Estoque, IReadOnlyList<ItemRelatorioDto> Itens);