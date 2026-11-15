namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>Um acontecimento da peça, da chegada até hoje: quando, o quê, quem e de qual registro saiu.</summary>
/// <param name="Data">Quando aconteceu.</param>
/// <param name="Tipo">O tipo, ex.: "Chegada", "Gravação da tag", "Troca feita".</param>
/// <param name="Descricao">A frase que a tela mostra.</param>
/// <param name="IdUsuario">Id de quem fez (→ USUARIO). Nulo no alerta automático.</param>
/// <param name="Origem">A tabela e o id de onde saiu, ex.: "TROCA 12".</param>
public record EventoDaPecaDto(DateTime Data, string Tipo, string Descricao, int? IdUsuario, string Origem);