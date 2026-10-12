namespace ViaPermanente.Dominio.Dtos.Classificacoes;

/// <summary>Uma classificação da espécie da peça. A mais nova vale; as de antes ficam (Anterior aponta para a anterior).</summary>
public record ClassificacaoDto(int Id, int IdDormente, int IdEspecie, string Especie, byte Classe, string Metodo, DateTime Data, int IdUsuario, int? Anterior, string? Observacao);
