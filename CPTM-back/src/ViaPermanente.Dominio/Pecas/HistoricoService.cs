using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>
/// O histórico da peça: o sistema segue a história da peça pelas tabelas de evento (chegada, tag, classificação, qualidade,
/// movimentações, leituras, alertas, vistorias, inspeções e trocas), em ordem de data. Nada é guardado à parte.
/// </summary>
public class HistoricoService(IHistoricoRepository historico, IPecaRepository pecas)
{
    /// <summary>Nulo se a peça não existe.</summary>
    public async Task<IReadOnlyList<EventoDaPecaDto>?> DaPeca(int idDormente)
    {
        if (await pecas.Buscar(idDormente) is null)
            return null;
        var h = await historico.Buscar(idDormente);
        var eventos = new List<EventoDaPecaDto>();
        void Add(DateTime data, string tipo, string descricao, int? usuario, string origem) => eventos.Add(new(data, tipo, descricao, usuario, origem));

        if (h.Chegada is { } c)
            Add(c.DataRecebimento, "Chegada", $"Chegou no pedido {c.IdPedido}, chegada {c.IdRecebimento}.", c.IdUsuario, $"RECEBIMENTO {c.IdRecebimento}");
        foreach (var g in h.Gravacoes)
            Add(g.Data, "Gravação da tag", $"{g.Resultado}: {g.TagGravada}.", g.IdUsuario, $"GRAVACAO_TAG {g.IdGravacao}");
        foreach (var t in h.Tags.Where(t => t.VinculadaEm is not null))
            Add(t.VinculadaEm!.Value, "Vínculo de tag", $"Tag {t.TagRfid} vinculada em campo.", t.IdUsuarioVinculou, $"RFID {t.IdRfid}");
        foreach (var c2 in h.Classificacoes)
            Add(c2.Data, "Classificação", $"Espécie {c2.IdEspecie} ({c2.Metodo}).", c2.IdUsuario, $"CLASSIFICACAO_MADEIRA {c2.IdClassificacao}");
        foreach (var v in h.Verificacoes)
            Add(v.Data, "Verificação de qualidade", v.Resultado == ViaPermanente.Dominio.Chegadas.Models.ResultadoVerificacao.Aprovada ? "Aprovada." : $"Reprovada: {v.Motivo}", v.IdUsuario, $"VERIFICACAO_QUALIDADE {v.IdVerificacao}");
        foreach (var m in h.Movimentacoes)
            Add(m.Data, "Movimentação", Movimento(m), m.IdUsuario, $"MOVIMENTACAO {m.IdMovimentacao}");
        foreach (var l in h.Leituras.Where(l => l.IdConferencia is null))
            Add(l.DataLeitura, "Leitura", l.Leitura ? $"Tag {l.TagLida} lida ({l.MetodoCaptura})." : $"A tag não respondeu. {l.Observacao}".Trim(), l.IdUsuario, $"LEITURA_DORMENTE {l.IdLeitura}");
        foreach (var l in h.Leituras.Where(l => l.IdConferencia is not null).GroupBy(l => l.IdConferencia))
            Add(l.First().DataLeitura, "Conferência do armazém", $"Respondeu na conferência {l.Key}.", l.First().IdUsuario, $"CONFERENCIA_ARMAZEM {l.Key}");
        foreach (var a in h.Alertas)
        {
            Add(a.Data, "Alerta", $"{a.Origem}: {a.Tipo}{(a.DesvioMm is decimal d ? $", desvio de {d:0.#} mm" : "")}.", null, $"ALERTA {a.IdAlerta}");
            if (a.DataDecisao is DateTime decidido)
                Add(decidido, "Decisão do alerta", $"{a.Status}{(a.MotivoDescarte is null ? "" : $": {a.MotivoDescarte}")}.", a.IdUsuarioDecisao, $"ALERTA {a.IdAlerta}");
        }
        foreach (var v in h.Vistorias)
            Add(v.Data, "Vistoria", v.Viu ? "Confirmou o defeito no local." : "Não viu defeito no local.", v.IdUsuario, $"VISTORIA_ALERTA {v.IdVistoria}");
        foreach (var i in h.Inspecoes)
            Add(i.FinalizadoEm, "Inspeção", $"{i.Resultado} (checklist v{i.VersaoChecklist}, {i.DuracaoS / 60} min).", i.IdUsuario, $"INSPECAO {i.IdInspecao}");
        foreach (var t in h.Trocas)
        {
            var papel = t.IdDormenteSaiu == idDormente ? "sai" : "entra";
            Add(t.DataPedido, "Troca pedida", $"Troca {t.IdTroca} pedida (a peça {papel}).", t.IdUsuarioPediu, $"TROCA {t.IdTroca}");
            if (t.DataAprovacao is DateTime ap) Add(ap, "Troca decidida", t.MotivoReprovacao is null ? "Aprovada." : $"Reprovada: {t.MotivoReprovacao}", t.IdUsuarioAprovou, $"TROCA {t.IdTroca}");
            if (t.Data is DateTime feita) Add(feita, "Troca feita", $"Equipe {t.Equipe}; a peça {papel}.", t.IdUsuarioFez, $"TROCA {t.IdTroca}");
        }
        return eventos.OrderBy(e => e.Data).ToList();
    }

    private static string Movimento(Movimentacao m) => m.Tipo switch
    {
        TipoMovimentacao.Entrada => $"Entrou no armazém (pilha {m.IdPilhaDestino}).",
        TipoMovimentacao.TrocaDePilha => $"Mudou da pilha {m.IdPilhaOrigem} para a {m.IdPilhaDestino}.",
        TipoMovimentacao.Separada => $"Separada para a troca {m.IdTroca}.",
        TipoMovimentacao.Devolvida => $"Voltou à pilha {m.IdPilhaDestino} (troca {m.IdTroca} recusada ou cancelada).",
        _ => $"Saiu do armazém para a troca {m.IdTroca}."
    };
}