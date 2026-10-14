using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>O especialista confirma ou corrige a espécie da placa, com a peça lida. Nunca apaga a de antes.</summary>
public class NovaClassificacaoDto
{
    [Range(1, int.MaxValue)] public int IdDormente { get; init; }
    [Range(1, int.MaxValue)] public int IdEspecie { get; init; }
    public MetodoClassificacao Metodo { get; init; } = MetodoClassificacao.Comparacao;
    [Required] public LeituraDto Leitura { get; init; } = new();
    public string? Observacao { get; init; }
    /// <summary>Com a pistola NIR (obrigatória): a umidade medida, de 0 a 100%.</summary>
    [Range(0, 100)] public decimal? UmidadePct { get; init; }
    /// <summary>Com a pistola NIR (opcional): a confiança da leitura da espécie, de 0 a 100%.</summary>
    [Range(0, 100)] public decimal? ConfiancaPct { get; init; }
    public Guid? IdCliente { get; init; }
}
