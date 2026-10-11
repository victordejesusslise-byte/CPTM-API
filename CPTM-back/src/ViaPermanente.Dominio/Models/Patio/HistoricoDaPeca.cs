using ViaPermanente.Dominio.Models.Campo;
using ViaPermanente.Dominio.Models.Chegada;
using ViaPermanente.Dominio.Models.Compras;

namespace ViaPermanente.Dominio.Models.Patio;

/// <summary>Tudo o que as tabelas de evento guardam de uma peça (Histórico por eventos, numa visão).</summary>
public record HistoricoDaPeca(
    Recebimento? Chegada, IReadOnlyList<Rfid> Tags, IReadOnlyList<GravacaoTag> Gravacoes, IReadOnlyList<ClassificacaoMadeira> Classificacoes,
    IReadOnlyList<VerificacaoQualidade> Verificacoes, IReadOnlyList<Movimentacao> Movimentacoes, IReadOnlyList<LeituraDormente> Leituras,
    IReadOnlyList<Alerta> Alertas, IReadOnlyList<VistoriaAlerta> Vistorias, IReadOnlyList<Inspecao> Inspecoes, IReadOnlyList<Troca> Trocas);