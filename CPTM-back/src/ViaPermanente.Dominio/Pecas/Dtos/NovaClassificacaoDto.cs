using System.ComponentModel.DataAnnotations;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>O especialista confirma ou corrige a espécie da placa, com a peça lida. Nunca apaga a de antes.</summary>
public class NovaClassificacaoDto
{
    /// <summary>Id da peça (→ DORMENTE).</summary>
    [Range(1, int.MaxValue)] public int IdDormente { get; init; }
    /// <summary>Id da espécie que o especialista definiu (→ ESPECIE).</summary>
    [Range(1, int.MaxValue)] public int IdEspecie { get; init; }
    /// <summary>Como ele conferiu: comparação, visual, catálogo, laboratório ou NIR (a placa não vale aqui).</summary>
    public MetodoClassificacao Metodo { get; init; } = MetodoClassificacao.Comparacao;
    /// <summary>A leitura da tag da peça, antes de classificar.</summary>
    [Required] public LeituraDto Leitura { get; init; } = new();
    /// <summary>Texto livre do especialista.</summary>
    public string? Observacao { get; init; }
    /// <summary>Com a pistola NIR (obrigatória): a umidade medida, de 0 a 100%.</summary>
    [Range(0, 100)] public decimal? UmidadePct { get; init; }
    /// <summary>Com a pistola NIR (opcional): a confiança da leitura da espécie, de 0 a 100%.</summary>
    [Range(0, 100)] public decimal? ConfiancaPct { get; init; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; init; }
}
