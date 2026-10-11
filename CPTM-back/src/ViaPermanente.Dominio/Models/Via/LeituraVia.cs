namespace ViaPermanente.Dominio.Models.Via;

/// <summary>
/// LEITURA_VIA: a varredura noturna. Vive 90 dias; a chave é (id_leitura_via, data) para a partição semanal.
/// Nenhuma tabela aponta para ela com chave estrangeira, porque as partições velhas são descartadas.
/// </summary>
public class LeituraVia
{
    public long IdLeituraVia { get; set; }
    public int IdPassagem { get; set; }
    public int? IdDormente { get; set; }
    public bool Leitura { get; set; }
    public decimal? KmMedido { get; set; }
    public decimal? DesvioMm { get; set; }
    public DateTime Data { get; set; }
    public decimal? Umidade { get; set; }
    public decimal? Temperatura { get; set; }
    public bool RachaduraSuspeita { get; set; }
}