using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Apresentacao.Dtos;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Apresentacao;

/// <summary>
/// O banco de demonstração, no banco de trabalho: 1 fornecedor, 3 pedidos (recebido, recebido em parte, aberto),
/// 3 peças na via com 3 alertas (aberto, confirmado, descartado) e 3 trocas (pedida, aprovada, separada).
/// Tudo leva a marca DEMO- e a limpeza apaga só isso, junto com o que alguém fizer em cima (vistoria, recusa, inspeção…).
/// </summary>
public class DemonstracaoService(IApresentacaoRepository repo)
{
    public const string Marca = "DEMO-";
    private const string EstacaoDemo = "DEMO ";
    private const int PecasPorFeixe = 4;

    public async Task<DemonstracaoDto> Criar(int idUsuario)
    {
        if (await repo.Primeiro<Pedido>(p => p.Codigo.StartsWith(Marca)) is not null)
            throw new RegraDeNegocioException("A demonstração já está no banco. Apague antes de criar de novo.");

        var agora = DateTime.Now;
        var ontem = agora.AddDays(-1).Date.AddHours(2);
        var pecas = 0;

        await repo.EmTransacao(async () =>
        {
            var tamanho = await repo.Primeiro<TamanhoDormente>(t => t.ComprimentoM == 2.80m)
                ?? throw new RegraDeNegocioException("Falta o tamanho de 2,80 m no catálogo.");
            var especies = new List<Especie>();
            foreach (var nome in new[] { "Ocotea neesiana", "Hymenaea courbaril" })
                especies.Add(await repo.Primeiro<Especie>(e => e.NomeCientifico == nome)
                    ?? throw new RegraDeNegocioException($"Falta a espécie {nome} no catálogo."));
            var armazem = await repo.Primeiro<Armazem>(a => a.Nome == "Santo Amaro" && a.Ativo) ?? await repo.Primeiro<Armazem>(a => a.Ativo)
                ?? throw new RegraDeNegocioException("Não há armazém ativo.");
            var idArmazem = armazem.IdArmazem;
            var pilha = await repo.Primeiro<Pilha>(p => p.IdArmazem == idArmazem && p.IdTamanho == tamanho.IdTamanho && p.Classe == 1 && p.Ativo)
                ?? throw new RegraDeNegocioException($"O armazém {armazem.Nome} não tem pilha de 2,80 m de 1ª classe.");

            // O fornecedor (o mesmo do exemplo da apresentação, criado uma vez só) e o contrato.
            var fornecedor = await Empresa("TG–Três Guri", PapelEmpresa.Fornecedor);
            var tratadora = await Empresa("Granoski", PapelEmpresa.Tratadora);
            var inspetora = await Empresa("UFV", PapelEmpresa.Inspetora);
            var contrato = await repo.Primeiro<Contrato>(c => c.NumeroContrato == "AR00224-03")
                ?? await repo.Adicionar(new Contrato { IdEmpresa = fornecedor.IdEmpresa, NumeroAta = "AR00224", NumeroContrato = "AR00224-03" });

            Pedido NovoPedido(int n, int quantidade, int dias) => new()
            {
                IdEmpresa = fornecedor.IdEmpresa, IdUsuario = idUsuario, IdContrato = contrato.IdContrato, Codigo = $"{Marca}PD-{n:00}",
                DataCompra = DateOnly.FromDateTime(agora.AddDays(-dias)), Quantidade = quantidade, CriadoEm = agora.AddDays(-dias), Status = StatusPedido.Aberto
            };

            // Pedido 1: chegou inteiro, cadastrado, verificado (1 reprovada) e guardado; 3 peças já foram para a via.
            var p1 = await repo.Adicionar(NovoPedido(1, PecasPorFeixe * 2, 20));
            await repo.AdicionarVarios(new[] { new PedidoItem { IdPedido = p1.IdPedido, IdTamanho = tamanho.IdTamanho, Classe = 1, Quantidade = p1.Quantidade } });
            var m3 = VolumeHelper.M3(tamanho.ComprimentoM, tamanho.LarguraM, tamanho.AlturaM);
            var nota = await repo.Adicionar(new NotaFiscal
            {
                IdPedido = p1.IdPedido, Numero = $"{Marca}NF-01", DataEmissao = DateOnly.FromDateTime(agora.AddDays(-15)), Origem = OrigemNota.Manual,
                StatusConferencia = StatusConferenciaNota.Confere, IdUsuarioConferiu = idUsuario, DataConferencia = agora.AddDays(-14)
            });
            await repo.AdicionarVarios(especies.Select(e => new NotaFiscalItem
            {
                IdNota = nota.IdNota, IdEspecie = e.IdEspecie, IdTamanho = tamanho.IdTamanho, Classe = e.Classe, Quantidade = PecasPorFeixe, VolumeM3 = PecasPorFeixe * m3
            }).ToList());
            var (dormentes1, tags1) = await Chegada(p1, nota.IdNota, idArmazem, especies, PecasPorFeixe, PecasPorFeixe, agora.AddDays(-14), 1, tamanho, tratadora, inspetora, fornecedor, idUsuario);

            var verificacoes = dormentes1.Select((d, n) => new VerificacaoQualidade
            {
                IdDormente = d.IdDormente, IdUsuario = idUsuario, Data = agora.AddDays(-13),
                AtaqueBiologico = AtaqueBiologicoVerificacao.Nenhum, Rachadura = n == 3 ? RachaduraVerificacao.Grave : RachaduraVerificacao.Nenhuma,
                PlacaAntirrachante = true, PlacaConfere = true, Motivo = n == 3 ? "Rachadura grave no topo da peça." : null,
                FotoArquivo = n == 3 ? "fotos/demonstracao/reprovada.jpg" : null,
                Resultado = n == 3 ? ResultadoVerificacao.Reprovada : ResultadoVerificacao.Aprovada,
                Destino = n == 3 ? DestinoVerificacao.Devolucao : DestinoVerificacao.Armazem, RegistradoEm = agora, RecebidoEm = agora
            }).ToList();
            await repo.AdicionarVarios(verificacoes);
            tags1[3].Ativo = false;

            var aprovadas = dormentes1.Where((_, n) => n != 3).ToList();
            foreach (var d in aprovadas) { d.IdPilha = pilha.IdPilha; d.EntradaArmazemEm = agora.AddDays(-12); }
            await repo.AdicionarVarios(aprovadas.Select(d => new Movimentacao
            {
                IdDormente = d.IdDormente, Tipo = TipoMovimentacao.Entrada, IdPilhaDestino = pilha.IdPilha, IdUsuario = idUsuario,
                Data = agora.AddDays(-12), RegistradoEm = agora, RecebidoEm = agora
            }).ToList());

            // 3 peças assentadas na via, num trecho (o primeiro que existir, ou um de demonstração).
            var trecho = await repo.Primeiro<Trecho>(t => true) ?? await TrechoDemo();
            var naVia = aprovadas.Take(3).ToList();
            for (var i = 0; i < naVia.Count; i++)
            {
                naVia[i].IdPilha = null; naVia[i].RetiradaEm = agora.AddDays(-10);
                naVia[i].IdTrecho = trecho.IdTrecho; naVia[i].KmProjeto = 213.100m + i * 0.006m;
            }
            await repo.AdicionarVarios(naVia.Select(d => new Movimentacao
            {
                IdDormente = d.IdDormente, Tipo = TipoMovimentacao.Saida, IdPilhaOrigem = pilha.IdPilha, IdUsuario = idUsuario,
                Data = agora.AddDays(-10), RegistradoEm = agora, RecebidoEm = agora
            }).ToList());

            // Pedido 2: chegou só uma parte (4 de 8) e só 2 peças ganharam a tag.
            var p2 = await repo.Adicionar(NovoPedido(2, PecasPorFeixe * 2, 6));
            await repo.AdicionarVarios(new[] { new PedidoItem { IdPedido = p2.IdPedido, IdTamanho = tamanho.IdTamanho, Classe = 1, Quantidade = p2.Quantidade } });
            var (dormentes2, _) = await Chegada(p2, null, idArmazem, especies.Take(1).ToList(), PecasPorFeixe, 2, agora.AddDays(-2), 2, tamanho, tratadora, inspetora, fornecedor, idUsuario);

            // Pedido 3: aberto, esperando a carga.
            var p3 = await repo.Adicionar(NovoPedido(3, 6, 1));
            await repo.AdicionarVarios(new[] { new PedidoItem { IdPedido = p3.IdPedido, IdTamanho = tamanho.IdTamanho, Classe = 1, Quantidade = 6 } });

            // A passagem da noite: uma peça fora de posição, uma com a tag muda, uma com suspeita de rachadura.
            var passagem = await repo.Adicionar(new Passagem
            {
                Codigo = $"{Marca}PASS-01", Veiculo = "Carro de inspeção (demonstração)", DataInicio = ontem, DataFim = ontem.AddHours(2),
                KmInicio = 212.000m, KmFim = 215.000m
            });
            var leituras = new List<LeituraVia>
            {
                new() { IdPassagem = passagem.IdPassagem, IdDormente = naVia[0].IdDormente, Leitura = true, KmMedido = naVia[0].KmProjeto, DesvioMm = 52, Data = ontem.AddMinutes(10) },
                new() { IdPassagem = passagem.IdPassagem, IdDormente = naVia[1].IdDormente, Leitura = false, KmMedido = naVia[1].KmProjeto, Data = ontem.AddMinutes(11) },
                new() { IdPassagem = passagem.IdPassagem, IdDormente = naVia[2].IdDormente, Leitura = true, KmMedido = naVia[2].KmProjeto, DesvioMm = 4, RachaduraSuspeita = true, Data = ontem.AddMinutes(12) },
            };
            await repo.AdicionarVarios(leituras);
            await repo.AdicionarVarios(new List<Alerta>
            {
                new() { Origem = OrigemAlerta.Sensor, IdLeituraVia = leituras[0].IdLeituraVia, IdDormente = naVia[0].IdDormente, Tipo = TipoAlerta.Posicao, DesvioMm = 52, Data = ontem.AddMinutes(10), Status = StatusAlerta.Aberto },
                new() { Origem = OrigemAlerta.Sensor, IdLeituraVia = leituras[1].IdLeituraVia, IdDormente = naVia[1].IdDormente, Tipo = TipoAlerta.TagMuda, Data = ontem.AddMinutes(11), Status = StatusAlerta.Confirmado, IdUsuarioDecisao = idUsuario, DataDecisao = agora.AddHours(-6) },
                new() { Origem = OrigemAlerta.Sensor, IdLeituraVia = leituras[2].IdLeituraVia, IdDormente = naVia[2].IdDormente, Tipo = TipoAlerta.Rachadura, Data = ontem.AddMinutes(12), Status = StatusAlerta.Descartado, IdUsuarioDecisao = idUsuario, DataDecisao = agora.AddHours(-5), MotivoDescarte = "A vistoria não achou rachadura." },
            });

            // As trocas: pedida, aprovada (o pátio separa) e separada (a via troca).
            var reserva = aprovadas.Skip(3).First();
            var tagReserva = tags1[dormentes1.IndexOf(reserva)].TagRfid;
            Troca NovaTroca(Dormente sai, string motivo, int dias) => new()
            {
                IdDormenteSaiu = sai.IdDormente, IdTrecho = trecho.IdTrecho, Km = sai.KmProjeto, IdUsuarioPediu = idUsuario, Origem = OrigemTroca.Requisicao,
                Motivo = motivo, DataPedido = agora.AddDays(-dias), Status = StatusTroca.Pedida
            };
            var pedida = NovaTroca(naVia[1], "Tag não responde na passagem.", 1);
            var aprovada = NovaTroca(naVia[0], "Peça fora de posição (52 mm).", 3);
            aprovada.Status = StatusTroca.Aprovada; aprovada.IdUsuarioAprovou = idUsuario; aprovada.DataAprovacao = agora.AddDays(-2);
            var separada = NovaTroca(naVia[2], "Peça antiga, com desgaste no topo.", 8);
            separada.Status = StatusTroca.Separada; separada.IdUsuarioAprovou = idUsuario; separada.DataAprovacao = agora.AddDays(-7);
            separada.IdDormenteEntrou = reserva.IdDormente; separada.TagEntrou = tagReserva; separada.IdUsuarioSeparou = idUsuario; separada.DataSeparacao = agora.AddDays(-1);
            await repo.AdicionarVarios(new List<Troca> { pedida, aprovada, separada });
            reserva.IdPilha = null; reserva.RetiradaEm = agora.AddDays(-1);
            await repo.Adicionar(new Movimentacao
            {
                IdDormente = reserva.IdDormente, Tipo = TipoMovimentacao.Saida, IdPilhaOrigem = pilha.IdPilha, IdTroca = separada.IdTroca,
                IdUsuario = idUsuario, Data = agora.AddDays(-1), RegistradoEm = agora, RecebidoEm = agora
            });
            await repo.Salvar();
            pecas = dormentes1.Count + dormentes2.Count;
        });

        return new DemonstracaoDto(3, pecas, 3, 3,
            "Demonstração criada: pedidos DEMO-PD-01 (recebido), DEMO-PD-02 (recebido em parte) e DEMO-PD-03 (aberto), 3 alertas e 3 trocas.");
    }

