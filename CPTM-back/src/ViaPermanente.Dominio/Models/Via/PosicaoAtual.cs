namespace ViaPermanente.Dominio.Models.Via;

/// <summary>POSICAO_ATUAL: uma linha por dormente, sobrescrita toda manhã.</summary>
public class PosicaoAtual
{
    public int IdDormente { get; set; }
    public int IdPassagem { get; set; }
    public decimal? KmMedido { get; set; }
    public decimal? DesvioMm { get; set; }
    public FaixaPosicao Faixa { get; set; }
    public DateTime AtualizadoEm { get; set; }
}