using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>A verificação de qualidade da peça, depois de lida. Reprovada exige motivo e foto.</summary>
public class VerificarPecaDto
{
    /// <summary>A leitura da tag da peça, com uma tag só no alcance.</summary>
    [Required] public LeituraDto Leitura { get; init; } = new();
    /// <summary>Ataque biológico na madeira: nenhum, suspeito ou presente.</summary>
    public AtaqueBiologicoVerificacao AtaqueBiologico { get; init; }
    /// <summary>Rachadura: nenhuma, leve ou grave.</summary>
    public RachaduraVerificacao Rachadura { get; init; }
    /// <summary>A peça tem a placa antirrachante?</summary>
    public bool PlacaAntirrachante { get; init; }
    /// <summary>A placa bate com a nota?</summary>
    public bool PlacaConfere { get; init; }
    /// <summary>Aprovada ou reprovada.</summary>
    public ResultadoVerificacao Resultado { get; init; }
    /// <summary>Por que foi reprovada. Obrigatório na reprovada.</summary>
    public string? Motivo { get; init; }
    /// <summary>O caminho da foto. Obrigatória na reprovada.</summary>
    public string? FotoArquivo { get; init; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; init; }
    /// <summary>A hora do celular; sem ela, vale a do servidor.</summary>
    public DateTime? RegistradoEm { get; init; }
}