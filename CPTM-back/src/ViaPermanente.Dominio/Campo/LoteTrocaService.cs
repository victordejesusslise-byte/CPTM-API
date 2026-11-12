using System.Globalization;
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
    private static readonly StatusLoteTroca[] Abertos = [StatusLoteTroca.Pedido, StatusLoteTroca.Aprovado, StatusLoteTroca.AguardandoPeca, StatusLoteTroca.Separado, StatusLoteTroca.Recusado];
    /// <summary>A troca avulsa já aprovada: estas são obrigatórias no lote que cobre a peça (Victor, 2026-11-12).</summary>
    private static readonly StatusTroca[] AvulsasAprovadas = [StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada];

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
        // As obrigatórias somam à quantidade aprovada; a que já tem peça separada no avulso usa aquela peça (Victor, 2026-11-12).
        var obrigatorias = AvulsasObrigatorias(lote, await DaFaixa(lote), await AvulsasAbertas());
        var limite = (lote.QuantidadeAprovada ?? 0) + obrigatorias.Count(o => o.Troca.Status != StatusTroca.Separada);
        if (Separadas(await lotes.Movimentacoes(idLote)).Count >= limite)
            throw new RegraDeNegocioException(obrigatorias.Count > 0
                ? $"O lote já tem as {limite} peças separadas (as aprovadas e as das trocas obrigatórias)."
                : $"O lote já tem as {lote.QuantidadeAprovada} peças aprovadas separadas.");
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

        var separadas = Separadas(await lotes.Movimentacoes(idLote)).ToDictionary(m => m.IdDormente);
        var daFaixa = await DaFaixa(lote);
        var faixa = daFaixa.ToDictionary(p => p.Id);
        var abertas = await AvulsasAbertas();
        // As trocas avulsas já aprovadas na faixa: a via troca no lote ou diz que não deu; elas somam à quantidade aprovada.
        var obrigatorias = AvulsasObrigatorias(lote, daFaixa, abertas);
        var limite = (lote.QuantidadeAprovada ?? 0) + obrigatorias.Count;
        if (dto.Pares.Count > limite)
            throw new RegraDeNegocioException(obrigatorias.Count > 0
                ? $"O lote aprovou {lote.QuantidadeAprovada} peças e tem {obrigatorias.Count} obrigatórias: cabem {limite} trocas, e vieram {dto.Pares.Count}."
                : $"O lote aprovou {lote.QuantidadeAprovada} peças, e vieram {dto.Pares.Count} trocas.");
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

            // A peça que entra: uma das separadas para o lote, ainda não usada; ou a que o pátio separou para o avulso obrigatório desta mesma peça.
            var tagEntra = par.LeituraEntrou?.Tag?.Trim();
            var idEntra = string.IsNullOrEmpty(tagEntra) ? null : (await pecas.BuscarTag(tagEntra))?.IdDormente;
            if (idEntra is not int idEntrou)
                throw Erro("a peça que entra não é uma das separadas para o lote.");
            var doAvulso = obrigatorias.Select(o => o.Troca).FirstOrDefault(t => t.Status == StatusTroca.Separada && t.IdDormenteEntrou == idEntrou);
            int? pilhaDeOrigem, idSeparou;
            DateTime? dataSeparacao;
            if (doAvulso is not null)
            {
                if (doAvulso.IdDormenteSaiu != idSaiu)
                    throw Erro($"esta peça foi separada para a troca da peça {faixa[doAvulso.IdDormenteSaiu].Codigo} (km {Km(faixa[doAvulso.IdDormenteSaiu].KmProjeto)}) e só entra no lugar dela.");
                (pilhaDeOrigem, idSeparou, dataSeparacao) = (await trocas.PilhaDaSeparacao(doAvulso.IdTroca, idEntrou), doAvulso.IdUsuarioSeparou, doAvulso.DataSeparacao);
            }
            else if (separadas.TryGetValue(idEntrou, out var separacao))
                (pilhaDeOrigem, idSeparou, dataSeparacao) = (separacao.IdPilhaOrigem, separacao.IdUsuario, separacao.Data);
            else
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
                IdUsuarioSeparou = idSeparou, DataSeparacao = dataSeparacao, IdUsuarioFez = idUsuario, Data = hora, RecebidoEm = agora,
                Motivo = lote.Motivo, Equipe = dto.Equipe.Trim(), FotoArquivo = par.FotoArquivo.Trim(), TagEntrou = tagAtiva.TagRfid, Km = km,
                NoiteDoPlano = lote.NoiteDoPlano, Urgente = lote.Urgente
            };
            mudancas.TrocasFeitas.Add((troca, new Movimentacao
            {
                IdDormente = idEntrou, Tipo = TipoMovimentacao.Saida, IdPilhaOrigem = pilhaDeOrigem, IdUsuario = idUsuario,
                IdLeitor = par.LeituraEntrou!.IdLeitor, Data = hora, RegistradoEm = hora, RecebidoEm = agora
            }, leituraEntrou));
            // A nova herda o trecho e o km; a velha ganha retirada_em; os alertas da velha fecham.
            mudancas.Pecas.Add((idEntrou, d => { d.IdTrecho = saiu.IdTrecho ?? lote.IdTrecho; d.KmProjeto = km; d.IdPilha = null; }));
            mudancas.Pecas.Add((idSaiu, d => d.RetiradaEm = hora));
            mudancas.FecharAlertas.Add((idSaiu, idUsuario, hora));
            foreach (var avulsa in abertas.Where(t => t.IdDormenteSaiu == idSaiu))
            {
                var fechada = await avulsas.FechadaPeloLote(avulsa, Codigo(idLote), idUsuario, agora, pecaEntrouNoLote: avulsa == doAvulso);
                mudancas.AvulsasAlteradas.Add(avulsa);
                mudancas.Movimentacoes.AddRange(fechada.Movimentacoes);
                mudancas.Pecas.AddRange(fechada.Pecas);
            }
        }
        // As obrigatórias que não deu para trocar voltam para o técnico como recusa; as que faltam, sem o "não deu", travam o lote.
        var naoDeu = new HashSet<int>();
        foreach (var nd in dto.NaoDeu)
        {
            var avulsa = obrigatorias.Select(o => o.Troca).FirstOrDefault(t => t.IdTroca == nd.IdTroca)
                ?? throw new RegraDeNegocioException("O \"não deu\" é só para as trocas obrigatórias do lote (as já aprovadas na faixa).");
            var peca = faixa[avulsa.IdDormenteSaiu].Codigo;
            if (sairam.Contains(avulsa.IdDormenteSaiu))
                throw new RegraDeNegocioException($"A peça {peca} foi trocada no lote: tire o \"não deu\" dela.");
            if (!naoDeu.Add(avulsa.IdTroca))
                throw new RegraDeNegocioException($"O \"não deu\" da peça {peca} veio duas vezes.");
            if (string.IsNullOrWhiteSpace(nd.Motivo) || string.IsNullOrWhiteSpace(nd.FotoArquivo))
                throw new RegraDeNegocioException($"O \"não deu\" da peça {peca} exige motivo e foto.");
            var (doNaoDeu, recusa) = await avulsas.NaoDeuNoLote(avulsa, nd, Codigo(idLote), idUsuario, agora);
            mudancas.AvulsasAlteradas.Add(avulsa);
            mudancas.Movimentacoes.AddRange(doNaoDeu.Movimentacoes);
            mudancas.Pecas.AddRange(doNaoDeu.Pecas);
            mudancas.RecusasDeAvulsas.Add(recusa);
        }
        var faltam = obrigatorias.Where(o => !sairam.Contains(o.Peca.Id) && !naoDeu.Contains(o.Troca.IdTroca)).ToList();
        if (faltam.Count > 0)
            throw new RegraDeNegocioException($"Falta{(faltam.Count > 1 ? "m" : "")} {string.Join(", ", faltam.Select(o => $"a peça {o.Peca.Codigo} (km {Km(o.Peca.KmProjeto)})"))}, "
                + "com troca já aprovada: troque no lote ou diga que não deu.");

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

    /// <summary>A via não conseguiu trocar nada (por exemplo, a interdição caiu): recusa o lote com motivo e foto, como a recusa da troca. As peças continuam separadas.</summary>
    public async Task<LoteTrocaDto?> Recusar(int idLote, RecusarTrocaDto dto, int idUsuario)
    {
        var lote = await Buscar(idLote);
        if (lote is null)
            return null;
        if (dto.IdCliente is Guid idCliente && (await lotes.Recusas(idLote)).Any(r => r.IdCliente == idCliente))
            return await BuscarPorId(idLote);
        if (lote.Status != StatusLoteTroca.Separado)
            throw new RegraDeNegocioException("Só o lote com as peças separadas é recusado.");
        if (string.IsNullOrWhiteSpace(dto.Motivo) || string.IsNullOrWhiteSpace(dto.FotoArquivo))
            throw new RegraDeNegocioException("A recusa exige motivo e foto.");

        var agora = DateTime.Now;
        var noCampo = HoraDoCampo.De(dto.RegistradoEm, agora);
        lote.Status = StatusLoteTroca.Recusado;
        await lotes.Gravar(new MudancasDoLote
        {
            LoteAlterado = lote,
            RecusaNova = new RecusaTroca
            {
                IdUsuario = idUsuario, Data = noCampo, Motivo = dto.Motivo.Trim(), Descricao = Limpo(dto.Descricao), FotoArquivo = dto.FotoArquivo.Trim(),
                IdCliente = dto.IdCliente, RegistradoEm = noCampo, RecebidoEm = agora
            }
        });
        return await BuscarPorId(idLote);
    }

    /// <summary>
    /// O técnico responde à recusa do lote: mantém (com resposta; o lote volta para separado, com as mesmas peças) ou aceita (o lote é cancelado
    /// e as separadas voltam para a pilha). O lote no plano desta noite, depois do corte, não é cancelado até a manhã.
    /// </summary>
    public async Task<LoteTrocaDto?> DecidirRecusa(int idLote, DecidirRecusaDto dto, int idUsuario)
    {
        var lote = await Buscar(idLote);
        if (lote is null)
            return null;
        if (lote.Status != StatusLoteTroca.Recusado)
            throw new RegraDeNegocioException("O lote não está recusado.");
        if (dto.Decisao == DecisaoRecusa.Manter && string.IsNullOrWhiteSpace(dto.Resposta))
            throw new RegraDeNegocioException("Para manter o lote, responda à recusa.");
        if (dto.Decisao != DecisaoRecusa.Manter && PlanoDaNoite.Travada(lote.NoiteDoPlano, DateTime.Now, await Corte()))
            throw new RegraDeNegocioException("Este lote está no plano desta noite; ele pode ser cancelado amanhã.");

        var agora = DateTime.Now;
        var recusa = (await lotes.Recusas(idLote)).Last();
        recusa.Decisao = dto.Decisao;
        recusa.IdUsuarioDecisao = idUsuario;
        recusa.DataDecisao = agora;
        recusa.Resposta = Limpo(dto.Resposta);
        var mudancas = new MudancasDoLote { LoteAlterado = lote, RecusaAlterada = recusa };
        if (dto.Decisao == DecisaoRecusa.Manter)
            lote.Status = StatusLoteTroca.Separado;
        else
        {
            lote.Status = StatusLoteTroca.Cancelado;
            lote.Cancelamento = $"Recusa aceita pelo técnico: {recusa.Motivo}";
            foreach (var separada in Separadas(await lotes.Movimentacoes(idLote)))
                Devolver(mudancas, separada, idUsuario, agora);
        }
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
        IReadOnlyList<Troca>? avulsasAbertas = null; // uma vez para a lista toda
        foreach (var l in lista)
        {
            var ultimas = Ultimas(await lotes.Movimentacoes(l.IdLoteTroca));
            var porPeca = await consulta.PorIds(ultimas.Select(m => m.IdDormente).ToList());
            IReadOnlyList<PecaDoLoteDto> Na(TipoMovimentacao tipo)
                => ultimas.Where(m => m.Tipo == tipo && porPeca.ContainsKey(m.IdDormente)).Select(m => DoLote(porPeca[m.IdDormente])).ToList();
            // A faixa: no lote aberto (para o celular conferir sem rede) e nos lotes em andamento (para as obrigatórias).
            var faixaToda = aberto || Abertos.Contains(l.Status) ? await DaFaixa(l) : [];
            var faixa = aberto ? faixaToda : [];
            var naFaixa = faixa.Select(p => p.Id).ToHashSet();
            var comAvulso = aberto
                ? (await trocas.Listar()).Where(t => t.IdLoteTroca is null && TrocaService.Aberta(t) && naFaixa.Contains(t.IdDormenteSaiu))
                    .Select(t => DoLote(faixa.First(p => p.Id == t.IdDormenteSaiu))).ToList()
                : [];
            IReadOnlyList<ObrigatoriaDto> obrigatorias = l.Status == StatusLoteTroca.Feito ? (aberto ? await DoHistorico(l) : [])
                : Abertos.Contains(l.Status) ? await ParaTela(AvulsasObrigatorias(l, faixaToda, avulsasAbertas ??= await AvulsasAbertas())) : [];
            var r = (await lotes.Recusas(l.IdLoteTroca)).LastOrDefault();
            resultado.Add(new LoteTrocaDto(l.IdLoteTroca, Codigo(l.IdLoteTroca), l.Status.ToString(), l.IdTrecho, nomes.GetValueOrDefault(l.IdTrecho, $"trecho {l.IdTrecho}"),
                (await trocas.BuscarTrecho(l.IdTrecho))?.ClasseExigida ?? 1, l.KmInicio, l.KmFim, l.IdTamanho, comprimentos.GetValueOrDefault(l.IdTamanho),
                l.QuantidadePedida, l.QuantidadeAprovada, l.Motivo, l.Observacao, l.IdUsuarioPediu, l.DataPedido, l.IdUsuarioAprovou, l.DataAprovacao,
                l.MotivoReprovacao, l.MotivoMenos, l.NoiteDoPlano, l.Urgente, l.IdUsuarioFez, l.Data, l.Equipe, l.Cancelamento,
                Na(TipoMovimentacao.Separada), Na(TipoMovimentacao.Devolvida), await avulsas.DoLote(l.IdLoteTroca),
                faixa.Select(DoLote).ToList(), comAvulso,
                r is null ? null : new RecusaDto(r.IdRecusa, r.Data, r.IdUsuario, r.Motivo, r.Descricao, r.FotoArquivo,
                    r.Decisao?.ToString(), r.IdUsuarioDecisao, r.DataDecisao, r.Resposta), obrigatorias));
        }
        return resultado;
    }

    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static string Km(decimal? km) => km?.ToString("0.000", CultureInfo.GetCultureInfo("pt-BR")) ?? "—";

    /// <summary>Para o técnico ver ao pedir o lote: as trocas avulsas já aprovadas na faixa, que entram nele como obrigatórias.</summary>
    public async Task<IReadOnlyList<ObrigatoriaDto>> Obrigatorias(int idTrecho, decimal kmInicio, decimal kmFim, int idTamanho)
    {
        var lote = new LoteTroca { IdTrecho = idTrecho, KmInicio = kmInicio, KmFim = kmFim, IdTamanho = idTamanho };
        return await ParaTela(AvulsasObrigatorias(lote, await DaFaixa(lote), await AvulsasAbertas()));
    }

    private async Task<IReadOnlyList<Troca>> AvulsasAbertas() => (await trocas.Listar()).Where(t => t.IdLoteTroca is null && TrocaService.Aberta(t)).ToList();

    /// <summary>
    /// As obrigatórias do lote (Victor, 2026-11-12): as trocas avulsas já aprovadas (aprovada, aguardando peça ou separada) das peças na via,
    /// dentro da faixa e com o comprimento do lote, na ordem do km. A pedida ainda sem aprovação não entra; a de outro comprimento fica avulsa.
    /// </summary>
    private static IReadOnlyList<(Troca Troca, PecaDto Peca)> AvulsasObrigatorias(LoteTroca lote, IReadOnlyList<PecaDto> faixa, IEnumerable<Troca> abertas)
    {
        var doComprimento = faixa.Where(p => p.IdTamanho == lote.IdTamanho).ToDictionary(p => p.Id);
        return abertas.Where(t => AvulsasAprovadas.Contains(t.Status) && doComprimento.ContainsKey(t.IdDormenteSaiu))
            .Select(t => (t, doComprimento[t.IdDormenteSaiu])).OrderBy(o => o.Item2.KmProjeto).ToList();
    }

    private async Task<IReadOnlyList<ObrigatoriaDto>> ParaTela(IReadOnlyList<(Troca Troca, PecaDto Peca)> obrigatorias)
    {
        var separadas = await consulta.PorIds(obrigatorias.Where(o => o.Troca.Status == StatusTroca.Separada && o.Troca.IdDormenteEntrou is not null)
            .Select(o => o.Troca.IdDormenteEntrou!.Value).ToList());
        return obrigatorias.Select(o => new ObrigatoriaDto(o.Troca.IdTroca, "Aberta", DoLote(o.Peca),
            o.Troca.Status == StatusTroca.Separada && o.Troca.IdDormenteEntrou is int e && separadas.TryGetValue(e, out var s) ? DoLote(s) : null, null)).ToList();
    }

    /// <summary>O lote feito: os pedidos avulsos que ele fechou (trocados) e os que não deu (a recusa diz "Não deu no lote LT-N").</summary>
    private async Task<IReadOnlyList<ObrigatoriaDto>> DoHistorico(LoteTroca lote)
    {
        var codigo = Codigo(lote.IdLoteTroca);
        var doTrecho = (await trocas.Listar()).Where(t => t.IdTrecho == lote.IdTrecho && t.IdLoteTroca is null).ToList();
        var trocadas = doTrecho.Where(t => t.Cancelamento == $"Feita no lote {codigo}").Select(t => (Troca: t, Situacao: "Trocada", Motivo: (string?)null));
        var naoDeu = (await trocas.Recusas(doTrecho.Select(t => t.IdTroca).ToList()))
            .Where(r => r.Descricao == $"Não deu no lote {codigo}" || r.Descricao?.StartsWith($"Não deu no lote {codigo}:") == true)
            .Select(r => (Troca: doTrecho.First(t => t.IdTroca == r.IdTroca), Situacao: "NaoDeu", Motivo: (string?)r.Motivo));
        var lista = trocadas.Concat(naoDeu).ToList();
        var porPeca = await consulta.PorIds(lista.Select(o => o.Troca.IdDormenteSaiu).Distinct().ToList());
        return lista.Where(o => porPeca.ContainsKey(o.Troca.IdDormenteSaiu))
            .Select(o => new ObrigatoriaDto(o.Troca.IdTroca, o.Situacao, DoLote(porPeca[o.Troca.IdDormenteSaiu]), null, o.Motivo))
            .OrderBy(o => o.Peca.KmProjeto).ToList();
    }
}
