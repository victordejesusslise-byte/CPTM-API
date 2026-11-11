using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
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

    /// <summary>
    /// A via fecha o lote, conferido ID por ID (se um par falhar, nada é gravado e a mensagem diz qual): a peça que sai está na via, no trecho
    /// e dentro da faixa; a que entra é uma das separadas e ainda não usada; as duas têm o comprimento do lote; nenhuma se repete; cada par
    /// tem foto; o total não passa do aprovado. Cada par vira uma troca feita ligada ao lote; as separadas que sobraram voltam para a pilha.
    /// </summary>
    public async Task<LoteTrocaDto?> Fechar(int idLote, FecharLoteTrocaDto dto, int idUsuario)
    {
        var lote = await Buscar(idLote);
        if (lote is null)
            return null;
        // O celular mandou de novo (a rede caiu antes da resposta): devolve o estado atual, sem fazer duas vezes.
        if (dto.IdCliente is Guid idCliente && lote.IdCliente == idCliente)
            return await BuscarPorId(idLote);
        if (lote.Status != StatusLoteTroca.Separado)
            throw new RegraDeNegocioException("Só o lote com as peças separadas é fechado.");
        if (string.IsNullOrWhiteSpace(dto.Equipe))
            throw new RegraDeNegocioException("Fechar o lote exige a equipe.");
        if (dto.Pares.Count == 0)
            throw new RegraDeNegocioException("Troque pelo menos um dormente; se não deu para trocar nada, recuse o lote.");
        if (dto.Pares.Count > lote.QuantidadeAprovada)
            throw new RegraDeNegocioException($"O lote aprovou {lote.QuantidadeAprovada} peças, e vieram {dto.Pares.Count} trocas.");

        var separadas = Separadas(await lotes.Movimentacoes(idLote)).ToDictionary(m => m.IdDormente);
        var faixa = (await DaFaixa(lote)).ToDictionary(p => p.Id);
        var abertas = (await trocas.Listar()).Where(t => t.IdLoteTroca is null && TrocaService.Aberta(t)).ToList();
        var agora = DateTime.Now;
        var mudancas = new MudancasDoLote { LoteAlterado = lote };
        var (sairam, entraram) = (new HashSet<int>(), new HashSet<int>());
        for (var i = 0; i < dto.Pares.Count; i++)
        {
            var par = dto.Pares[i];
            RegraDeNegocioException Erro(string texto) => new($"Dormente {i + 1}: {texto}");
            if (string.IsNullOrWhiteSpace(par.FotoArquivo))
                throw Erro("falta a foto da peça nova no lugar.");
            var hora = HoraDoCampo.De(par.RegistradoEm ?? dto.RegistradoEm, agora);

            // A peça que sai: a da tag lida, dentro da faixa do lote. Se a tag não responde, a escolhida pelo km na lista da faixa.
            int idSaiu;
            if (par.TagSaiuNaoResponde)
                idSaiu = par.IdDormenteSaiu ?? throw Erro("a tag da peça que sai não responde: escolha a peça pelo km.");
            else
            {
                var lida = par.LeituraSaiu?.Tag?.Trim();
                if (string.IsNullOrEmpty(lida))
                    throw Erro("leia a tag da peça que sai, ou marque que ela não responde.");
                idSaiu = (await pecas.BuscarTag(lida))?.IdDormente ?? throw Erro($"a tag {lida} não é de nenhuma peça.");
            }
            if (!faixa.TryGetValue(idSaiu, out var saiu))
                throw Erro("a peça que sai não está na via dentro da faixa do lote.");
            if (!sairam.Add(idSaiu))
                throw Erro($"a peça {saiu.Codigo} já saiu neste lote.");
            if (saiu.IdTamanho != lote.IdTamanho)
                throw Erro($"a peça {saiu.Codigo} tem outro comprimento.");

            // A peça que entra: uma das separadas para o lote, ainda não usada.
            var tagEntra = par.LeituraEntrou?.Tag?.Trim();
            var idEntra = string.IsNullOrEmpty(tagEntra) ? null : (await pecas.BuscarTag(tagEntra))?.IdDormente;
            if (idEntra is not int idEntrou || !separadas.TryGetValue(idEntrou, out var separacao))
                throw Erro("a peça que entra não é uma das separadas para o lote.");
            if (!entraram.Add(idEntrou))
                throw Erro("a peça que entra já entrou neste lote.");

            mudancas.Leituras.Add(par.TagSaiuNaoResponde
                ? new LeituraDormente
                {
                    IdDormente = idSaiu, IdUsuario = idUsuario, MetodoCaptura = MetodoCaptura.ModuloUhf, Leitura = false, DataLeitura = hora,
                    RegistradoEm = hora, RecebidoEm = agora, Observacao = $"Tag da peça que sai não respondeu no lote {Codigo(idLote)}"
                }
                : NoCampo(await leituras.ConferirPeca(idSaiu, (await pecas.TagAtiva(idSaiu))!, par.LeituraSaiu!, idUsuario), hora));
            var tagAtiva = await pecas.TagAtiva(idEntrou) ?? throw Erro("a peça que entra não tem tag ativa.");
            var leituraEntrou = NoCampo(await leituras.ConferirPeca(idEntrou, tagAtiva, par.LeituraEntrou!, idUsuario), hora);

            var km = par.Km ?? saiu.KmProjeto;
            var troca = new Troca
            {
                IdDormenteSaiu = idSaiu, IdDormenteEntrou = idEntrou, IdTrecho = lote.IdTrecho, IdLoteTroca = idLote, Origem = OrigemTroca.Lote, Status = StatusTroca.Feita,
                IdUsuarioPediu = lote.IdUsuarioPediu, DataPedido = lote.DataPedido, IdUsuarioAprovou = lote.IdUsuarioAprovou, DataAprovacao = lote.DataAprovacao,
                IdUsuarioSeparou = separacao.IdUsuario, DataSeparacao = separacao.Data, IdUsuarioFez = idUsuario, Data = hora, RecebidoEm = agora,
                Motivo = lote.Motivo, Equipe = dto.Equipe.Trim(), FotoArquivo = par.FotoArquivo.Trim(), TagEntrou = tagAtiva.TagRfid, Km = km,
                NoiteDoPlano = lote.NoiteDoPlano, Urgente = lote.Urgente
            };
            mudancas.TrocasFeitas.Add((troca, new Movimentacao
            {
                IdDormente = idEntrou, Tipo = TipoMovimentacao.Saida, IdPilhaOrigem = separacao.IdPilhaOrigem, IdUsuario = idUsuario,
                IdLeitor = par.LeituraEntrou!.IdLeitor, Data = hora, RegistradoEm = hora, RecebidoEm = agora
            }, leituraEntrou));
            // A nova herda o trecho e o km; a velha ganha retirada_em; os alertas da velha fecham.
            mudancas.Pecas.Add((idEntrou, d => { d.IdTrecho = saiu.IdTrecho ?? lote.IdTrecho; d.KmProjeto = km; d.IdPilha = null; }));
            mudancas.Pecas.Add((idSaiu, d => d.RetiradaEm = hora));
            mudancas.FecharAlertas.Add((idSaiu, idUsuario, hora));
            foreach (var avulsa in abertas.Where(t => t.IdDormenteSaiu == idSaiu))
            {
                var doAvulso = await avulsas.FechadaPeloLote(avulsa, Codigo(idLote), idUsuario, agora);
                mudancas.AvulsasAlteradas.Add(avulsa);
                mudancas.Movimentacoes.AddRange(doAvulso.Movimentacoes);
                mudancas.Pecas.AddRange(doAvulso.Pecas);
            }
        }
        foreach (var sobra in separadas.Values.Where(s => !entraram.Contains(s.IdDormente)))
            Devolver(mudancas, sobra, idUsuario, agora);

        lote.Status = StatusLoteTroca.Feito;
        lote.IdUsuarioFez = idUsuario;
        lote.Data = HoraDoCampo.De(dto.RegistradoEm, agora);
        lote.Equipe = dto.Equipe.Trim();
        lote.IdCliente = dto.IdCliente;
        lote.RecebidoEm = agora;
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

    /// <summary>A peça separada para o lote volta para a pilha de onde saiu (movimentação Devolvida), como na recusa da troca.</summary>
    private static void Devolver(MudancasDoLote mudancas, Movimentacao separacao, int idUsuario, DateTime agora)
    {
        mudancas.Movimentacoes.Add((new Movimentacao
        {
            IdDormente = separacao.IdDormente, Tipo = TipoMovimentacao.Devolvida, IdPilhaDestino = separacao.IdPilhaOrigem,
            IdUsuario = idUsuario, Data = agora, RegistradoEm = agora, RecebidoEm = agora
        }, null));
        mudancas.Pecas.Add((separacao.IdDormente, d => d.IdPilha = separacao.IdPilhaOrigem));
    }

    private static LeituraDormente NoCampo(LeituraDormente leitura, DateTime noCampo)
    {
        (leitura.DataLeitura, leitura.RegistradoEm) = (noCampo, noCampo);
        return leitura;
    }

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
