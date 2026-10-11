namespace ViaPermanente.Dominio.Dtos.Chegadas;

/// <summary>A chegada com os feixes e a conferência (recalculada a cada consulta).</summary>
public record ChegadaDto(int Id, int IdPedido, int? IdNota, string? NumeroNota, int IdArmazem, string Armazem,
    string? DocumentoOrigemTipo, string? DocumentoOrigemNumero, DateTime DataRecebimento, int QuantidadeRecebida, string? Observacao,
    int IdLote, string Lote, IReadOnlyList<FeixeDto> Feixes, bool Confere, IReadOnlyList<string> Divergencias, IReadOnlyList<string> Avisos);