    /// <summary>Apaga tudo o que tem a marca DEMO- e o que foi feito em cima, dos filhos para os pais (tudo ou nada).</summary>
    public async Task<DemonstracaoDto> Apagar()
    {
        var apagados = 0;
        int pedidosApagados = 0, pecasApagadas = 0, alertasApagados = 0, trocasApagadas = 0;
        await repo.EmTransacao(async () =>
        {
            var pedidos = await repo.Ids<Pedido, int>(p => p.Codigo.StartsWith(Marca), p => p.IdPedido);
            var lotes = await repo.Ids<Lote, int>(l => pedidos.Contains(l.IdPedido), l => l.IdLote);
            var dorm = await repo.Ids<Dormente, int>(d => lotes.Contains(d.IdLote), d => d.IdDormente);
            var recebimentos = await repo.Ids<Recebimento, int>(r => pedidos.Contains(r.IdPedido), r => r.IdRecebimento);
            var notas = await repo.Ids<NotaFiscal, int>(n => pedidos.Contains(n.IdPedido), n => n.IdNota);
            var passagens = await repo.Ids<Passagem, int>(p => p.Codigo.StartsWith(Marca), p => p.IdPassagem);
            var leituras = await repo.Ids<LeituraVia, long>(l => passagens.Contains(l.IdPassagem) || (l.IdDormente != null && dorm.Contains(l.IdDormente.Value)), l => l.IdLeituraVia);
            var alertas = await repo.Ids<Alerta, int>(a => (a.IdDormente != null && dorm.Contains(a.IdDormente.Value)) || (a.IdLeituraVia != null && leituras.Contains(a.IdLeituraVia.Value)), a => a.IdAlerta);
            var inspecoes = await repo.Ids<Inspecao, int>(i => dorm.Contains(i.IdDormente) || (i.IdAlerta != null && alertas.Contains(i.IdAlerta.Value)), i => i.IdInspecao);
            var trocas = await repo.Ids<Troca, int>(t => dorm.Contains(t.IdDormenteSaiu) || (t.IdDormenteEntrou != null && dorm.Contains(t.IdDormenteEntrou.Value)) || (t.IdInspecao != null && inspecoes.Contains(t.IdInspecao.Value)), t => t.IdTroca);
            var rfids = await repo.Ids<Rfid, int>(r => dorm.Contains(r.IdDormente), r => r.IdRfid);

            apagados += await repo.Apagar<RecusaTroca>(r => trocas.Contains(r.IdTroca));
            apagados += await repo.Apagar<InspecaoCorrecao>(c => inspecoes.Contains(c.IdInspecao) || (c.IdTrocaCancelada != null && trocas.Contains(c.IdTrocaCancelada.Value)));
            apagados += await repo.Apagar<Movimentacao>(m => dorm.Contains(m.IdDormente) || (m.IdTroca != null && trocas.Contains(m.IdTroca.Value)));
            apagados += trocasApagadas = await repo.Apagar<Troca>(t => trocas.Contains(t.IdTroca));
            apagados += await repo.Apagar<Inspecao>(i => inspecoes.Contains(i.IdInspecao));
            apagados += await repo.Apagar<VistoriaAlerta>(v => alertas.Contains(v.IdAlerta));
            apagados += alertasApagados = await repo.Apagar<Alerta>(a => alertas.Contains(a.IdAlerta));
            apagados += await repo.Apagar<DeteccaoImagem>(d => passagens.Contains(d.IdPassagem) || (d.IdDormente != null && dorm.Contains(d.IdDormente.Value)));
            apagados += await repo.Apagar<LeituraVia>(l => leituras.Contains(l.IdLeituraVia));
            apagados += await repo.Apagar<EventoPosicao>(e => dorm.Contains(e.IdDormente) || passagens.Contains(e.IdPassagem));
            apagados += await repo.Apagar<PosicaoAtual>(p => dorm.Contains(p.IdDormente) || passagens.Contains(p.IdPassagem));
            apagados += await repo.Apagar<LeituraDormente>(l => l.IdDormente != null && dorm.Contains(l.IdDormente.Value));
            apagados += await repo.Apagar<GravacaoTag>(g => g.IdRfid != null && rfids.Contains(g.IdRfid.Value));
            apagados += await repo.Apagar<VerificacaoQualidade>(v => dorm.Contains(v.IdDormente));
            apagados += await repo.Apagar<ClassificacaoMadeira>(c => dorm.Contains(c.IdDormente));
            apagados += await repo.Apagar<Rfid>(r => rfids.Contains(r.IdRfid));
            apagados += pecasApagadas = await repo.Apagar<Dormente>(d => dorm.Contains(d.IdDormente));
            apagados += await repo.Apagar<Feixe>(f => recebimentos.Contains(f.IdRecebimento));
            apagados += await repo.Apagar<Lote>(l => lotes.Contains(l.IdLote));
            apagados += await repo.Apagar<Recebimento>(r => recebimentos.Contains(r.IdRecebimento));
            apagados += await repo.Apagar<NotaFiscalItem>(i => notas.Contains(i.IdNota));
            apagados += await repo.Apagar<NotaFiscal>(n => notas.Contains(n.IdNota));
            apagados += await repo.Apagar<PedidoHistorico>(h => pedidos.Contains(h.IdPedido));
            apagados += await repo.Apagar<PedidoItem>(i => pedidos.Contains(i.IdPedido));
            apagados += pedidosApagados = await repo.Apagar<Pedido>(p => pedidos.Contains(p.IdPedido));
            apagados += await repo.Apagar<Passagem>(p => passagens.Contains(p.IdPassagem));
            var estacoes = await repo.Ids<Estacao, int>(e => e.Nome.StartsWith(EstacaoDemo), e => e.IdEstacao);
            apagados += await repo.Apagar<Trecho>(t => estacoes.Contains(t.IdEstacaoInicio) || estacoes.Contains(t.IdEstacaoFim));
            apagados += await repo.Apagar<Estacao>(e => estacoes.Contains(e.IdEstacao));
        });
        return new DemonstracaoDto(pedidosApagados, pecasApagadas, alertasApagados, trocasApagadas,
            pedidosApagados == 0 ? "Não havia demonstração no banco." : $"Demonstração apagada: {apagados} linhas. O resto do banco ficou como estava.");
    }

