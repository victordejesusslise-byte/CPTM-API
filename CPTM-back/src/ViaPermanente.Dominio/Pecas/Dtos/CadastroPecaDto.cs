namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>O resultado do cadastro: Ok (com a peça), Bloqueada, Falhou ou Divergente (sem peça), ou Repetido.</summary>
/// <param name="Resultado">Ok, Bloqueada, Falhou, Divergente ou Repetido.</param>
/// <param name="Mensagem">A frase que a tela mostra ao operador.</param>
/// <param name="Peca">A peça cadastrada. Nula quando a gravação não deu certo.</param>
public record CadastroPecaDto(string Resultado, string Mensagem, PecaDto? Peca);