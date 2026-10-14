using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>CLASSIFICACAO_MADEIRA: só insere; a vigente é a de maior id; a revisão aponta para a anterior.</summary>
public class ClassificacaoMadeira : IRegistroDeCampo
{
    public int IdClassificacao { get; set; }
    public int IdDormente { get; set; }
    public int IdEspecie { get; set; }
    public int IdUsuario { get; set; }
    public int? Anterior { get; set; }
    public DateTime Data { get; set; }
    public MetodoClassificacao Metodo { get; set; }
    public MetodoCaptura? MetodoCaptura { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Só com a pistola NIR: a umidade que ela mediu (%).</summary>
    public decimal? UmidadePct { get; set; }
    /// <summary>Só com a pistola NIR: a confiança da leitura da espécie (%).</summary>
    public decimal? ConfiancaPct { get; set; }
    public Guid? IdCliente { get; set; }
    public DateTime RegistradoEm { get; set; }
    public DateTime? RecebidoEm { get; set; }
}