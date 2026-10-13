namespace ViaPermanente.Dominio.Armazens.Dtos;

/// <summary>Uma peça que falta ou que sobra numa pilha.</summary>
public record PecaConferidaDto(int IdDormente, string? Tag, string? PilhaDela, string? PilhaOndeRespondeu);

/// <summary>
/// A conferência do armazém, pilha a pilha. Só fecha com todas as pilhas lidas; os números ficam guardados ao concluir.
/// </summary>
public record ConferenciaDto(int Id, int IdArmazem, string Armazem, DateTime IniciadaEm, DateTime? ConcluidaEm,
    IReadOnlyList<string> PilhasLidas, IReadOnlyList<string> PilhasFaltando,
    int Esperadas, int Lidas, IReadOnlyList<PecaConferidaDto> Faltando, IReadOnlyList<PecaConferidaDto> AMais, IReadOnlyList<string> TagsSemCadastro);