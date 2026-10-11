namespace ViaPermanente.Dominio.Dtos.Inspecoes;

public record InspecaoDto(int Id, int IdDormente, int IdUsuario, int? IdAlerta, int? IdInspecaoOrigem,
    DateTime IniciadoEm, DateTime FinalizadoEm, int DuracaoS, int VersaoChecklist,
    string EstadoConservacao, string Rachadura, string AtaqueBiologico, decimal? UmidadePct, int? VidaUtilMeses, bool TipoMadeiraConfere,
    string Resultado, IReadOnlyList<string> MotivosDeReprovacao, string? Observacao, string FotoArquivo);