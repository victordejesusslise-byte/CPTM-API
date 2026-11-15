using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Tudo o que a tela da peça precisa, numa consulta só: a peça, o feixe, a chegada, o lote, a tag vigente,
/// a espécie que vale hoje e o resultado da verificação.</summary>
/// <param name="IdDormente">Id da peça (→ DORMENTE).</param>
/// <param name="Codigo">Código impresso: DM-E(empresa)-L(lote)-(id da peça), ex.: DM-E01-L01-0028.</param>
/// <param name="IdLote">Id do lote (→ LOTE).</param>
/// <param name="IdEmpresa">Id da empresa fornecedora (→ EMPRESA).</param>
/// <param name="IdPedido">Id do pedido (→ PEDIDO).</param>
/// <param name="IdFeixe">Id do feixe (→ FEIXE).</param>
/// <param name="NumeroFeixe">O número do feixe.</param>
/// <param name="IdTamanho">Id do tamanho da peça (→ TAMANHO_DORMENTE).</param>
/// <param name="IdArmazemChegada">Id do armazém onde a carga chegou (→ ARMAZEM).</param>
/// <param name="TagFornecedor">A identificação do fornecedor (não é a nossa tag).</param>
/// <param name="IdPilha">Id da pilha onde a peça está (→ PILHA). Nulo fora do armazém.</param>
/// <param name="EntradaArmazemEm">Quando a peça entrou na pilha.</param>
/// <param name="RetiradaEm">Quando a peça foi tirada da via. Nulo enquanto não saiu.</param>
/// <param name="IdTrecho">Id do trecho onde a peça está na via (→ TRECHO).</param>
/// <param name="KmProjeto">O km da peça na via, em km com 3 casas.</param>
/// <param name="TagVigente">O número da tag que vale hoje (BR e 10 dígitos). Nulo sem tag.</param>
/// <param name="IdEspecieVigente">Id da espécie que vale hoje: a da classificação mais nova, ou a do feixe (→ ESPECIE).</param>
/// <param name="Verificacao">O resultado da verificação de qualidade. Nulo: ainda não verificada.</param>
/// <param name="Classe">A classe da peça: 1 ou 2 (vem do feixe).</param>
public record PecaResumo(
    int IdDormente, string? Codigo, int IdLote, int IdEmpresa, int IdPedido,
    int IdFeixe, string NumeroFeixe, int IdTamanho, int IdArmazemChegada,
    string? TagFornecedor, int? IdPilha, DateTime? EntradaArmazemEm, DateTime? RetiradaEm, int? IdTrecho, decimal? KmProjeto,
    string? TagVigente, int IdEspecieVigente, ResultadoVerificacao? Verificacao, byte Classe);