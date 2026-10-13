namespace ViaPermanente.Dominio.Catalogos.Models;

/// <summary>ESPECIE: as espécies vegetais; a classe vem da espécie.</summary>
public class Especie
{
    public int IdEspecie { get; set; }
    public string NomeCientifico { get; set; } = "";
    public string Familia { get; set; } = "";
    public string? NomePopular { get; set; }
    public byte Classe { get; set; }
    public bool Ativo { get; set; } = true;
}