using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Uma peça que serve na troca, com a distância do armazém até a troca e a idade no estoque.</summary>
public record CompativelDto(PecaDto Peca, decimal? DistanciaKm, int IdadeDias, bool Sugerida);