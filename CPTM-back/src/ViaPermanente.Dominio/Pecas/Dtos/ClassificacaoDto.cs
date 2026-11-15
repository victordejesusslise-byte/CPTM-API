namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>Uma classificação da espécie da peça. A mais nova vale; as de antes ficam (Anterior aponta para a anterior).</summary>
/// <param name="Id">Id da classificação (→ CLASSIFICACAO_MADEIRA).</param>
/// <param name="IdDormente">Id da peça (→ DORMENTE).</param>
/// <param name="IdEspecie">Id da espécie (→ ESPECIE).</param>
/// <param name="Especie">O nome popular da espécie, ou o científico quando não há popular.</param>
/// <param name="Metodo">Como a espécie foi definida (PlacaFornecedor, Comparacao, Visual, Catalogo, Laboratorio, Nir).</param>
/// <param name="Data">Quando foi classificada.</param>
/// <param name="IdUsuario">Id de quem classificou (→ USUARIO).</param>
/// <param name="Anterior">Id da classificação anterior. Nulo na primeira.</param>
/// <param name="Observacao">Texto livre de quem classificou.</param>
/// <param name="UmidadePct">Só com a pistola NIR: a umidade medida, em %.</param>
/// <param name="ConfiancaPct">Só com a pistola NIR: a confiança da leitura da espécie, em %.</param>
public record ClassificacaoDto(int Id, int IdDormente, int IdEspecie, string Especie, string Metodo, DateTime Data, int IdUsuario, int? Anterior, string? Observacao,
    decimal? UmidadePct = null, decimal? ConfiancaPct = null);
