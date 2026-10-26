
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>DORMENTE: a peça. Espécie, tamanho e placa vêm do feixe; a tag vigente vem de RFID.</summary>
public class Dormente
{
    public int IdDormente { get; set; }
    public int IdLote { get; set; }
    public int? IdFeixe { get; set; }
    public string? TagFornecedor { get; set; }
    public MetodoCaptura? TagFornecedorLeitura { get; set; }
    public decimal? KmProjeto { get; set; }
    public int? IdTrecho { get; set; }
    public int? IdPilha { get; set; }
    public DateTime? EntradaArmazemEm { get; set; }
    public DateTime? RetiradaEm { get; set; }
    /// <summary>
    /// Quando a peça ou um registro dela (inspeção, tag, movimentação, troca, leitura) mudou pela última vez. O app usa para baixar só
    /// o que mudou na cópia das linhas (Entrega 2). Gravada sozinha pelo ViaPermanenteContext, não à mão.
    /// </summary>
    public DateTime? DataAlteracao { get; set; }
}