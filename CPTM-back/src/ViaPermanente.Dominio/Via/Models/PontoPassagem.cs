namespace ViaPermanente.Dominio.Via.Models;

/// <summary>PONTO_PASSAGEM: cada bip do caminho do veículo na passagem (a hora e o GPS), com o km que o sistema calculou. Só insere.</summary>
public class PontoPassagem
{
    /// <summary>Id do ponto.</summary>
    public long IdPonto { get; set; }
    /// <summary>Id da passagem (→ PASSAGEM).</summary>
    public int IdPassagem { get; set; }
    /// <summary>A hora do GPS.</summary>
    public DateTime Data { get; set; }
    /// <summary>A latitude, em graus.</summary>
    public decimal Latitude { get; set; }
    /// <summary>A longitude, em graus.</summary>
    public decimal Longitude { get; set; }
    /// <summary>O km calculado pelo caminho que o GPS andou entre a estação A e a B. Nulo se não deu para calcular.</summary>
    public decimal? KmCalculado { get; set; }
}
