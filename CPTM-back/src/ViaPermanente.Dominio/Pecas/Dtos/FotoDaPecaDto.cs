namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>
/// Uma foto da peça: a da entrada (verificação de qualidade, "como estava") ou o quadro da câmera do carro na via
/// ("como ficou"). AnaliseIa fica vazia até o modelo de imagem existir.
/// </summary>
public record FotoDaPecaDto(string Momento, string Arquivo, DateTime Data, string? Descricao, bool NoServidor, string? AnaliseIa);

public record FotosDaPecaDto(int IdDormente, IReadOnlyList<FotoDaPecaDto> Entrada, IReadOnlyList<FotoDaPecaDto> Via);
