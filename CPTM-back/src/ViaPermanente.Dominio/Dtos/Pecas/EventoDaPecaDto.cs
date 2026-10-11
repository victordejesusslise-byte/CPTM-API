namespace ViaPermanente.Dominio.Dtos.Pecas;

/// <summary>Um acontecimento da peça, da chegada até hoje: quando, o quê, quem e de qual registro saiu.</summary>
public record EventoDaPecaDto(DateTime Data, string Tipo, string Descricao, int? IdUsuario, string Origem);