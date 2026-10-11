namespace ViaPermanente.Dominio.Models.Via;

/// <summary>EVENTO_POSICAO: só quando a peça muda de faixa; histórico permanente.</summary>
public class EventoPosicao
{
    public int IdEvento { get; set; }
    public int IdDormente { get; set; }
    public int IdPassagem { get; set; }
    public FaixaPosicao? FaixaAnterior { get; set; }
    public FaixaPosicao FaixaNova { get; set; }
    public decimal? DesvioMm { get; set; }
    public DateTime Data { get; set; }
}