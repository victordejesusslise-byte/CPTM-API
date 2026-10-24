using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// Etapas 11 a 13, a troca em quatro passos: o técnico pede, quem aprova (PARAMETRO troca_quem_aprova) aprova,
/// o operador de pátio separa a peça compatível e a via troca, ou recusa. Uma troca aberta por peça.
/// Cada passo tem pessoa e data; o tempo entre eles vira indicador.
/// </summary>
public class TrocaService(ITrocaRepository trocas, IPecaRepository pecas, IInspecaoRepository inspecoes, IParametroRepository parametros,
    ICatalogoRepository catalogo, LeituraService leituras, PecaService consulta)
{
    public const string ChaveQuemAprova = "troca_quem_aprova";
    private static readonly StatusTroca[] Abertas = [StatusTroca.Pedida, StatusTroca.Aprovada, StatusTroca.AguardandoPeca, StatusTroca.Separada, StatusTroca.Recusada];

    public async Task<TrocaDto> Pedir(NovaTrocaDto dto, int idUsuario)
    {
        var peca = await consulta.BuscarPorId(dto.IdDormente) ?? throw new RegraDeNegocioException("Peça não encontrada.");
        if (peca.Situacao != "NaVia")
            throw new RegraDeNegocioException("A troca é de uma peça que está na via.");
        if ((await trocas.Listar(idDormenteSaiu: dto.IdDormente)).Any(t => Abertas.Contains(t.Status)))
            throw new RegraDeNegocioException("Esta peça já tem uma troca aberta (Uma troca aberta por peça).");
        var origem = OrigemTroca.Requisicao;
        if (dto.IdInspecao is int idInspecao)
        {
            var inspecao = (await inspecoes.Listar(idInspecao: idInspecao)).FirstOrDefault()
                ?? throw new RegraDeNegocioException("Inspeção não encontrada.");
            if (inspecao.IdDormente != dto.IdDormente || inspecao.Resultado != ResultadoInspecao.Reprovada)
                throw new RegraDeNegocioException("A troca por inspeção precisa de uma inspeção reprovada desta peça.");
            origem = OrigemTroca.Inspecao;
        }
        else if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new RegraDeNegocioException("A troca por requisição precisa de motivo.");
        var idTrecho = peca.IdTrecho ?? dto.IdTrecho;
        if (idTrecho is int t && await trocas.BuscarTrecho(t) is null)
            throw new RegraDeNegocioException("Trecho não encontrado.");

        var troca = new Troca
        {
            IdDormenteSaiu = dto.IdDormente, IdInspecao = dto.IdInspecao, IdTrecho = idTrecho, IdUsuarioPediu = idUsuario,
            Status = StatusTroca.Pedida, Origem = origem, Motivo = Limpo(dto.Motivo), Observacao = Limpo(dto.Observacao),
            DataPedido = DateTime.Now, Km = dto.Km ?? peca.KmProjeto
        };
        await trocas.Gravar(new MudancasDaTroca { TrocaNova = troca });
        return (await BuscarPorId(troca.IdTroca))!;
    }

    /// <summary>Aprova ou reprova. Quem decide é o cargo de PARAMETRO troca_quem_aprova. Aprovada sem peça compatível fica aguardando peça.</summary>
    public async Task<TrocaDto?> Decidir(int idTroca, DecisaoTrocaDto dto, Cargo cargo, int idUsuario)
    {
        var troca = await Buscar(idTroca);
        if (troca is null)
            return null;
        var quemAprova = (await parametros.Buscar(ChaveQuemAprova))?.Valor?.Trim();
        if (!string.Equals(quemAprova, cargo.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new RegraDeNegocioException($"Quem aprova a troca é o cargo {quemAprova} (parâmetro {ChaveQuemAprova}).");
        if (troca.Status != StatusTroca.Pedida)
            throw new RegraDeNegocioException("Só a troca pedida é aprovada ou reprovada.");
        if (!dto.Aprovar && string.IsNullOrWhiteSpace(dto.Motivo))
            throw new RegraDeNegocioException("Reprovar exige motivo.");

        troca.IdUsuarioAprovou = idUsuario;
        troca.DataAprovacao = DateTime.Now;
        if (dto.Aprovar)
        {
            troca.Status = (await Compativeis(troca)).Count > 0 ? StatusTroca.Aprovada : StatusTroca.AguardandoPeca;
            // O plano da noite: até o corte entra hoje; depois, na noite seguinte, salvo urgente.
            var corte = await Corte();
            troca.Urgente = dto.Urgente && TimeOnly.FromDateTime(troca.DataAprovacao.Value) > corte;
            troca.NoiteDoPlano = PlanoDaNoite.NoiteDe(troca.DataAprovacao.Value, corte, dto.Urgente);
        }
        else
        {
            troca.Status = StatusTroca.Reprovada;
            troca.MotivoReprovacao = dto.Motivo!.Trim();
        }
        await trocas.Gravar(new MudancasDaTroca { TrocaAlterada = troca });
        return await BuscarPorId(idTroca);
    }

    /// <summary>As peças que servem: do armazém mais perto da troca e, nele, a que entrou primeiro (saída do mais antigo, para a madeira não apodrecer parada). A primeira é a sugerida.</summary>
    public async Task<IReadOnlyList<CompativelDto>?> ListarCompativeis(int idTroca)
        => await Buscar(idTroca) is { } troca ? await Compativeis(troca) : null;

    public async Task<TrocaDto?> Separar(int idTroca, SepararTrocaDto dto, int idUsuario)
    {
        var troca = await Buscar(idTroca);
        if (troca is null)
            return null;
        if (troca.Status is not (StatusTroca.Aprovada or StatusTroca.AguardandoPeca))
            throw new RegraDeNegocioException("Só a troca aprovada (ou aguardando peça) é separada.");
        var compativel = (await Compativeis(troca)).FirstOrDefault(c => c.Peca.Id == dto.IdDormente)
            ?? throw new RegraDeNegocioException("Esta peça não serve na troca: precisa ter o mesmo comprimento e classe igual ou melhor que a do trecho, e estar no estoque.");
        var tag = await pecas.TagAtiva(dto.IdDormente) ?? throw new RegraDeNegocioException("A peça não tem tag ativa.");
        var leitura = await leituras.ConferirPeca(dto.IdDormente, tag, dto.Leitura, idUsuario);

        var agora = DateTime.Now;
        troca.IdDormenteEntrou = dto.IdDormente;
        troca.IdUsuarioSeparou = idUsuario;
        troca.DataSeparacao = agora;
        troca.Status = StatusTroca.Separada;
        var mudancas = new MudancasDaTroca { TrocaAlterada = troca };
        mudancas.Movimentacoes.Add((new Movimentacao
        {
            IdDormente = dto.IdDormente, Tipo = TipoMovimentacao.Separada, IdPilhaOrigem = compativel.Peca.IdPilha, IdTroca = idTroca,
            IdUsuario = idUsuario, IdLeitor = dto.Leitura.IdLeitor, Data = agora, RegistradoEm = agora, RecebidoEm = agora
        }, leitura));
        mudancas.Pecas.Add((dto.IdDormente, d => d.IdPilha = null)); // saiu da pilha, reservada para a troca
        await trocas.Gravar(mudancas);
        return await BuscarPorId(idTroca);
    }

    public async Task<TrocaDto?> Executar(int idTroca, ExecutarTrocaDto dto, int idUsuario)
    {
        var troca = await Buscar(idTroca);
        if (troca is null)
            return null;
        if (troca.Status != StatusTroca.Separada || troca.IdDormenteEntrou is not int idEntrou)
            throw new RegraDeNegocioException("Só a troca com a peça separada é feita.");
        if (string.IsNullOrWhiteSpace(dto.Equipe) || string.IsNullOrWhiteSpace(dto.FotoArquivo))
            throw new RegraDeNegocioException("Registrar a troca exige a equipe e a foto da peça nova no lugar.");
        var mudancas = new MudancasDaTroca { TrocaAlterada = troca };
        var agora = DateTime.Now;

        // A peça que sai: é a do pedido, não a vizinha. Se a tag não responde, vai pelo km e pela foto.
        if (dto.TagSaiuNaoResponde)
            mudancas.Leituras.Add(new LeituraDormente
            {
                IdDormente = troca.IdDormenteSaiu, IdUsuario = idUsuario, MetodoCaptura = MetodoCaptura.ModuloUhf, Leitura = false,
                DataLeitura = agora, RegistradoEm = agora, RecebidoEm = agora, Observacao = "Tag da peça que sai não respondeu na troca"
            });
        else
        {
            if (dto.LeituraSaiu is null)
                throw new RegraDeNegocioException("Leia a tag da peça que sai, ou marque que ela não responde.");
            var tagSaiu = await pecas.TagAtiva(troca.IdDormenteSaiu) ?? throw new RegraDeNegocioException("A peça que sai não tem tag ativa: marque que ela não responde.");
            mudancas.Leituras.Add(await leituras.ConferirPeca(troca.IdDormenteSaiu, tagSaiu, dto.LeituraSaiu, idUsuario));
        }
        // A peça que entra: é a que o pátio separou. A leitura dela fica na movimentação de saída.
        var tagEntrou = await pecas.TagAtiva(idEntrou) ?? throw new RegraDeNegocioException("A peça que entra não tem tag ativa.");
        var leituraEntrou = await leituras.ConferirPeca(idEntrou, tagEntrou, dto.LeituraEntrou, idUsuario);

        var saiu = (await consulta.BuscarPorId(troca.IdDormenteSaiu))!;
        troca.Status = StatusTroca.Feita;
        troca.IdUsuarioFez = idUsuario;
        troca.Data = agora;
        troca.Equipe = dto.Equipe.Trim();
        troca.FotoArquivo = dto.FotoArquivo.Trim();
        troca.TagEntrou = tagEntrou.TagRfid;
        troca.Km = dto.Km ?? troca.Km;
        mudancas.Movimentacoes.Add((new Movimentacao
        {
            IdDormente = idEntrou, Tipo = TipoMovimentacao.Saida, IdPilhaOrigem = await trocas.PilhaDaSeparacao(idTroca, idEntrou),
            IdTroca = idTroca, IdUsuario = idUsuario, IdLeitor = dto.LeituraEntrou.IdLeitor, Data = agora, RegistradoEm = agora, RecebidoEm = agora
        }, leituraEntrou));
        // A nova herda o km e o trecho; a velha ganha retirada_em.
        mudancas.Pecas.Add((idEntrou, d => { d.IdTrecho = saiu.IdTrecho ?? troca.IdTrecho; d.KmProjeto = troca.Km ?? saiu.KmProjeto; d.IdPilha = null; }));
        mudancas.Pecas.Add((troca.IdDormenteSaiu, d => d.RetiradaEm = agora));
        await trocas.Gravar(new MudancasDaTroca
        {
            TrocaAlterada = mudancas.TrocaAlterada, FecharAlertas = (troca.IdDormenteSaiu, idUsuario, agora)
        }.Com(mudancas));
        return await BuscarPorId(idTroca);
    }

    /// <summary>A via recusa: motivo, o que viu e foto. Cada volta é uma linha; a peça separada volta ao armazém.</summary>
    public async Task<TrocaDto?> Recusar(int idTroca, RecusarTrocaDto dto, int idUsuario)
    {
        var troca = await Buscar(idTroca);
        if (troca is null)
            return null;
        if (troca.Status != StatusTroca.Separada || troca.IdDormenteEntrou is not int idEntrou)
            throw new RegraDeNegocioException("Só a troca com a peça separada é recusada.");
        if (string.IsNullOrWhiteSpace(dto.Motivo) || string.IsNullOrWhiteSpace(dto.FotoArquivo))
            throw new RegraDeNegocioException("A recusa exige motivo e foto.");

        var agora = DateTime.Now;
        var mudancas = Devolver(troca, idEntrou, idUsuario, agora, await trocas.PilhaDaSeparacao(idTroca, idEntrou));
        troca.Status = StatusTroca.Recusada;
        await trocas.Gravar(new MudancasDaTroca
        {
            TrocaAlterada = troca,
            RecusaNova = new RecusaTroca
            {
                IdTroca = idTroca, IdUsuario = idUsuario, Data = agora, Motivo = dto.Motivo.Trim(), Descricao = Limpo(dto.Descricao),
                FotoArquivo = dto.FotoArquivo.Trim(), RegistradoEm = agora, RecebidoEm = agora
            }
        }.Com(mudancas));
        return await BuscarPorId(idTroca);
    }

    /// <summary>O técnico responde à recusa: mantém (com resposta; a troca volta para aprovada) ou aceita (a troca é cancelada).</summary>
    public async Task<TrocaDto?> DecidirRecusa(int idTroca, DecidirRecusaDto dto, int idUsuario)
    {
        var troca = await Buscar(idTroca);
        if (troca is null)
            return null;
        if (troca.Status != StatusTroca.Recusada)
            throw new RegraDeNegocioException("A troca não está recusada.");
        if (dto.Decisao == DecisaoRecusa.Manter && string.IsNullOrWhiteSpace(dto.Resposta))
            throw new RegraDeNegocioException("Para manter a troca, responda à recusa.");
        if (dto.Decisao != DecisaoRecusa.Manter && PlanoDaNoite.Travada(troca.NoiteDoPlano, DateTime.Now, await Corte()))
            throw new RegraDeNegocioException(MensagemTravada);
        var recusa = (await trocas.Recusas([idTroca])).Last();
        recusa.Decisao = dto.Decisao;
        recusa.IdUsuarioDecisao = idUsuario;
        recusa.DataDecisao = DateTime.Now;
        recusa.Resposta = Limpo(dto.Resposta);
        if (dto.Decisao == DecisaoRecusa.Manter)
            troca.Status = StatusTroca.Aprovada;
        else
        {
            troca.Status = StatusTroca.Cancelada;
            troca.Cancelamento = $"Recusa aceita pelo técnico: {recusa.Motivo}";
        }
        await trocas.Gravar(new MudancasDaTroca { TrocaAlterada = troca, RecusaAlterada = recusa });
        return await BuscarPorId(idTroca);
    }

    public async Task<IReadOnlyList<TrocaDto>> Listar(StatusTroca? status) => await Montar(await trocas.Listar(status));

    public async Task<TrocaDto?> BuscarPorId(int idTroca) => (await Montar(await trocas.Listar(idTroca: idTroca))).FirstOrDefault();

    /// <summary>Usado pela correção da inspeção: cancela a troca aberta e devolve a peça separada, se houver.</summary>
    internal async Task<MudancasDaTroca?> Cancelamento(int idInspecao, int idUsuario, string motivo)
    {
        var troca = (await trocas.Listar()).FirstOrDefault(t => t.IdInspecao == idInspecao && Abertas.Contains(t.Status));
        if (troca is null)
            return null;
        if (PlanoDaNoite.Travada(troca.NoiteDoPlano, DateTime.Now, await Corte()))
            throw new RegraDeNegocioException(MensagemTravada);
        var mudancas = troca.IdDormenteEntrou is int idEntrou && troca.Status == StatusTroca.Separada
            ? Devolver(troca, idEntrou, idUsuario, DateTime.Now, await trocas.PilhaDaSeparacao(troca.IdTroca, idEntrou))
            : new MudancasDaTroca();
        troca.Status = StatusTroca.Cancelada;
        troca.Cancelamento = motivo;
        return new MudancasDaTroca { TrocaAlterada = troca }.Com(mudancas);
    }

    // ---------- apoio ----------

    private const string MensagemTravada = "Esta peça tem troca no plano desta noite; a correção pode ser feita amanhã.";

    /// <summary>O horário de corte do plano da noite (parâmetro troca_plano_horario).</summary>
    private async Task<TimeOnly> Corte() => PlanoDaNoite.Corte((await parametros.Buscar(PlanoDaNoite.ChaveHorario))?.Valor);

    private static MudancasDaTroca Devolver(Troca troca, int idEntrou, int idUsuario, DateTime agora, int? idPilha)
    {
        var mudancas = new MudancasDaTroca();
        mudancas.Movimentacoes.Add((new Movimentacao
        {
            IdDormente = idEntrou, Tipo = TipoMovimentacao.Devolvida, IdPilhaDestino = idPilha, IdTroca = troca.IdTroca,
            IdUsuario = idUsuario, Data = agora, RegistradoEm = agora, RecebidoEm = agora
        }, null));
        mudancas.Pecas.Add((idEntrou, d => d.IdPilha = idPilha));
        troca.IdDormenteEntrou = null;
        return mudancas;
    }

    private async Task<Troca?> Buscar(int idTroca) => (await trocas.Listar(idTroca: idTroca)).FirstOrDefault();

    private async Task<IReadOnlyList<CompativelDto>> Compativeis(Troca troca)
    {
        var saiu = (await consulta.BuscarPorId(troca.IdDormenteSaiu))!;
        var classeExigida = troca.IdTrecho is int idTrecho && await trocas.BuscarTrecho(idTrecho) is { } trecho ? trecho.ClasseExigida : saiu.Classe;
        var reservadas = await trocas.PecasReservadas();
        var armazens = (await catalogo.ListarArmazens()).ToDictionary(a => a.IdArmazem, a => a.Km);
        var km = troca.Km ?? saiu.KmProjeto;
        var hoje = DateTime.Now;

        var lista = (await consulta.Estoque())
            .Where(p => p.IdPilha is not null && !reservadas.Contains(p.Id) && p.Id != saiu.Id
                        && CompatibilidadeTroca.Serve(p.IdTamanho, p.Classe, saiu.IdTamanho, classeExigida))
            .Select(p => (Peca: p, Distancia: km is decimal k && armazens.GetValueOrDefault(p.IdArmazem) is decimal ka ? Math.Abs(ka - k) : (decimal?)null))
            .OrderBy(x => x.Distancia ?? decimal.MaxValue).ThenBy(x => x.Peca.EntradaArmazemEm ?? DateTime.MaxValue).ThenBy(x => x.Peca.Id)
            .ToList();
        return lista.Select((x, i) => new CompativelDto(x.Peca, x.Distancia,
            x.Peca.EntradaArmazemEm is DateTime e ? (int)(hoje - e).TotalDays : 0, i == 0)).ToList();
    }

    private async Task<IReadOnlyList<TrocaDto>> Montar(IReadOnlyList<Troca> lista)
    {
        if (lista.Count == 0)
            return [];
        var ids = lista.SelectMany(t => new[] { t.IdDormenteSaiu, t.IdDormenteEntrou ?? 0 }).Where(i => i > 0).Distinct().ToList();
        var porPeca = await consulta.PorIds(ids);
        var recusas = (await trocas.Recusas(lista.Select(t => t.IdTroca).ToList())).GroupBy(r => r.IdTroca).ToDictionary(g => g.Key, g => g.Last());
        var agora = DateTime.Now;
        return lista.Select(t =>
        {
            var saiu = porPeca.GetValueOrDefault(t.IdDormenteSaiu);
            var entrou = t.IdDormenteEntrou is int e ? porPeca.GetValueOrDefault(e) : null;
            var r = recusas.GetValueOrDefault(t.IdTroca);
            return new TrocaDto(t.IdTroca, t.Status.ToString(), t.Origem.ToString(), t.IdDormenteSaiu, saiu?.Codigo, saiu?.Tag,
                t.IdDormenteEntrou, entrou?.Codigo, entrou?.Tag ?? t.TagEntrou, t.IdInspecao, t.IdTrecho, t.Km, t.Motivo, t.Observacao,
                t.IdUsuarioPediu, t.DataPedido, t.IdUsuarioAprovou, t.DataAprovacao, t.MotivoReprovacao,
                t.IdUsuarioSeparou, t.DataSeparacao, t.IdUsuarioFez, t.Data, t.Equipe, t.FotoArquivo, t.Cancelamento,
                (int)((t.Data ?? agora) - t.DataPedido).TotalDays,
                r is null ? null : new RecusaDto(r.IdRecusa, r.Data, r.IdUsuario, r.Motivo, r.Descricao, r.FotoArquivo,
                    r.Decisao?.ToString(), r.IdUsuarioDecisao, r.DataDecisao, r.Resposta), t.NoiteDoPlano, t.Urgente);
        }).ToList();
    }

    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}