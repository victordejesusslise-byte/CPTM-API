using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pecas.Models;

/// <summary>Tudo o que as tabelas de evento guardam de uma peça (Histórico por eventos, numa visão).</summary>
public record HistoricoDaPeca(
    Recebimento? Chegada, IReadOnlyList<Rfid> Tags, IReadOnlyList<GravacaoTag> Gravacoes, IReadOnlyList<ClassificacaoMadeira> Classificacoes,
    IReadOnlyList<VerificacaoQualidade> Verificacoes, IReadOnlyList<Movimentacao> Movimentacoes, IReadOnlyList<LeituraDormente> Leituras,
    IReadOnlyList<Alerta> Alertas, IReadOnlyList<VistoriaAlerta> Vistorias, IReadOnlyList<Inspecao> Inspecoes, IReadOnlyList<Troca> Trocas);