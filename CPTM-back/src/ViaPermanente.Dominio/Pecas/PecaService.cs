using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>Etapa 03 (operador de pátio): cadastra a peça gravando a nossa tag com o módulo do celular, e faz as consultas da peça.
/// A peça só é criada quando a gravação dá certo (decisão: Leitura tolerante, gravação rigorosa).
/// A espécie da plaquinha do feixe vira a primeira classificação; tamanho e classe vêm do feixe.</summary>
public class PecaService(IPecaRepository pecas, ICatalogoRepository catalogo, ISinaisPecaRepository? sinais = null)
{
    /// <summary>O próximo número para o módulo gravar. Não fica reservado: o cadastro confere de novo.</summary>
    public async Task<string> ProximaTag() => TagHelper.Proxima(await pecas.MaiorTag());

    /// <summary>
    /// A tag do fornecedor da próxima peça do feixe, montada pelo pedido: SIGLA-NÚMERO DO PEDIDO-SEQUÊNCIA (TG-20261013193233-28). É uma
    /// sugestão: a tela deixa conferir e mudar. Nulo: feixe não encontrado.
    /// </summary>
    public async Task<string?> ProximaTagFornecedor(int idFeixe)
        => await pecas.PedidoDoFeixe(idFeixe) is { } p ? TagHelper.DoFornecedor(p.Sigla, p.Codigo, p.IdPedido, p.Cadastradas) : null;

