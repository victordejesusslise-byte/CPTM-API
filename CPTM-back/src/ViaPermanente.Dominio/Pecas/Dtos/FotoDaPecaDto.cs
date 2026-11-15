namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>Uma foto da peça: a da entrada (verificação de qualidade, "como estava") ou o quadro da câmera do carro na via
/// ("como ficou"). AnaliseIa fica vazia até o modelo de imagem existir.</summary>
/// <param name="Momento">"entrada" ou "via".</param>
/// <param name="Arquivo">O caminho do arquivo na pasta das fotos.</param>
/// <param name="Data">A data do registro da foto (a verificação ou a passagem do carro).</param>
/// <param name="Descricao">A frase que a tela mostra embaixo da foto.</param>
/// <param name="NoServidor">Verdadeiro se o arquivo está mesmo no servidor.</param>
/// <param name="AnaliseIa">O que a IA disse da foto. Vazio por enquanto.</param>
public record FotoDaPecaDto(string Momento, string Arquivo, DateTime Data, string? Descricao, bool NoServidor, string? AnaliseIa);

/// <summary>As fotos de uma peça, separadas em entrada e via.</summary>
/// <param name="IdDormente">Id da peça (→ DORMENTE).</param>
/// <param name="Entrada">As fotos da verificação de qualidade.</param>
/// <param name="Via">Os quadros da câmera do carro.</param>
public record FotosDaPecaDto(int IdDormente, IReadOnlyList<FotoDaPecaDto> Entrada, IReadOnlyList<FotoDaPecaDto> Via);
