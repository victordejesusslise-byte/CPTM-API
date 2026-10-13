namespace ViaPermanente.Dominio.Ambiental.Models;

/// <summary>RELATORIO_AMBIENTAL: mensal; congelado ao enviar.</summary>
public class RelatorioAmbiental
{
    public int IdRelatorio { get; set; }
    public DateOnly MesReferencia { get; set; }
    public DateTime GeradoEm { get; set; }
    public int IdUsuario { get; set; }
    public StatusRelatorio Status { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public string? Arquivo { get; set; }
}