namespace ViaPermanente.Dominio.Pecas.Dtos;

/// <summary>A peça como a tela mostra. A situação, a nota e a posição são calculadas na hora, não guardadas.</summary>
/// <param name="Id">Id da peça (→ DORMENTE).</param>
/// <param name="Codigo">Código impresso: DM-E(empresa)-L(lote)-(id da peça), ex.: DM-E01-L01-0028.</param>
/// <param name="Tag">O número da tag que vale hoje (BR e 10 dígitos). Nulo sem tag.</param>
/// <param name="TagFornecedor">A identificação do fornecedor (não é a nossa tag).</param>
/// <param name="IdPedido">Id do pedido (→ PEDIDO).</param>
/// <param name="IdLote">Id do lote (→ LOTE).</param>
/// <param name="IdFeixe">Id do feixe (→ FEIXE).</param>
/// <param name="NumeroFeixe">O número do feixe.</param>
/// <param name="IdEspecie">Id da espécie que vale hoje (→ ESPECIE).</param>
/// <param name="Especie">O nome científico da espécie.</param>
/// <param name="Classe">A classe: 1 ou 2 (vem do feixe).</param>
/// <param name="IdTamanho">Id do tamanho (→ TAMANHO_DORMENTE).</param>
/// <param name="ComprimentoM">O comprimento da peça, em m.</param>
/// <param name="IdArmazem">Id do armazém: o da pilha, ou o da chegada (→ ARMAZEM).</param>
/// <param name="Armazem">O nome do armazém.</param>
/// <param name="IdPilha">Id da pilha (→ PILHA). Nulo fora do armazém.</param>
/// <param name="Pilha">O código da pilha.</param>
/// <param name="EntradaArmazemEm">Quando a peça entrou na pilha.</param>
/// <param name="IdTrecho">Id do trecho na via (→ TRECHO). Nulo fora da via.</param>
/// <param name="KmProjeto">O km da peça na via, em km com 3 casas.</param>
/// <param name="Situacao">AguardandoVerificacao, Aprovada, Reprovada, NoArmazem, NaVia ou Retirada.</param>
/// <param name="Nota">A nota única de estado: Bom, Regular, Ruim ou Crítico. Nula sem avaliação.</param>
/// <param name="Posicao">O ID de posição na via (L09-213100-0012 = linha, km e nº da peça no trecho). Nulo fora da via.</param>
public record PecaDto(int Id, string Codigo, string? Tag, string? TagFornecedor, int IdPedido, int IdLote, int IdFeixe, string NumeroFeixe,
    int IdEspecie, string Especie, byte Classe, int IdTamanho, decimal ComprimentoM,
    int IdArmazem, string Armazem, int? IdPilha, string? Pilha, DateTime? EntradaArmazemEm, int? IdTrecho, decimal? KmProjeto, string Situacao,
    string? Nota = null, string? Posicao = null);