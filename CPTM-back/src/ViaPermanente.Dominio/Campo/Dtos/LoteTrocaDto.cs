namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Uma peça do lote: separada para ele, devolvida dele ou na via dentro da faixa.</summary>
public record PecaDoLoteDto(int Id, string Codigo, string? Tag, decimal? KmProjeto, int IdTamanho, string? Pilha);

/// <summary>
/// O lote de troca com os passos (quem e quando), as peças separadas e devolvidas e as trocas feitas nele. Aberto (BuscarPorId e o plano
/// da noite), traz também as peças na via dentro da faixa (para o celular conferir sem rede) e as que têm pedido avulso aberto.
/// </summary>
public record LoteTrocaDto(int Id, string Codigo, string Status, int IdTrecho, string Trecho, byte ClasseExigida,
    decimal KmInicio, decimal KmFim, int IdTamanho, decimal ComprimentoM, int QuantidadePedida, int? QuantidadeAprovada,
    string Motivo, string? Observacao, int IdUsuarioPediu, DateTime DataPedido, int? IdUsuarioAprovou, DateTime? DataAprovacao,
    string? MotivoReprovacao, string? MotivoMenos, DateOnly? NoiteDoPlano, bool Urgente, int? IdUsuarioFez, DateTime? Data,
    string? Equipe, string? Cancelamento,
    IReadOnlyList<PecaDoLoteDto> Separadas, IReadOnlyList<PecaDoLoteDto> Devolvidas, IReadOnlyList<TrocaDto> Trocas,
    IReadOnlyList<PecaDoLoteDto> PecasDaFaixa, IReadOnlyList<PecaDoLoteDto> AvulsasAbertas, RecusaDto? UltimaRecusa);
