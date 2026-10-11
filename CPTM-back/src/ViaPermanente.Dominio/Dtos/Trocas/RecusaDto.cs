namespace ViaPermanente.Dominio.Dtos.Trocas;

public record RecusaDto(int Id, DateTime Data, int IdUsuario, string Motivo, string? Descricao, string FotoArquivo,
    string? Decisao, int? IdUsuarioDecisao, DateTime? DataDecisao, string? Resposta);