    /// <summary>Cadastra a peça do feixe com a tag gravada pelo módulo. Toda tentativa fica na GRAVACAO_TAG; só a ok cria a peça.</summary>
    public async Task<CadastroPecaDto> Cadastrar(NovaPecaDto nova, int idUsuario)
    {
        // O celular reenviou o mesmo cadastro (estava sem rede): devolve o que já foi feito, sem gravar de novo.
        if (nova.IdCliente is Guid idCliente && await pecas.GravacaoPorCliente(idCliente) is { } repetida)
            return await Repetida(repetida);
        // Confere o que veio: só o módulo grava a tag, e o número tem de estar no formato BR e 10 dígitos.
        if (nova.Metodo != MetodoCaptura.ModuloUhf)
            throw new RegraDeNegocioException("O cadastro crava a nossa tag UHF com o módulo do celular (a pistola agora é a NIR, que analisa a madeira).");
        if (nova.TagFornecedorLeitura is not (null or MetodoCaptura.Camera or MetodoCaptura.Nfc or MetodoCaptura.Digitado))
            throw new RegraDeNegocioException("A identificação do fornecedor é lida pela câmera, por NFC ou digitada.");
        var tag = (nova.Tag ?? "").Trim();
        if (tag.Length == 0)
            throw new RegraDeNegocioException("Informe o número da tag.");
        var modulo = nova.Metodo == MetodoCaptura.ModuloUhf;
        if (modulo && !TagHelper.Valida(tag))
            throw new RegraDeNegocioException($"O módulo grava {TagHelper.Prefixo} e 10 dígitos (ex.: {TagHelper.Numero(1)}).");

        // O feixe trava o cadastro quando todas as peças dele já estão cadastradas.
        var feixe = await pecas.BuscarFeixe(nova.IdFeixe) ?? throw new RegraDeNegocioException("Feixe não encontrado.");
        var recebimento = (await pecas.BuscarRecebimento(feixe.IdRecebimento))!;
        if (await pecas.PecasNoFeixe(feixe.IdFeixe) >= feixe.Quantidade)
            throw new RegraDeNegocioException($"O feixe {feixe.NumeroFeixe} já tem as {feixe.Quantidade} peças cadastradas.");
        // O leitor informado tem de existir e estar ativo.
        var leitores = await catalogo.ListarLeitores();
        if (nova.IdLeitor is int idLeitor && leitores.All(l => l.IdLeitor != idLeitor || !l.Ativo))
            throw new RegraDeNegocioException("Leitor não encontrado.");
        // O celular não sabe o id do próprio módulo: sem leitor, vale o único módulo RFID ativo cadastrado (Victor, 2026-10-27).
        var idLeitorUsado = nova.IdLeitor;
        if (modulo && idLeitorUsado is null)
        {
            var modulos = leitores.Where(l => l.Ativo && l.Tipo == TipoLeitor.ModuloRfid).ToList();
            if (modulos.Count == 0)
                throw new RegraDeNegocioException("Nenhum módulo do celular cadastrado: cadastre em Cadastros › Leitores de tag.");
            if (modulos.Count == 1)
                idLeitorUsado = modulos[0].IdLeitor;
        }
        if (modulo && (idLeitorUsado is null || nova.IdModo is null))
            throw new RegraDeNegocioException("Com o módulo, informe o leitor e o modo de leitura.");
        if (nova.IdModo is int idModo && (await catalogo.ListarModos()).All(m => m.IdModo != idModo))
            throw new RegraDeNegocioException("Modo de leitura não encontrado.");
        // Um número já gravado (ok ou divergente) nunca vai para outra tag.
        if (await pecas.TagEmUso(tag))
            throw new RegraDeNegocioException(modulo
                ? $"O número {tag} já foi usado: peça um número novo em GET /api/tags/proxima."
                : $"A tag {tag} já está associada a uma peça.");

        var agora = DateTime.Now;
        var registradoEm = nova.RegistradoEm ?? agora;
        // Toda tentativa fica em GRAVACAO_TAG; a ok é a etiqueta da peça: Gravada e vigente (Etiqueta na GRAVACAO_TAG, 2026-11-14).
        var resultado = GravacaoRigorosa.Resultado(nova.TagsVistas, tag, nova.TagRelida);
        var gravacao = new GravacaoTag
        {
            Tipo = TipoGravacao.Gravada, TagGravada = tag, TagRelida = nova.TagRelida?.Trim(), Resultado = resultado, TagsVistas = nova.TagsVistas,
            RssiDbm = nova.RssiDbm, IdModo = nova.IdModo, IdLeitor = idLeitorUsado, IdUsuario = idUsuario,
            Data = agora, IdCliente = nova.IdCliente, RegistradoEm = registradoEm, RecebidoEm = agora
        };
        // Gravação que não deu certo: fica registrada, mas não cria a peça.
        if (resultado != ResultadoGravacao.Ok)
        {
            await pecas.RegistrarGravacao(gravacao);
            return new CadastroPecaDto(resultado.ToString(), GravacaoRigorosa.Mensagem(resultado, tag, nova.TagRelida), null);
        }
        gravacao.Vigente = true;

        // Deu certo: grava a peça, a tag vigente e a primeira classificação (a espécie da plaquinha), tudo junto.
        var dormente = new Dormente
        {
            IdLote = (await LoteDaChegada(recebimento.IdPedido)), IdFeixe = feixe.IdFeixe,
            TagFornecedor = string.IsNullOrWhiteSpace(nova.TagFornecedor) ? null : nova.TagFornecedor.Trim(),
            TagFornecedorLeitura = nova.TagFornecedorLeitura
        };
        await pecas.Cadastrar(dormente, gravacao,
            new ClassificacaoMadeira
            {
                IdEspecie = feixe.IdEspecie, IdUsuario = idUsuario, Data = agora, Metodo = MetodoClassificacao.PlacaFornecedor,
                MetodoCaptura = nova.TagFornecedorLeitura, Observacao = "Espécie da plaquinha do feixe", RegistradoEm = registradoEm, RecebidoEm = agora
            });
        return new CadastroPecaDto(nameof(ResultadoGravacao.Ok), GravacaoRigorosa.Mensagem(ResultadoGravacao.Ok, tag, tag), await BuscarPorId(dormente.IdDormente));
    }

    /// <summary>Abre a peça pelo id. Nulo: a peça não existe.</summary>
    public async Task<PecaDto?> BuscarPorId(int idDormente) => (await Montar(await pecas.Resumos(idDormente: idDormente))).FirstOrDefault();

    /// <summary>Abre a peça pela tag (vigente ou não). Nulo: tag não associada.</summary>
    public async Task<PecaDto?> BuscarPorTag(string numero)
        => await pecas.BuscarTag(numero.Trim()) is { } tag ? await BuscarPorId(tag.IdDormente) : null;

    /// <summary>Abre a peça pelo código impresso (DM-E01-L02-0034), direto na coluna DORMENTE.codigo. Nulo quando o código não é de nenhuma peça.</summary>
    public async Task<PecaDto?> BuscarPorCodigo(string codigo)
        => await pecas.IdPeloCodigo(codigo.Trim().ToUpperInvariant()) is int id ? await BuscarPorId(id) : null;

    /// <summary>Várias peças de uma vez, pelo id (para as listas de alertas e trocas).</summary>
    public async Task<IReadOnlyDictionary<int, PecaDto>> PorIds(IReadOnlyCollection<int> ids)
        => ids.Count == 0 ? new Dictionary<int, PecaDto>() : (await Montar(await pecas.Resumos(ids: ids))).ToDictionary(p => p.Id);

