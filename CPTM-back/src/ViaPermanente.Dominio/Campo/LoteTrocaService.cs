using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// O lote de troca (Victor, 2026-11-11): várias trocas de uma vez, no mesmo padrão da troca em quatro passos. O técnico pede o trecho, a faixa
/// de km, a quantidade e o comprimento, sem escolher as peças; quem aprova (PARAMETRO troca_quem_aprova) aprova o lote inteiro, podendo baixar
/// a quantidade com o porquê; o pátio separa até a quantidade aprovada; na noite, a via lê a tag de cada dormente que sai e de cada um que
/// entra, tira uma foto por dormente e fecha o lote, conferido ID por ID. Cada par vira uma TROCA normal (origem Lote) ligada ao lote.
/// </summary>
public class LoteTrocaService(ILoteTrocaRepository lotes, ITrocaRepository trocas, IPecaRepository pecas, IParametroRepository parametros,
    ICatalogoRepository catalogo, ITrechoMapaRepository trechos, LeituraService leituras, PecaService consulta, TrocaService avulsas)
{
    private static readonly StatusLoteTroca[] Separaveis = [StatusLoteTroca.Aprovado, StatusLoteTroca.AguardandoPeca, StatusLoteTroca.Separado];

    public static string Codigo(int idLote) => $"LT-{idLote:0000}";

    public async Task<LoteTrocaDto> Pedir(NovoLoteTrocaDto dto, int idUsuario)
    {
        if (await trocas.BuscarTrecho(dto.IdTrecho) is null)
            throw new RegraDeNegocioException("Trecho não encontrado.");
        if (dto.KmInicio > dto.KmFim)
            throw new RegraDeNegocioException("O km de início vem antes do km do fim.");
        if (dto.Quantidade < 1)
            throw new RegraDeNegocioException("O lote precisa de pelo menos uma peça.");
        if ((await catalogo.ListarTamanhos()).All(t => t.IdTamanho != dto.IdTamanho))
            throw new RegraDeNegocioException("Comprimento de dormente não encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new RegraDeNegocioException("O lote de troca precisa de motivo.");

        var lote = new LoteTroca
        {
            IdTrecho = dto.IdTrecho, KmInicio = dto.KmInicio, KmFim = dto.KmFim, IdTamanho = dto.IdTamanho, QuantidadePedida = dto.Quantidade,
            Motivo = dto.Motivo.Trim(), Observacao = Limpo(dto.Observacao), Status = StatusLoteTroca.Pedido, IdUsuarioPediu = idUsuario, DataPedido = DateTime.Now
        };
        await lotes.Gravar(new MudancasDoLote { LoteNovo = lote });
        return (await BuscarPorId(lote.IdLoteTroca))!;
    }

    /// <summary>Aprova o lote inteiro (a quantidade pode baixar, com o porquê) ou reprova (com motivo). Aprovado sem peça compatível fica aguardando peça.</summary>
    public async Task<LoteTrocaDto?> Decidir(int idLote, DecisaoLoteTrocaDto dto, Cargo cargo, int idUsuario)
    {
        var lote = await Buscar(idLote);
        if (lote is null)
            return null;
        QuemAprovaTroca.Conferir((await parametros.Buscar(QuemAprovaTroca.Chave))?.Valor, cargo);
        if (lote.Status != StatusLoteTroca.Pedido)
            throw new RegraDeNegocioException("Só o lote pedido é aprovado ou reprovado.");

        lote.IdUsuarioAprovou = idUsuario;
        lote.DataAprovacao = DateTime.Now;
        if (dto.Aprovar)
        {
            var quantidade = dto.Quantidade ?? lote.QuantidadePedida;
            if (quantidade < 1 || quantidade > lote.QuantidadePedida)
                throw new RegraDeNegocioException($"A quantidade aprovada vai de 1 a {lote.QuantidadePedida}.");
            if (quantidade < lote.QuantidadePedida && string.IsNullOrWhiteSpace(dto.Motivo))
                throw new RegraDeNegocioException("Para aprovar menos peças que as pedidas, escreva o porquê.");
            lote.QuantidadeAprovada = quantidade;
            lote.MotivoMenos = quantidade < lote.QuantidadePedida ? dto.Motivo!.Trim() : null;
            lote.Status = (await Compativeis(lote)).Count > 0 ? StatusLoteTroca.Aprovado : StatusLoteTroca.AguardandoPeca;
            // O plano da noite: até o corte entra hoje; depois, na noite seguinte, salvo urgente.
            var corte = await Corte();
            lote.Urgente = dto.Urgente && TimeOnly.FromDateTime(lote.DataAprovacao.Value) > corte;
            lote.NoiteDoPlano = PlanoDaNoite.NoiteDe(lote.DataAprovacao.Value, corte, dto.Urgente);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Motivo))
                throw new RegraDeNegocioException("Reprovar exige motivo.");
            lote.Status = StatusLoteTroca.Reprovado;
            lote.MotivoReprovacao = dto.Motivo.Trim();
        }
        await lotes.Gravar(new MudancasDoLote { LoteAlterado = lote });
        return await BuscarPorId(idLote);
    }

    /// <summary>As peças que servem no lote: o comprimento do lote, classe igual ou melhor que a do trecho, do armazém mais perto da faixa. A primeira é a sugerida.</summary>
    public async Task<IReadOnlyList<CompativelDto>?> ListarCompativeis(int idLote)
        => await Buscar(idLote) is { } lote ? await Compativeis(lote) : null;

    /// <summary>O pátio lê a peça compatível na pilha (alvo único) e a separa para o lote, até a quantidade aprovada.</summary>
    public async Task<LoteTrocaDto?> Separar(int idLote, SepararTrocaDto dto, int idUsuario)
    {
        var lote = await Buscar(idLote);
        if (lote is null)
            return null;
        if (!Separaveis.Contains(lote.Status))
            throw new RegraDeNegocioException("Só o lote aprovado é separado.");
        if (Separadas(await lotes.Movimentacoes(idLote)).Count >= lote.QuantidadeAprovada)
            throw new RegraDeNegocioException($"O lote já tem as {lote.QuantidadeAprovada} peças aprovadas separadas.");
        var compativel = (await Compativeis(lote)).FirstOrDefault(c => c.Peca.Id == dto.IdDormente)
            ?? throw new RegraDeNegocioException("Esta peça não serve no lote: precisa ter o comprimento do lote e classe igual ou melhor que a do trecho, e estar no estoque.");
        var tag = await pecas.TagAtiva(dto.IdDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");
        var leitura = await leituras.ConferirPeca(dto.IdDormente, tag, dto.Leitura, idUsuario);

        var agora = DateTime.Now;
        lote.Status = StatusLoteTroca.Separado;
        var mudancas = new MudancasDoLote { LoteAlterado = lote };
        mudancas.Movimentacoes.Add((new Movimentacao
        {
            IdDormente = dto.IdDormente, Tipo = TipoMovimentacao.Separada, IdPilhaOrigem = compativel.Peca.IdPilha,
            IdUsuario = idUsuario, IdLeitor = dto.Leitura.IdLeitor, Data = agora, RegistradoEm = agora, RecebidoEm = agora
        }, leitura));
        mudancas.Pecas.Add((dto.IdDormente, d => d.IdPilha = null)); // saiu da pilha, reservada para o lote
        await lotes.Gravar(mudancas);
        return await BuscarPorId(idLote);
    }

    public async Task<IReadOnlyList<LoteTrocaDto>> Listar(StatusLoteTroca? status) => await Montar(await lotes.Listar(status), aberto: false);

    /// <summary>O lote aberto: com as peças na via dentro da faixa e as que têm pedido avulso aberto.</summary>
    public async Task<LoteTrocaDto?> BuscarPorId(int idLote) => (await Montar(await lotes.Listar(idLote: idLote), aberto: true)).FirstOrDefault();

    // ---------- apoio ----------

    private async Task<LoteTroca?> Buscar(int idLote) => (await lotes.Listar(idLote: idLote)).FirstOrDefault();

    private async Task<TimeOnly> Corte() => PlanoDaNoite.Corte((await parametros.Buscar(PlanoDaNoite.ChaveHorario))?.Valor);

    /// <summary>Onde cada peça do lote está: a última movimentação dela no lote (Separada: ainda separada para ele).</summary>
    private static IReadOnlyList<Movimentacao> Ultimas(IReadOnlyList<Movimentacao> movimentacoes)
        => movimentacoes.GroupBy(m => m.IdDormente).Select(g => g.Last()).ToList();

    private static IReadOnlyList<Movimentacao> Separadas(IReadOnlyList<Movimentacao> movimentacoes)
        => Ultimas(movimentacoes).Where(m => m.Tipo == TipoMovimentacao.Separada).ToList();

    private async Task<IReadOnlyList<CompativelDto>> Compativeis(LoteTroca lote)
    {
        var classeExigida = (await trocas.BuscarTrecho(lote.IdTrecho))!.ClasseExigida;
        var fora = (await trocas.PecasReservadas()).ToHashSet();
        var armazens = (await catalogo.ListarArmazens()).ToDictionary(a => a.IdArmazem, a => a.Km);
        return CompatibilidadeTroca.Ordenar(await consulta.Estoque(), lote.IdTamanho, classeExigida, (lote.KmInicio + lote.KmFim) / 2, armazens, fora, DateTime.Now);
    }

    /// <summary>As peças na via do trecho do lote, dentro da faixa de km, na ordem do km.</summary>
    private async Task<IReadOnlyList<PecaDto>> DaFaixa(LoteTroca lote)
    {
        var ids = (await trechos.PecasNaVia(lote.IdTrecho)).Select(p => p.IdDormente).ToList();
        return (await consulta.PorIds(ids)).Values
            .Where(p => p.Situacao == "NaVia" && p.KmProjeto >= lote.KmInicio && p.KmProjeto <= lote.KmFim)
            .OrderBy(p => p.KmProjeto).ThenBy(p => p.Id).ToList();
    }

    private static PecaDoLoteDto DoLote(PecaDto p) => new(p.Id, p.Codigo, p.Tag, p.KmProjeto, p.IdTamanho, p.Pilha);

    private async Task<IReadOnlyList<LoteTrocaDto>> Montar(IReadOnlyList<LoteTroca> lista, bool aberto)
    {
        if (lista.Count == 0)
            return [];
        var comprimentos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho, t => t.ComprimentoM);
        var nomes = (await trechos.Trechos()).ToDictionary(t => t.IdTrecho, t => $"{t.Inicio} – {t.Fim}");
        var resultado = new List<LoteTrocaDto>();
        foreach (var l in lista)
        {
            var ultimas = Ultimas(await lotes.Movimentacoes(l.IdLoteTroca));
            var porPeca = await consulta.PorIds(ultimas.Select(m => m.IdDormente).ToList());
            IReadOnlyList<PecaDoLoteDto> Na(TipoMovimentacao tipo)
                => ultimas.Where(m => m.Tipo == tipo && porPeca.ContainsKey(m.IdDormente)).Select(m => DoLote(porPeca[m.IdDormente])).ToList();
            var faixa = aberto ? await DaFaixa(l) : [];
            var naFaixa = faixa.Select(p => p.Id).ToHashSet();
            var comAvulso = aberto
                ? (await trocas.Listar()).Where(t => t.IdLoteTroca is null && TrocaService.Aberta(t) && naFaixa.Contains(t.IdDormenteSaiu))
                    .Select(t => DoLote(faixa.First(p => p.Id == t.IdDormenteSaiu))).ToList()
                : [];
            var r = (await lotes.Recusas(l.IdLoteTroca)).LastOrDefault();
            resultado.Add(new LoteTrocaDto(l.IdLoteTroca, Codigo(l.IdLoteTroca), l.Status.ToString(), l.IdTrecho, nomes.GetValueOrDefault(l.IdTrecho, $"trecho {l.IdTrecho}"),
                (await trocas.BuscarTrecho(l.IdTrecho))?.ClasseExigida ?? 1, l.KmInicio, l.KmFim, l.IdTamanho, comprimentos.GetValueOrDefault(l.IdTamanho),
                l.QuantidadePedida, l.QuantidadeAprovada, l.Motivo, l.Observacao, l.IdUsuarioPediu, l.DataPedido, l.IdUsuarioAprovou, l.DataAprovacao,
                l.MotivoReprovacao, l.MotivoMenos, l.NoiteDoPlano, l.Urgente, l.IdUsuarioFez, l.Data, l.Equipe, l.Cancelamento,
                Na(TipoMovimentacao.Separada), Na(TipoMovimentacao.Devolvida), await avulsas.DoLote(l.IdLoteTroca),
                faixa.Select(DoLote).ToList(), comAvulso,
                r is null ? null : new RecusaDto(r.IdRecusa, r.Data, r.IdUsuario, r.Motivo, r.Descricao, r.FotoArquivo,
                    r.Decisao?.ToString(), r.IdUsuarioDecisao, r.DataDecisao, r.Resposta)));
        }
        return resultado;
    }

    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
