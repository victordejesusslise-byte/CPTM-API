using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Integracoes.Dtos;
using ViaPermanente.Dominio.Laudos.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas;

namespace ViaPermanente.Dominio.Laudos;

/// <summary>
/// Os laudos, gerados na hora a partir das tabelas (Três laudos, gerados na hora), um para cada leitor:
/// BR boletim de recebimento (especialista), LI inspeção (técnico e auditoria), LB liberação (a ordem de troca para a via)
/// e LD troca (todos; cresce a cada passo, e a versão sobe). LI, LB e LD trazem as vistorias no local.
/// Ainda é refeito a cada download: guardar cada versão com hash e assinar é ponto em aberto (Laudo congelado e assinado).
/// </summary>
public class LaudoService(IDocumentoPdfRepository pdf, IPecaRepository pecas, IHistoricoRepository historicos, IUsuarioRepository usuarios,
    ITrocaRepository trocasRepo, ChegadaService chegadas, PecaService consulta, InspecaoService inspecoes, AlertaService alertas, TrocaService trocas)
{
    private async Task<DocumentoPdf?> DocumentoBoletim(int idRecebimento)
    {
        var recebimento = await pecas.BuscarRecebimento(idRecebimento);
        if (recebimento is null)
            return null;
        var chegada = (await chegadas.Listar(recebimento.IdPedido))!.First(c => c.Id == idRecebimento);
        var pecasDaCarga = new List<PecaDto>();
        foreach (var feixe in chegada.Feixes)
            pecasDaCarga.AddRange(await consulta.DoFeixe(feixe.Id));
        var nomes = await Nomes();
        var codigo = $"BR-{idRecebimento:0000}";

        return new DocumentoPdf("Boletim de recebimento", codigo, $"Pedido {chegada.IdPedido} · lote {chegada.Lote} · {chegada.Armazem}", DateTime.Now,
        [
            new("Chegada",
            [
                new("Data", Data(chegada.DataRecebimento)), new("Armazém", chegada.Armazem), new("Nota fiscal", chegada.NumeroNota ?? "sem nota"),
                new("Documento de origem", $"{chegada.DocumentoOrigemTipo} {chegada.DocumentoOrigemNumero}".Trim() is { Length: > 0 } doc ? doc : "não informado"),
                new("Peças informadas", chegada.QuantidadeRecebida.ToString()), new("Recebido por", Nome(nomes, recebimento.IdUsuario))
            ]),
            new("Conferência", [new("Resultado", chegada.Confere ? "Confere" : "Diverge"),
                .. chegada.Divergencias.Select(d => new CampoPdf("Divergência", d)), .. chegada.Avisos.Select(a => new CampoPdf("Aviso", a))]),
            new("Feixes", [], new TabelaPdf(["Feixe", "Espécie", "Classe", "Comprimento", "Peças", "Cadastradas", "Tratamento"],
                chegada.Feixes.Select(f => (IReadOnlyList<string>)[f.Numero, f.Especie, $"{f.Classe}ª", FormatoHelper.Metros(f.ComprimentoM),
                    f.Quantidade.ToString(), f.Cadastradas.ToString(), f.Tratamento ?? ""]).ToList())),
            new("Peças e verificação de qualidade",
            [
                new("Aprovadas", pecasDaCarga.Count(p => p.Situacao is not ("Reprovada" or "AguardandoVerificacao")).ToString()),
                new("Reprovadas (tag desativada, devolução)", pecasDaCarga.Count(p => p.Situacao == "Reprovada").ToString()),
                new("Aguardando verificação", pecasDaCarga.Count(p => p.Situacao == "AguardandoVerificacao").ToString())
            ], new TabelaPdf(["Peça", "Tag", "Espécie", "Situação"],
                pecasDaCarga.Select(p => (IReadOnlyList<string>)[p.Codigo, p.Tag ?? "inativa", p.Especie, p.Situacao]).ToList()))
        ]);
    }

    private async Task<DocumentoPdf?> DocumentoInspecao(int idInspecao)
    {
        var inspecao = await inspecoes.BuscarPorId(idInspecao);
        if (inspecao is null)
            return null;
        var peca = (await consulta.BuscarPorId(inspecao.IdDormente))!;
        var alerta = inspecao.IdAlerta is int idAlerta ? await alertas.BuscarPorId(idAlerta) : null;
        var nomes = await Nomes();
        var secoes = new List<SecaoPdf>
        {
            SecaoPeca(peca),
            new("Inspeção",
            [
                new("Técnico", Nome(nomes, inspecao.IdUsuario)), new("Início", Data(inspecao.IniciadoEm)), new("Fim", Data(inspecao.FinalizadoEm)),
                new("Duração", $"{inspecao.DuracaoS / 60} min {inspecao.DuracaoS % 60} s"), new("Checklist", $"versão {inspecao.VersaoChecklist}"),
                new("Resultado", inspecao.Resultado),
                new("Motivos", inspecao.MotivosDeReprovacao.Count == 0 ? "—" : string.Join("; ", inspecao.MotivosDeReprovacao)),
                new("Retrabalho de", inspecao.IdInspecaoOrigem is int o ? $"LI-{o:0000}" : "—")
            ]),
            new("Checklist",
            [
                new("Estado de conservação", inspecao.EstadoConservacao), new("Rachadura", inspecao.Rachadura), new("Ataque biológico", inspecao.AtaqueBiologico),
                new("Umidade", inspecao.UmidadePct is decimal u ? $"{u:0.#}%" : "—"), new("Vida útil", inspecao.VidaUtilMeses is int v ? $"{v} meses" : "—"),
                new("Tipo de madeira confere", inspecao.TipoMadeiraConfere ? "sim" : "não"), new("Observação", inspecao.Observacao ?? "—"), new("Foto", inspecao.FotoArquivo)
            ])
        };
        if (alerta is not null)
            secoes.Add(SecaoAlerta(alerta));
        secoes.Add(await SecaoVistorias(inspecao.IdDormente, nomes));
        return new DocumentoPdf("Laudo de inspeção", $"LI-{idInspecao:0000}", peca.Codigo, DateTime.Now, secoes);
    }

    /// <summary>A ordem de troca para a via, numa página, depois da aprovação.</summary>
    private async Task<DocumentoPdf?> DocumentoLiberacao(int idTroca)
    {
        var troca = await trocas.BuscarPorId(idTroca);
        if (troca is null)
            return null;
        if (troca.Status is "Pedida" or "Reprovada")
            throw new RegraDeNegocioException("A liberação sai depois que a troca é aprovada.");
        var saiu = (await consulta.BuscarPorId(troca.IdDormenteSaiu))!;
        var nomes = await Nomes();
        return new DocumentoPdf("Liberação para troca", $"LB-{idTroca:0000}", $"{saiu.Codigo} · km {Km(troca.Km)}", DateTime.Now,
        [
            SecaoPeca(saiu, "Peça que sai"),
            new("Pedido e aprovação",
            [
                new("Origem", troca.Origem), new("Motivo", troca.Motivo ?? (troca.IdInspecao is int i ? $"inspeção reprovada (LI-{i:0000})" : "—")),
                new("Pedida por", $"{Nome(nomes, troca.IdUsuarioPediu)} em {Data(troca.DataPedido)}"),
                new("Aprovada por", troca.IdUsuarioAprovou is int a ? $"{Nome(nomes, a)} em {Data(troca.DataAprovacao)}" : "—"),
                new("Situação", troca.Status)
            ]),
            new("Peça separada", troca.IdDormenteEntrou is null
                ? [new("Peça", troca.Status == "AguardandoPeca" ? "aguardando peça compatível no armazém" : "ainda não separada")]
                : [new("Peça", troca.CodigoEntrou ?? ""), new("Tag", troca.TagEntrou ?? ""),
                   new("Separada por", troca.IdUsuarioSeparou is int s ? $"{Nome(nomes, s)} em {Data(troca.DataSeparacao)}" : "—")]),
            await SecaoVistorias(troca.IdDormenteSaiu, nomes)
        ]);
    }

    /// <summary>O laudo da troca: cresce a cada passo, e a versão sobe junto.</summary>
    private async Task<DocumentoPdf?> DocumentoTroca(int idTroca)
    {
        var troca = await trocas.BuscarPorId(idTroca);
        if (troca is null)
            return null;
        var nomes = await Nomes();
        var recusas = await trocasRepo.Recusas([idTroca]);
        var passos = new List<IReadOnlyList<string>> { new[] { "Pedida", Nome(nomes, troca.IdUsuarioPediu), Data(troca.DataPedido), troca.Motivo ?? troca.Origem } };
        if (troca.DataAprovacao is not null)
            passos.Add(new[] { troca.MotivoReprovacao is null ? "Aprovada" : "Reprovada", Nome(nomes, troca.IdUsuarioAprovou), Data(troca.DataAprovacao), troca.MotivoReprovacao ?? "" });
        if (troca.DataSeparacao is not null)
            passos.Add(new[] { "Separada", Nome(nomes, troca.IdUsuarioSeparou), Data(troca.DataSeparacao), troca.CodigoEntrou ?? "" });
        if (troca.Data is not null)
            passos.Add(new[] { "Feita", Nome(nomes, troca.IdUsuarioFez), Data(troca.Data), $"equipe {troca.Equipe}; foto {troca.FotoArquivo}" });
        var versao = passos.Count + recusas.Count;
        var saiu = (await consulta.BuscarPorId(troca.IdDormenteSaiu))!;
        var secoes = new List<SecaoPdf>
        {
            SecaoPeca(saiu, "Peça que sai"),
            new("Passos", [new("Situação", troca.Status), new("Dias desde o pedido", troca.DiasDesdeOPedido.ToString())],
                new TabelaPdf(["Passo", "Quem", "Quando", "Detalhe"], passos)),
            new("Recusas da via", [], new TabelaPdf(["Data", "Quem", "Motivo", "Decisão do técnico"],
                recusas.Select(r => (IReadOnlyList<string>)[Data(r.Data), Nome(nomes, r.IdUsuario), r.Motivo,
                    r.Decisao is null ? "aguardando" : $"{r.Decisao}{(r.Resposta is null ? "" : $": {r.Resposta}")}"]).ToList()))
        };
        if (troca.IdInspecao is int idInspecao && await inspecoes.BuscarPorId(idInspecao) is { IdAlerta: int idAlerta } && await alertas.BuscarPorId(idAlerta) is { } alerta)
            secoes.Add(SecaoAlerta(alerta));
        secoes.Add(await SecaoVistorias(troca.IdDormenteSaiu, nomes));
        return new DocumentoPdf("Laudo da troca", $"LD-{idTroca:0000}-v{versao}", $"{saiu.Codigo} · km {Km(troca.Km)}", DateTime.Now, secoes);
    }

    // ---------- seções comuns ----------

    private static SecaoPdf SecaoPeca(PecaDto p, string titulo = "Peça")
        => new(titulo,
        [
            new("Peça", p.Codigo), new("Tag", p.Tag ?? "sem tag ativa"), new("Espécie", $"{p.Especie} · {p.Classe}ª classe"),
            new("Comprimento", FormatoHelper.Metros(p.ComprimentoM)), new("Km", Km(p.KmProjeto)), new("Situação", p.Situacao)
        ]);

    private static SecaoPdf SecaoAlerta(AlertaDto a)
    {
        var campos = new List<CampoPdf> { new("Alerta", $"{a.Origem}: {a.Tipo}"), new("Data", Data(a.Data)), new("Situação", a.Status) };
        if (a.DesvioMm is decimal d) campos.Add(new("Desvio medido", $"{d:0.#} mm"));
        if (a.Deteccao is { } q)
            campos.AddRange([new("Quadro da câmera", q.QuadroArquivo), new("Marcação", $"x {q.CaixaX}, y {q.CaixaY}, {q.CaixaLargura} × {q.CaixaAltura} px"),
                new("Defeito", $"{q.Defeito}, grau {q.Grau}"), new("Confiança", FormatoHelper.Porcento(q.Confianca)), new("Versão do modelo", q.VersaoModelo)]);
        return new("Alerta de origem", campos);
    }

    private async Task<SecaoPdf> SecaoVistorias(int idDormente, IReadOnlyDictionary<int, string> nomes)
        => new("Vistorias no local", [], new TabelaPdf(["Data", "Quem", "O que viu", "Tag lida", "Foto"],
            (await historicos.Buscar(idDormente)).Vistorias.OrderBy(v => v.Data)
                .Select(v => (IReadOnlyList<string>)[Data(v.Data), Nome(nomes, v.IdUsuario), v.Viu ? "confirmou o defeito" : "não viu defeito", v.TagLida ?? "—", v.FotoArquivo]).ToList()));

    /// <summary>
    /// O laudo como documento, para a tela desenhar as mesmas páginas do PDF antes de baixar (Victor, 2026-10-18).
    /// Tipo: recebimento (BR), inspecao (LI), liberacao (LB) ou troca (LD). Nulo se o registro não existe.
    /// </summary>
    public Task<DocumentoPdf?> Documento(string tipo, int id) => tipo switch
    {
        "recebimento" => DocumentoBoletim(id),
        "inspecao" => DocumentoInspecao(id),
        "liberacao" => DocumentoLiberacao(id),
        "troca" => DocumentoTroca(id),
        _ => throw new RegraDeNegocioException($"Não há laudo do tipo {tipo}.")
    };

    /// <summary>BR: boletim de recebimento de uma chegada, em PDF.</summary>
    public async Task<ArquivoDto?> BoletimRecebimento(int idRecebimento) => await DocumentoBoletim(idRecebimento) is { } d ? Arquivo(d) : null;
    /// <summary>LI: laudo da inspeção, em PDF.</summary>
    public async Task<ArquivoDto?> LaudoInspecao(int idInspecao) => await DocumentoInspecao(idInspecao) is { } d ? Arquivo(d) : null;
    /// <summary>LB: liberação para troca, em PDF.</summary>
    public async Task<ArquivoDto?> Liberacao(int idTroca) => await DocumentoLiberacao(idTroca) is { } d ? Arquivo(d) : null;
    /// <summary>LD: laudo da troca, em PDF.</summary>
    public async Task<ArquivoDto?> LaudoTroca(int idTroca) => await DocumentoTroca(idTroca) is { } d ? Arquivo(d) : null;

    private ArquivoDto Arquivo(DocumentoPdf documento) => new($"{documento.Codigo}.pdf", pdf.Montar(documento));

    private async Task<IReadOnlyDictionary<int, string>> Nomes() => (await usuarios.Listar()).ToDictionary(u => u.Usuario.IdUsuario, u => u.Pessoa.Nome);
    private static string Nome(IReadOnlyDictionary<int, string> nomes, int? id) => id is int i ? nomes.GetValueOrDefault(i, $"usuário {i}") : "—";
    private static string Data(DateTime? d) => d?.ToString("dd/MM/yyyy HH:mm") ?? "—";
    private static string Km(decimal? km) => km is decimal k ? k.ToString("0.000", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) : "—";
}