    /// <summary>As peças já cadastradas de um feixe.</summary>
    public async Task<IReadOnlyList<PecaDto>> DoFeixe(int idFeixe) => await Montar(await pecas.Resumos(idFeixe: idFeixe));

    /// <summary>
    /// O estoque: peças aprovadas na verificação, com tag vigente e fora da via. A reprovada e a que ainda não
    /// foi verificada não aparecem. Novos na frente: a entrada mais recente primeiro.
    /// </summary>
    public async Task<IReadOnlyList<PecaDto>> Estoque(int? idArmazem = null, int? idTamanho = null, byte? classe = null)
        => (await Montar(await pecas.Resumos(soDisponiveis: true)))
            .Where(p => (idArmazem is null || p.IdArmazem == idArmazem) && (idTamanho is null || p.IdTamanho == idTamanho) && (classe is null || p.Classe == classe))
            .OrderByDescending(p => p.EntradaArmazemEm ?? DateTime.MinValue).ThenByDescending(p => p.Id)
            .ToList();

    /// <summary>Transforma os resumos do banco na peça como a tela mostra, com os nomes, a situação, a nota e a posição.</summary>
    internal async Task<IReadOnlyList<PecaDto>> Montar(IReadOnlyList<PecaResumo> resumos)
    {
        if (resumos.Count == 0)
            return [];
        // Busca os catálogos e os sinais de uma vez só, para não ir ao banco peça por peça.
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho);
        var armazens = (await catalogo.ListarArmazens()).ToDictionary(a => a.IdArmazem, a => a.Nome);
        var pilhas = (await catalogo.ListarPilhas()).ToDictionary(p => p.IdPilha);
        var sinaisDe = sinais is null ? new Dictionary<int, SinaisDaPeca>() : await sinais.Sinais(resumos.Select(r => r.IdDormente).ToList());

        return resumos.Select(r =>
        {
            var especie = especies[r.IdEspecieVigente];
            var pilha = r.IdPilha is int idPilha ? pilhas.GetValueOrDefault(idPilha) : null;
            // Na pilha, vale o armazém da pilha; fora dela, o armazém onde a carga chegou.
            var idArmazem = pilha?.IdArmazem ?? r.IdArmazemChegada;
            return new PecaDto(r.IdDormente, r.Codigo ?? "", r.TagVigente, r.TagFornecedor,
                r.IdPedido, r.IdLote, r.IdFeixe, r.NumeroFeixe, especie.IdEspecie, especie.NomeCientifico, r.Classe,
                r.IdTamanho, tamanhos[r.IdTamanho].ComprimentoM, idArmazem, armazens.GetValueOrDefault(idArmazem, ""),
                pilha?.IdPilha, pilha?.Codigo, r.EntradaArmazemEm, r.IdTrecho, r.KmProjeto, Situacao(r),
                NotaDaPeca.Calcular(sinaisDe.GetValueOrDefault(r.IdDormente)),
                r.RetiradaEm is null && sinaisDe.GetValueOrDefault(r.IdDormente) is { } s ? NotaDaPeca.Posicao(s.Linha, r.KmProjeto, s.NumeroNoTrecho) : null);
        }).ToList();
    }

    /// <summary>A situação da peça, calculada na ordem: retirada, na via, sem verificação, reprovada, no armazém ou aprovada
    /// (decisão: Estado derivado, não coluna guardada).</summary>
    private static string Situacao(PecaResumo r)
        => r.RetiradaEm is not null ? "Retirada"
            : r.IdTrecho is not null || r.KmProjeto is not null ? "NaVia"
            : r.Verificacao is null ? "AguardandoVerificacao"
            : r.Verificacao == ResultadoVerificacao.Reprovada ? "Reprovada"
            : r.IdPilha is not null ? "NoArmazem"
            : "Aprovada";

    /// <summary>O lote do pedido, criado na primeira chegada. Sem lote, o cadastro é recusado.</summary>
    private async Task<int> LoteDaChegada(int idPedido)
        => (await pecas.LoteDoPedido(idPedido))?.IdLote
           ?? throw new RegraDeNegocioException("A chegada deste feixe não tem lote.");

    /// <summary>A resposta para o cadastro reenviado: avisa que já tinha chegado e mostra a peça, se ela existe.</summary>
    private async Task<CadastroPecaDto> Repetida(GravacaoTag repetida)
    {
        var peca = repetida.IdDormente is int idDormente ? await BuscarPorId(idDormente) : null;
        return new CadastroPecaDto("Repetido", "Este registro já tinha chegado antes; nada foi gravado de novo.", peca);
    }
}