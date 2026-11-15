using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>CLASSIFICACAO_MADEIRA: a espécie da peça. A primeira vem da placa do feixe; o especialista pode revisar depois.
/// Nada é apagado: vale a mais nova (maior id).</summary>
public class ClassificacaoMadeira : IRegistroDeCampo
{
    /// <summary>Id da classificação.</summary>
    public int IdClassificacao { get; set; }
    /// <summary>Id da peça (→ DORMENTE).</summary>
    public int IdDormente { get; set; }
    /// <summary>Id da espécie (→ ESPECIE).</summary>
    public int IdEspecie { get; set; }
    /// <summary>Id de quem classificou (→ USUARIO).</summary>
    public int IdUsuario { get; set; }
    /// <summary>Id da classificação anterior, que esta revisa (→ CLASSIFICACAO_MADEIRA). Nulo na primeira.</summary>
    public int? Anterior { get; set; }
    /// <summary>Quando foi classificada.</summary>
    public DateTime Data { get; set; }
    /// <summary>Como a espécie foi definida (placa, comparação, visual, catálogo, laboratório ou NIR).</summary>
    public MetodoClassificacao Metodo { get; set; }
    /// <summary>Como a peça foi identificada (módulo, câmera, NFC ou digitado).</summary>
    public MetodoCaptura? MetodoCaptura { get; set; }
    /// <summary>Texto livre de quem classificou.</summary>
    public string? Observacao { get; set; }
    /// <summary>Só com a pistola NIR: a umidade que ela mediu (%).</summary>
    public decimal? UmidadePct { get; set; }
    /// <summary>Só com a pistola NIR: a confiança da leitura da espécie (%).</summary>
    public decimal? ConfiancaPct { get; set; }
    /// <summary>Gerado no celular: o mesmo envio repetido entra uma vez.</summary>
    public Guid? IdCliente { get; set; }
    /// <summary>A hora do celular; sem ela, a do servidor.</summary>
    public DateTime RegistradoEm { get; set; }
    /// <summary>A hora em que chegou ao servidor.</summary>
    public DateTime? RecebidoEm { get; set; }
}