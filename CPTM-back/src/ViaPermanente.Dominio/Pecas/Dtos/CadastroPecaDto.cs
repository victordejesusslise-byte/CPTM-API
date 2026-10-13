namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>O resultado do cadastro: Ok (com a peça), Bloqueada, Falhou ou Divergente (sem peça), ou Repetido.</summary>
public record CadastroPecaDto(string Resultado, string Mensagem, PecaDto? Peca);