using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Tudo o que as tabelas de evento guardam de uma peça, para montar o histórico dela
/// (decisão: Histórico por eventos, numa visão).</summary>
/// <param name="Chegada">A chegada em que a peça veio (RECEBIMENTO).</param>
/// <param name="Etiquetas">As etiquetas da peça: gravada, ligada e desligada (GRAVACAO_TAG).</param>
/// <param name="Classificacoes">As classificações da espécie (CLASSIFICACAO_MADEIRA).</param>
/// <param name="Verificacoes">As verificações de qualidade (VERIFICACAO_QUALIDADE).</param>
/// <param name="Movimentacoes">As entradas e saídas do armazém e as trocas de pilha (MOVIMENTACAO).</param>
/// <param name="Leituras">As leituras da tag (LEITURA_DORMENTE).</param>
/// <param name="Alertas">Os alertas da peça (ALERTA).</param>
/// <param name="Vistorias">O que o operador da via viu em cada alerta (VISTORIA_ALERTA).</param>
/// <param name="Inspecoes">As inspeções do técnico (INSPECAO).</param>
/// <param name="Trocas">As trocas em que a peça sai ou entra (TROCA).</param>
public record HistoricoDaPeca(
    Recebimento? Chegada, IReadOnlyList<GravacaoTag> Etiquetas, IReadOnlyList<ClassificacaoMadeira> Classificacoes,
    IReadOnlyList<VerificacaoQualidade> Verificacoes, IReadOnlyList<Movimentacao> Movimentacoes, IReadOnlyList<LeituraDormente> Leituras,
    IReadOnlyList<Alerta> Alertas, IReadOnlyList<VistoriaAlerta> Vistorias, IReadOnlyList<Inspecao> Inspecoes, IReadOnlyList<Troca> Trocas);