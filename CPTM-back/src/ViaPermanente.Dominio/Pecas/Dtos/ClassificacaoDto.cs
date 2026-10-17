namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>Uma classificação da espécie da peça. A mais nova vale; as de antes ficam (Anterior aponta para a anterior).</summary>
public record ClassificacaoDto(int Id, int IdDormente, int IdEspecie, string Especie, string Metodo, DateTime Data, int IdUsuario, int? Anterior, string? Observacao,
    decimal? UmidadePct = null, decimal? ConfiancaPct = null);
