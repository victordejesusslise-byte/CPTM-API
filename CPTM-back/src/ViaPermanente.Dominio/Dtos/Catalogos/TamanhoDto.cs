namespace ViaPermanente.Dominio.Dtos.Catalogos;

/// <summary>O tamanho do dormente com o m³ já calculado.</summary>
public record TamanhoDto(int Id, decimal ComprimentoM, decimal LarguraM, decimal AlturaM, decimal VolumeM3, bool MedidaProvisoria);