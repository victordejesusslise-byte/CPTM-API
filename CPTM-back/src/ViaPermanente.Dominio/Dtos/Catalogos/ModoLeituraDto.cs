namespace ViaPermanente.Dominio.Dtos.Catalogos;

public record ModoLeituraDto(int Id, string Nome, decimal PotenciaDbm, decimal FolgaRssiDb, string Antena, bool ExigeAlvoUnico);