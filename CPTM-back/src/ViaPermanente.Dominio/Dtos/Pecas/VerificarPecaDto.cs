using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Models.Chegada;

namespace ViaPermanente.Dominio.Dtos.Pecas;

/// <summary>A verificação de qualidade da peça, depois de lida. Reprovada exige motivo e foto.</summary>
public class VerificarPecaDto
{
    [Required] public LeituraDto Leitura { get; init; } = new();
    public AtaqueBiologicoVerificacao AtaqueBiologico { get; init; }
    public RachaduraVerificacao Rachadura { get; init; }
    public bool PlacaAntirrachante { get; init; }
    public bool PlacaConfere { get; init; }
    public ResultadoVerificacao Resultado { get; init; }
    public string? Motivo { get; init; }
    public string? FotoArquivo { get; init; }
    public Guid? IdCliente { get; init; }
    public DateTime? RegistradoEm { get; init; }
}