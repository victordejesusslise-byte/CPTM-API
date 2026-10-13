using ViaPermanente.Dominio.Chegadas.Models;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>
/// Tudo o que a tela da peça precisa, numa consulta só: a peça, o feixe, a chegada, o lote,
/// a tag ativa, a espécie vigente (a classificação de maior id; sem classificação, a do feixe) e o resultado da verificação.
/// </summary>
public record PecaResumo(
    int IdDormente, int IdLote, int IdEmpresa, int IdPedido,
    int IdFeixe, string NumeroFeixe, int IdTamanho, int IdArmazemChegada,
    string? TagFornecedor, int? IdPilha, DateTime? EntradaArmazemEm, DateTime? RetiradaEm, int? IdTrecho, decimal? KmProjeto,
    string? TagAtiva, int IdEspecieVigente, ResultadoVerificacao? Verificacao);