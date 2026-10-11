namespace ViaPermanente.Dominio.Models.Catalogos;

/// <summary>PARAMETRO: chave e valor que o supervisor pode mudar sem mexer no código.</summary>
public class Parametro
{
    public string Chave { get; set; } = "";
    public string Valor { get; set; } = "";
    public int? AlteradoPor { get; set; }
    public DateTime? AlteradoEm { get; set; }
}