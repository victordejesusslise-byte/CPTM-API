using ViaPermanente.Dominio.Pecas.Dtos;

namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Uma peça que serve na troca, com a distância do armazém até a troca e a idade no estoque.</summary>
/// <param name="Peca">a peça do estoque que serve na troca.</param>
/// <param name="DistanciaKm">a distância do armazém até o km da troca, em km; nula sem km.</param>
/// <param name="IdadeDias">há quantos dias a peça está no armazém.</param>
/// <param name="Sugerida">verdadeiro na peça que o sistema sugere (a primeira da lista).</param>
public record CompativelDto(
    
    PecaDto Peca, decimal? 
    
    DistanciaKm, int IdadeDias, 
    
    bool Sugerida);