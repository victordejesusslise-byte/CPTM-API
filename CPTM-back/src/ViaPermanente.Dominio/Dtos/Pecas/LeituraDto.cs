using ViaPermanente.Dominio.Models.Patio;

namespace ViaPermanente.Dominio.Dtos.Pecas;

/// <summary>A leitura da peça que abre a operação (verificar, guardar). Com o módulo ou a pistola, uma tag só no alcance.</summary>
public class LeituraDto
{
    public string Tag { get; init; } = "";
    public MetodoCaptura Metodo { get; init; } = MetodoCaptura.ModuloUhf;
    public int TagsVistas { get; init; } = 1;
    public int? IdLeitor { get; init; }
    public int? IdModo { get; init; }
    public decimal? PotenciaDbm { get; init; }
    public decimal? RssiDbm { get; init; }
}