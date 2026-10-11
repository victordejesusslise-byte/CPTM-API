namespace ViaPermanente.Dominio.Dtos.Trocas;

/// <summary>A troca com os quatro passos (quem e quando) e os dias de peça condenada na via desde o pedido.</summary>
public record TrocaDto(int Id, string Status, string Origem,
    int IdDormenteSaiu, string? CodigoSaiu, string? TagSaiu, int? IdDormenteEntrou, string? CodigoEntrou, string? TagEntrou,
    int? IdInspecao, int? IdTrecho, decimal? Km, string? Motivo, string? Observacao,
    int IdUsuarioPediu, DateTime DataPedido, int? IdUsuarioAprovou, DateTime? DataAprovacao, string? MotivoReprovacao,
    int? IdUsuarioSeparou, DateTime? DataSeparacao, int? IdUsuarioFez, DateTime? Data, string? Equipe, string? FotoArquivo,
    string? Cancelamento, int DiasDesdeOPedido, RecusaDto? UltimaRecusa);