
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>MODO_LEITURA: Armazém, Normal, Via e Personalizado; só o supervisor muda. O operador escolhe o modo,
/// e a potência fica no menu técnico (decisão: Modos de leitura no lugar de potência).</summary>
public class ModoLeitura
{
    /// <summary>Id do modo: 1 Armazém, 2 Normal, 3 Via, 4 Personalizado.</summary>
    public int IdModo { get; set; }
    /// <summary>O nome que o operador vê.</summary>
    public string Nome { get; set; } = "";
    /// <summary>A potência do leitor neste modo, em dBm.</summary>
    public decimal PotenciaDbm { get; set; }
    /// <summary>Quanto o sinal da 1ª tag precisa passar o da 2ª para valer a 1ª, em dB.</summary>
    public decimal FolgaRssiDb { get; set; }
    /// <summary>A antena usada: curta, longa ou única.</summary>
    public Antena Antena { get; set; }
    /// <summary>Verdadeiro: neste modo, só vale a leitura com uma tag só no alcance.</summary>
    public bool ExigeAlvoUnico { get; set; }
    /// <summary>Id de quem mudou o modo por último (→ USUARIO).</summary>
    public int? AlteradoPor { get; set; }
    /// <summary>Quando o modo foi mudado por último.</summary>
    public DateTime? AlteradoEm { get; set; }
}