    private async Task<Empresa> Empresa(string nome, PapelEmpresa papel)
        => await repo.Primeiro<Empresa>(e => e.Nome == nome && e.Papel == papel)
            ?? await repo.Adicionar(new Empresa { Nome = nome, Papel = papel, Ativo = true });

    private async Task<Trecho> TrechoDemo()
    {
        var inicio = await repo.Adicionar(new Estacao { Nome = $"{EstacaoDemo}Estação A", Linha = "9-Esmeralda", Km = 212.000m, Ordem = 901 });
        var fim = await repo.Adicionar(new Estacao { Nome = $"{EstacaoDemo}Estação B", Linha = "9-Esmeralda", Km = 215.000m, Ordem = 902 });
        return await repo.Adicionar(new Trecho { IdEstacaoInicio = inicio.IdEstacao, IdEstacaoFim = fim.IdEstacao, ClasseExigida = 1 });
    }

    /// <summary>A chegada com um feixe por espécie; cadastra as primeiras "cadastradas" peças de cada feixe, com tag e a espécie da placa.</summary>
    private async Task<(List<Dormente> Dormentes, List<Rfid> Tags)> Chegada(Pedido pedido, int? idNota, int idArmazem, List<Especie> especies, int porFeixe,
        int cadastradas, DateTime quando, int n, TamanhoDormente tamanho, Empresa tratadora, Empresa inspetora, Empresa fornecedor, int idUsuario)
    {
        var recebimento = await repo.Adicionar(new Recebimento
        {
            IdPedido = pedido.IdPedido, IdUsuario = idUsuario, IdNota = idNota, IdArmazem = idArmazem, DocumentoOrigemTipo = "DOF",
            DocumentoOrigemNumero = $"{Marca}DOF-{n:00}", DataRecebimento = quando, QuantidadeRecebida = porFeixe * especies.Count
        });
        var lote = await repo.Adicionar(new Lote
        {
            IdEmpresa = fornecedor.IdEmpresa, IdPedido = pedido.IdPedido, Codigo = $"{Marca}LOTE-{n:00}", DataCriacao = quando, Descricao = "Banco de demonstração"
        });
        var feixes = especies.Select((e, i) => new Feixe
        {
            IdRecebimento = recebimento.IdRecebimento, NumeroFeixe = $"{n}{i + 1:00}", Quantidade = porFeixe, IdEspecie = e.IdEspecie, IdTamanho = tamanho.IdTamanho,
            Tratamento = "CCA", IdEmpresaTratamento = tratadora.IdEmpresa, IdEmpresaInspecao = inspetora.IdEmpresa, AnoInspecao = (short)quando.Year
        }).ToList();
        await repo.AdicionarVarios(feixes);
        var pecas = feixes.SelectMany(f => Enumerable.Range(0, cadastradas).Select(_ => f)).ToList();
        var dormentes = pecas.Select((f, k) => new Dormente { IdLote = lote.IdLote, IdFeixe = f.IdFeixe, TagFornecedor = $"{Marca}TG-{n}-{k + 1:00}", TagFornecedorLeitura = MetodoCaptura.Digitado }).ToList();
        await repo.AdicionarVarios(dormentes);
        var proxima = await repo.UltimaTag();
        var tags = new List<Rfid>();
        foreach (var d in dormentes)
        {
            proxima = TagHelper.Proxima(proxima);
            tags.Add(new Rfid { IdDormente = d.IdDormente, TagRfid = proxima, Ativo = true });
        }
        await repo.AdicionarVarios(tags);
        await repo.AdicionarVarios(dormentes.Select((d, k) => new ClassificacaoMadeira
        {
            IdDormente = d.IdDormente, IdEspecie = pecas[k].IdEspecie, IdUsuario = idUsuario, Data = quando, Metodo = MetodoClassificacao.PlacaFornecedor,
            MetodoCaptura = MetodoCaptura.Digitado, RegistradoEm = quando, RecebidoEm = quando
        }).ToList());
        return (dormentes, tags);
    }
}
