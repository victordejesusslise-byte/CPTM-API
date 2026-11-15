
namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>DORMENTE: a peça de madeira. Espécie, tamanho e classe vêm do feixe (decisão: Dados da placa ficam no feixe).
/// A tag da peça fica na GRAVACAO_TAG, e a situação dela é calculada, não guardada.</summary>
public class Dormente
{
    /// <summary>Id da peça. É o número do fim do código (o 0028 de DM-E01-L01-0028).</summary>
    public int IdDormente { get; set; }
    /// <summary>
    /// Código impresso na peça: DM-E(empresa)-L(lote)-(id da peça), ex.: DM-E01-L01-0028. Único; gravado no cadastro e nunca muda
    /// (Victor, 2026-11-14).
    /// </summary>
    public string? Codigo { get; set; }
    /// <summary>Id do lote da peça (→ LOTE).</summary>
    public int IdLote { get; set; }
    /// <summary>Id do feixe de onde a peça veio (→ FEIXE).</summary>
    public int? IdFeixe { get; set; }
    /// <summary>A identificação do fornecedor (não é a nossa tag), ex.: TG-20261013193233-28.</summary>
    public string? TagFornecedor { get; set; }
    /// <summary>Como a identificação do fornecedor foi lida: câmera, NFC ou digitada.</summary>
    public MetodoCaptura? TagFornecedorLeitura { get; set; }
    /// <summary>O km da peça na via, em km com 3 casas. Nulo fora da via.</summary>
    public decimal? KmProjeto { get; set; }
    /// <summary>Id do trecho onde a peça foi assentada (→ TRECHO). Nulo fora da via.</summary>
    public int? IdTrecho { get; set; }
    /// <summary>Id da pilha onde a peça está agora (→ PILHA). Nulo fora do armazém.</summary>
    public int? IdPilha { get; set; }
    /// <summary>Quando a peça entrou na pilha.</summary>
    public DateTime? EntradaArmazemEm { get; set; }
    /// <summary>Quando a peça foi tirada da via numa troca. Nulo enquanto não saiu.</summary>
    public DateTime? RetiradaEm { get; set; }
    /// <summary>
    /// Quando a peça ou um registro dela (inspeção, tag, movimentação, troca, leitura) mudou pela última vez. O app usa para baixar
    /// só o que mudou. Preenchida sozinha pelo ViaPermanenteContext, não à mão.
    /// </summary>
    public DateTime? DataAlteracao { get; set; }

    /// <summary>O código da peça: DM-E(empresa, 2 dígitos)-L(lote, 2 dígitos)-(peça, 4 dígitos), como no protótipo.</summary>
    public static string MontarCodigo(int idEmpresa, int idLote, int idDormente) => $"DM-E{idEmpresa:00}-L{idLote:00}-{idDormente:0000}";
}