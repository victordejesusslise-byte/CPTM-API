using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Apresentacao.Dtos;
using ViaPermanente.Dominio.Apresentacao.Models;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Apresentacao;

/// <summary>
/// Mostra o caminho da peça, etapa por etapa, com o que o banco tem em cada uma.
/// O exemplo cria um pedido que passa pelas cinco etapas, para apresentar o sistema.
/// </summary>
public class ApresentacaoService(IApresentacaoRepository repo)
{
    public const int PecasPorFeixe = 14;
    private const string NomeFornecedor = "TG–Três Guri";
    private const string NumeroContrato = "AR00224-03";
    private static readonly string[] EspeciesDoExemplo = ["Ocotea neesiana", "Hymenaea courbaril"];

    /// <summary>As etapas com as contagens. Com idPedido de um pedido que não existe, devolve nulo.</summary>
    public async Task<IReadOnlyList<EtapaFluxoDto>?> Fluxo(int? idPedido)
    {
        var contagem = await repo.Contar(idPedido);
        return idPedido is not null && contagem.Pedidos == 0 ? null : Etapas(contagem);
    }

    public static IReadOnlyList<EtapaFluxoDto> Etapas(ContagemFluxo c) =>
    [
        Etapa(1, "Pedido", "Operador de pátio e supervisor", ["PEDIDO", "PEDIDO_ITEM", "NOTA_FISCAL"],
            c.Pedidos + c.ItensPedido + c.Notas,
            $"{c.Pedidos} pedido(s) com {c.PecasPedidas} peças em {c.ItensPedido} item(ns); {c.Notas} nota(s) fiscal(is)."),
        Etapa(2, "Chegada da carga", "Operador de pátio", ["RECEBIMENTO", "LOTE", "FEIXE"],
            c.Recebimentos + c.Lotes + c.Feixes,
            $"{c.Recebimentos} chegada(s), {c.Lotes} lote(s) e {c.Feixes} feixe(s) com {c.PecasNosFeixes} peças."),
        Etapa(3, "Cadastro da peça", "Operador de pátio", ["DORMENTE", "RFID", "CLASSIFICACAO_MADEIRA"],
            c.Dormentes + c.TagsAtivas + c.TagsDesativadas + c.Classificacoes,
            $"{c.Dormentes} peça(s) com tag UHF; a espécie da placa do fornecedor vira a primeira classificação."),
        Etapa(4, "Verificação de qualidade", "Especialista", ["VERIFICACAO_QUALIDADE"],
            c.Aprovadas + c.Reprovadas,
            $"{c.Aprovadas} aprovada(s) e {c.Reprovadas} reprovada(s); a reprovada tem a tag desativada ({c.TagsDesativadas}) e volta ao fornecedor."),
        Etapa(5, "Armazém", "Operador de pátio", ["MOVIMENTACAO", "PILHA"],
            c.Movimentacoes,
            $"{c.EmPilha} peça(s) guardada(s) em pilha pelo tamanho e pela classe; {c.Movimentacoes} movimentação(ões).")
    ];

