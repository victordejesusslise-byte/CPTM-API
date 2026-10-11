namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>MODO_LEITURA: Armazém, Normal, Via e Personalizado; só o supervisor muda.</summary>
public class ModoLeitura
{
    public int IdModo { get; set; }
    public string Nome { get; set; } = "";
    public decimal PotenciaDbm { get; set; }
    public decimal FolgaRssiDb { get; set; }
    public Antena Antena { get; set; }
    public bool ExigeAlvoUnico { get; set; }
    public int? AlteradoPor { get; set; }
    public DateTime? AlteradoEm { get; set; }
}