    /// <summary>
    /// Cria um pedido de exemplo e o leva pelas cinco etapas: 28 peças de 2,80 m em dois feixes de 14,
    /// duas reprovadas na qualidade e as outras guardadas na pilha A01 de Santo Amaro. Tudo numa transação.
    /// </summary>
    public async Task<ExemploCriadoDto> CriarExemplo(int idUsuario)
    {
        var agora = DateTime.Now;
        var sufixo = agora.ToString("yyyyMMddHHmmss");
        Pedido pedido = null!;
        Armazem armazem = null!;
        Pilha pilha = null!;

        await repo.EmTransacao(async () =>
        {
            // Catálogo: o que o exemplo precisa já estar no banco.
            var tamanho = await repo.Primeiro<TamanhoDormente>(t => t.ComprimentoM == 2.80m)
                ?? throw new RegraDeNegocioException("Falta o tamanho de 2,80 m no catálogo.");
            var especies = new List<Especie>();
            foreach (var nome in EspeciesDoExemplo)
                especies.Add(await repo.Primeiro<Especie>(e => e.NomeCientifico == nome)
                    ?? throw new RegraDeNegocioException($"Falta a espécie {nome} no catálogo."));
            armazem = await repo.Primeiro<Armazem>(a => a.Nome == "Santo Amaro" && a.Ativo)
                ?? await repo.Primeiro<Armazem>(a => a.Ativo)
                ?? throw new RegraDeNegocioException("Não há armazém ativo.");
            var idArmazem = armazem.IdArmazem;

            // Empresas e contrato da placa de exemplo (criados uma vez, reaproveitados depois).
            var fornecedor = await EmpresaDoExemplo(NomeFornecedor, PapelEmpresa.Fornecedor);
            var tratadora = await EmpresaDoExemplo("Granoski", PapelEmpresa.Tratadora);
            var inspetora = await EmpresaDoExemplo("UFV", PapelEmpresa.Inspetora);
            var contrato = await repo.Primeiro<Contrato>(c => c.NumeroContrato == NumeroContrato)
                ?? await repo.Adicionar(new Contrato { IdEmpresa = fornecedor.IdEmpresa, NumeroAta = "AR00224", NumeroContrato = NumeroContrato });

            // 1. Pedido e nota fiscal.
            var total = PecasPorFeixe * especies.Count;
            pedido = await repo.Adicionar(new Pedido
            {
                IdEmpresa = fornecedor.IdEmpresa, IdUsuario = idUsuario, IdContrato = contrato.IdContrato,
                Codigo = $"EX-{sufixo}", DataCompra = DateOnly.FromDateTime(agora), Quantidade = total,
                CriadoEm = agora, Status = StatusPedido.Aberto
            });
            await repo.AdicionarVarios(new[] { new PedidoItem { IdPedido = pedido.IdPedido, IdTamanho = tamanho.IdTamanho, Classe = 1, Quantidade = total } });
            var nota = await repo.Adicionar(new NotaFiscal
            {
                IdPedido = pedido.IdPedido, Numero = $"NF-{sufixo}", DataEmissao = DateOnly.FromDateTime(agora),
                Origem = OrigemNota.Manual, StatusConferencia = StatusConferenciaNota.Confere,
                IdUsuarioConferiu = idUsuario, DataConferencia = agora
            });
            var m3PorPeca = VolumeHelper.M3(tamanho.ComprimentoM, tamanho.LarguraM, tamanho.AlturaM);
            await repo.AdicionarVarios(especies.Select(e => new NotaFiscalItem
            {
                IdNota = nota.IdNota, IdEspecie = e.IdEspecie, IdTamanho = tamanho.IdTamanho, Classe = e.Classe,
                Quantidade = PecasPorFeixe, VolumeM3 = PecasPorFeixe * m3PorPeca
            }).ToList());

            // 2. Chegada: recebimento no armazém, lote e um feixe por espécie.
            var recebimento = await repo.Adicionar(new Recebimento
            {
                IdPedido = pedido.IdPedido, IdUsuario = idUsuario, IdNota = nota.IdNota, IdArmazem = idArmazem,
                DocumentoOrigemTipo = "DOF", DocumentoOrigemNumero = $"DOF-{sufixo}",
                DataRecebimento = agora, QuantidadeRecebida = total
            });
            var lote = await repo.Adicionar(new Lote
            {
                IdEmpresa = fornecedor.IdEmpresa, IdPedido = pedido.IdPedido, Codigo = $"LOTE-{sufixo}",
                DataCriacao = agora, Descricao = "Exemplo da apresentação do fluxo"
            });
            var feixes = especies.Select((e, i) => new Feixe
            {
                IdRecebimento = recebimento.IdRecebimento, NumeroFeixe = $"{i + 1:00}", Quantidade = PecasPorFeixe,
                IdEspecie = e.IdEspecie, IdTamanho = tamanho.IdTamanho, Tratamento = "CCA",
                IdEmpresaTratamento = tratadora.IdEmpresa, IdEmpresaInspecao = inspetora.IdEmpresa, AnoInspecao = (short)agora.Year
            }).ToList();
            await repo.AdicionarVarios(feixes);

            // 3. Cadastro: a peça, a tag UHF e a espécie da placa como primeira classificação.
            var pecas = feixes.SelectMany(f => Enumerable.Range(0, f.Quantidade).Select(_ => f)).ToList();
            var dormentes = pecas.Select((f, n) => new Dormente
            {
                IdLote = lote.IdLote, IdFeixe = f.IdFeixe,
                TagFornecedor = $"TG-{sufixo}-{n + 1:00}", TagFornecedorLeitura = MetodoCaptura.Digitado
            }).ToList();
            await repo.AdicionarVarios(dormentes);

            var proxima = await repo.UltimaTag();
            var tags = new List<Rfid>();
            foreach (var d in dormentes)
            {
                proxima = TagHelper.Proxima(proxima);
                tags.Add(new Rfid { IdDormente = d.IdDormente, TagRfid = proxima, Ativo = true });
            }
            await repo.AdicionarVarios(tags);
            await repo.AdicionarVarios(dormentes.Select((d, n) => new ClassificacaoMadeira
            {
                IdDormente = d.IdDormente, IdEspecie = pecas[n].IdEspecie, IdUsuario = idUsuario, Data = agora,
                Metodo = MetodoClassificacao.PlacaFornecedor, MetodoCaptura = MetodoCaptura.Digitado,
                RegistradoEm = agora, RecebidoEm = agora
            }).ToList());

            // 4. Verificação de qualidade: duas reprovadas, uma em cada feixe.
            var verificacoes = dormentes.Select((d, n) => Verificar(d.IdDormente, n, idUsuario, agora, sufixo)).ToList();
            await repo.AdicionarVarios(verificacoes);
            foreach (var (tag, verificacao) in tags.Zip(verificacoes))
                if (verificacao.Resultado == ResultadoVerificacao.Reprovada)
                    tag.Ativo = false; // reprovada: a tag é desativada e a peça volta ao fornecedor

            // 5. Armazém: a aprovada entra na pilha do tamanho e da classe.
            pilha = await repo.Primeiro<Pilha>(p => p.IdArmazem == idArmazem && p.IdTamanho == tamanho.IdTamanho && p.Classe == 1 && p.Ativo)
                ?? throw new RegraDeNegocioException($"O armazém {armazem.Nome} não tem pilha de 2,80 m de 1ª classe.");
            var idPilha = pilha.IdPilha;
            var aprovadas = dormentes.Where((_, n) => verificacoes[n].Resultado == ResultadoVerificacao.Aprovada).ToList();
            foreach (var d in aprovadas)
            {
                d.IdPilha = idPilha;
                d.EntradaArmazemEm = agora;
            }
            await repo.AdicionarVarios(aprovadas.Select(d => new Movimentacao
            {
                IdDormente = d.IdDormente, Tipo = TipoMovimentacao.Entrada, IdPilhaDestino = idPilha,
                IdUsuario = idUsuario, Data = agora, RegistradoEm = agora, RecebidoEm = agora
            }).ToList());
            await repo.Salvar();
        });

        var etapas = Etapas(await repo.Contar(pedido.IdPedido));
        return new ExemploCriadoDto(pedido.IdPedido, pedido.Codigo, armazem.Nome, pilha.Codigo, etapas);
    }

    /// <summary>A etapa está feita quando tem pelo menos uma linha no banco.</summary>
    private static EtapaFluxoDto Etapa(int numero, string nome, string quemFaz, string[] tabelas, int linhas, string resumo)
        => new(numero, nome, quemFaz, tabelas, linhas, linhas > 0, resumo);

    private async Task<Empresa> EmpresaDoExemplo(string nome, PapelEmpresa papel)
        => await repo.Primeiro<Empresa>(e => e.Nome == nome && e.Papel == papel)
            ?? await repo.Adicionar(new Empresa { Nome = nome, Papel = papel, Ativo = true });

    /// <summary>A 6ª peça tem rachadura grave e a 20ª, ataque biológico; as outras passam.</summary>
    private static VerificacaoQualidade Verificar(int idDormente, int n, int idUsuario, DateTime agora, string sufixo)
    {
        var verificacao = new VerificacaoQualidade
        {
            IdDormente = idDormente, IdUsuario = idUsuario, Data = agora,
            AtaqueBiologico = AtaqueBiologicoVerificacao.Nenhum, Rachadura = RachaduraVerificacao.Nenhuma,
            PlacaAntirrachante = true, PlacaConfere = true,
            Resultado = ResultadoVerificacao.Aprovada, Destino = DestinoVerificacao.Armazem,
            RegistradoEm = agora, RecebidoEm = agora
        };
        if (n is not (5 or 19))
            return verificacao;

        if (n == 5) { verificacao.Rachadura = RachaduraVerificacao.Grave; verificacao.Motivo = "Rachadura grave no topo da peça."; }
        else { verificacao.AtaqueBiologico = AtaqueBiologicoVerificacao.Presente; verificacao.Motivo = "Ataque biológico (cupim)."; }
        verificacao.Resultado = ResultadoVerificacao.Reprovada;
        verificacao.Destino = DestinoVerificacao.Devolucao;
        verificacao.FotoArquivo = $"fotos/exemplo/{sufixo}-peca-{n + 1:00}.jpg";
        return verificacao;
    }
}