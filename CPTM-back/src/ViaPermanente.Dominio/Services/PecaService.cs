using ViaPermanente.Dominio.Dtos.Pecas;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Models.Chegada;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// Etapa 03: o cadastro da peça, com a nossa tag UHF cravada (módulo do celular ou pistola), e as consultas da peça.
/// A espécie da plaquinha do feixe vira a primeira classificação. Espécie, tamanho e placa não são copiados: vêm do feixe.
/// </summary>
public class PecaService(IPecaRepository pecas, ICatalogoRepository catalogo)
{
    /// <summary>O próximo número para o módulo gravar. Não fica reservado: o cadastro confere de novo.</summary>
    public async Task<string> ProximaTag() => TagHelper.Proxima(await pecas.MaiorTag());

    public async Task<CadastroPecaDto> Cadastrar(NovaPecaDto nova, int idUsuario)
    {
        if (nova.IdCliente is Guid idCliente && await pecas.GravacaoPorCliente(idCliente) is { } repetida)
            return await Repetida(repetida);
        if (nova.Metodo is not (MetodoCaptura.ModuloUhf or MetodoCaptura.Pistola))
            throw new RegraDeNegocioException("O cadastro crava a nossa tag UHF: use o módulo do celular ou a pistola.");
        if (nova.TagFornecedorLeitura is not (null or MetodoCaptura.Camera or MetodoCaptura.Nfc or MetodoCaptura.Digitado))
            throw new RegraDeNegocioException("A identificação do fornecedor é lida pela câmera, por NFC ou digitada.");
        var tag = (nova.Tag ?? "").Trim();
        if (tag.Length == 0)
            throw new RegraDeNegocioException("Informe o número da tag.");
        var modulo = nova.Metodo == MetodoCaptura.ModuloUhf;
        if (modulo && !TagHelper.Valida(tag))
            throw new RegraDeNegocioException($"O módulo grava {TagHelper.Prefixo} e 10 dígitos (ex.: {TagHelper.Numero(1)}).");

        var feixe = await pecas.BuscarFeixe(nova.IdFeixe) ?? throw new RegraDeNegocioException("Feixe não encontrado.");
        var recebimento = (await pecas.BuscarRecebimento(feixe.IdRecebimento))!;
        if (await pecas.PecasNoFeixe(feixe.IdFeixe) >= feixe.Quantidade)
            throw new RegraDeNegocioException($"O feixe {feixe.NumeroFeixe} já tem as {feixe.Quantidade} peças cadastradas.");
        var leitores = await catalogo.ListarLeitores();
        if (nova.IdLeitor is int idLeitor && leitores.All(l => l.IdLeitor != idLeitor || !l.Ativo))
            throw new RegraDeNegocioException("Leitor não encontrado.");
        if (modulo && (nova.IdLeitor is null || nova.IdModo is null))
            throw new RegraDeNegocioException("Com o módulo, informe o leitor e o modo de leitura.");
        if (nova.IdModo is int idModo && (await catalogo.ListarModos()).All(m => m.IdModo != idModo))
            throw new RegraDeNegocioException("Modo de leitura não encontrado.");
        if (await pecas.TagEmUso(tag))
            throw new RegraDeNegocioException(modulo
                ? $"O número {tag} já foi usado: peça um número novo em GET /api/tags/proxima."
                : $"A tag {tag} já está associada a uma peça.");

        var agora = DateTime.Now;
        var registradoEm = nova.RegistradoEm ?? agora;
        GravacaoTag? gravacao = null;
        LeituraDormente? leitura = null;
        if (modulo)
        {
            var resultado = GravacaoRigorosa.Resultado(nova.TagsVistas, tag, nova.TagRelida);
            gravacao = new GravacaoTag
            {
                TagGravada = tag, TagRelida = nova.TagRelida?.Trim(), Resultado = resultado, TagsVistas = nova.TagsVistas,
                RssiDbm = nova.RssiDbm, IdModo = nova.IdModo!.Value, IdLeitor = nova.IdLeitor!.Value, IdUsuario = idUsuario,
                Data = agora, IdCliente = nova.IdCliente, RegistradoEm = registradoEm, RecebidoEm = agora
            };
            if (resultado != ResultadoGravacao.Ok)
            {
                await pecas.RegistrarGravacao(gravacao); // toda tentativa fica em GRAVACAO_TAG
                return new CadastroPecaDto(resultado.ToString(), GravacaoRigorosa.Mensagem(resultado, tag, nova.TagRelida), null);
            }
        }
        else
        {
            leitura = new LeituraDormente
            {
                IdLeitor = nova.IdLeitor, IdUsuario = idUsuario, MetodoCaptura = MetodoCaptura.Pistola, Leitura = true,
                DataLeitura = agora, TagLida = tag, IdModo = nova.IdModo, RssiDbm = nova.RssiDbm, TagsVistas = nova.TagsVistas,
                Ambigua = AlvoUnico.EhAmbigua(MetodoCaptura.Pistola, nova.TagsVistas),
                IdCliente = nova.IdCliente, RegistradoEm = registradoEm, RecebidoEm = agora
            };
            if (leitura.Ambigua)
            {
                await pecas.RegistrarLeitura(leitura);
                return new CadastroPecaDto(nameof(ResultadoGravacao.Bloqueada), AlvoUnico.MensagemAmbigua, null);
            }
        }

        var dormente = new Dormente
        {
            IdLote = (await LoteDaChegada(recebimento.IdPedido)), IdFeixe = feixe.IdFeixe,
            TagFornecedor = string.IsNullOrWhiteSpace(nova.TagFornecedor) ? null : nova.TagFornecedor.Trim(),
            TagFornecedorLeitura = nova.TagFornecedorLeitura
        };
        await pecas.Cadastrar(dormente, new Rfid { TagRfid = tag, Ativo = true },
            new ClassificacaoMadeira
            {
                IdEspecie = feixe.IdEspecie, IdUsuario = idUsuario, Data = agora, Metodo = MetodoClassificacao.PlacaFornecedor,
                MetodoCaptura = nova.TagFornecedorLeitura, Observacao = "Espécie da plaquinha do feixe", RegistradoEm = registradoEm, RecebidoEm = agora
            },
            gravacao, leitura);
        return new CadastroPecaDto(nameof(ResultadoGravacao.Ok), GravacaoRigorosa.Mensagem(ResultadoGravacao.Ok, tag, tag), await BuscarPorId(dormente.IdDormente));
    }

    public async Task<PecaDto?> BuscarPorId(int idDormente) => (await Montar(await pecas.Resumos(idDormente: idDormente))).FirstOrDefault();

    /// <summary>Abre a peça pela tag (ativa ou não). Nulo: tag não associada.</summary>
    public async Task<PecaDto?> BuscarPorTag(string numero)
        => await pecas.BuscarTag(numero.Trim()) is { } tag ? await BuscarPorId(tag.IdDormente) : null;

    /// <summary>Abre a peça pelo código impresso (DM-E01-L02-0034). Nulo quando o código não é de nenhuma peça.</summary>
    public async Task<PecaDto?> BuscarPorCodigo(string codigo)
    {
        var id = IdDoCodigo(codigo);
        if (id is null) return null;
        var peca = await BuscarPorId(id.Value);
        return peca is not null && string.Equals(peca.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase) ? peca : null;
    }

    /// <summary>O número da peça no fim do código (DM-E01-L02-0034 → 34); nulo se o código não tem esse formato.</summary>
    public static int? IdDoCodigo(string? codigo)
    {
        var m = System.Text.RegularExpressions.Regex.Match(codigo?.Trim() ?? "", @"^DM-E\d+-L\d+-(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success && int.TryParse(m.Groups[1].Value, out var id) ? id : null;
    }

    /// <summary>Várias peças de uma vez, pelo id (para as listas de alertas e trocas).</summary>
    public async Task<IReadOnlyDictionary<int, PecaDto>> PorIds(IReadOnlyCollection<int> ids)
        => ids.Count == 0 ? new Dictionary<int, PecaDto>() : (await Montar(await pecas.Resumos(ids: ids))).ToDictionary(p => p.Id);

    public async Task<IReadOnlyList<PecaDto>> DoFeixe(int idFeixe) => await Montar(await pecas.Resumos(idFeixe: idFeixe));

    /// <summary>
    /// O estoque: peças aprovadas na verificação, com tag ativa e fora da via. A reprovada e a que ainda não
    /// foi verificada não aparecem. Novos na frente: a entrada mais recente primeiro.
    /// </summary>
    public async Task<IReadOnlyList<PecaDto>> Estoque(int? idArmazem = null, int? idTamanho = null, byte? classe = null)
        => (await Montar(await pecas.Resumos(soDisponiveis: true)))
            .Where(p => (idArmazem is null || p.IdArmazem == idArmazem) && (idTamanho is null || p.IdTamanho == idTamanho) && (classe is null || p.Classe == classe))
            .OrderByDescending(p => p.EntradaArmazemEm ?? DateTime.MinValue).ThenByDescending(p => p.Id)
            .ToList();

    internal async Task<IReadOnlyList<PecaDto>> Montar(IReadOnlyList<PecaResumo> resumos)
    {
        if (resumos.Count == 0)
            return [];
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho);
        var armazens = (await catalogo.ListarArmazens()).ToDictionary(a => a.IdArmazem, a => a.Nome);
        var pilhas = (await catalogo.ListarPilhas()).ToDictionary(p => p.IdPilha);

        return resumos.Select(r =>
        {
            var especie = especies[r.IdEspecieVigente];
            var pilha = r.IdPilha is int idPilha ? pilhas.GetValueOrDefault(idPilha) : null;
            var idArmazem = pilha?.IdArmazem ?? r.IdArmazemChegada;
            return new PecaDto(r.IdDormente, $"DM-E{r.IdEmpresa:00}-L{r.IdLote:00}-{r.IdDormente:0000}", r.TagAtiva, r.TagFornecedor,
                r.IdPedido, r.IdLote, r.IdFeixe, r.NumeroFeixe, especie.IdEspecie, especie.NomeCientifico, especie.Classe,
                r.IdTamanho, tamanhos[r.IdTamanho].ComprimentoM, idArmazem, armazens.GetValueOrDefault(idArmazem, ""),
                pilha?.IdPilha, pilha?.Codigo, r.EntradaArmazemEm, r.IdTrecho, r.KmProjeto, Situacao(r));
        }).ToList();
    }

    /// <summary>Estado derivado, não coluna guardada.</summary>
    private static string Situacao(PecaResumo r)
        => r.RetiradaEm is not null ? "Retirada"
            : r.IdTrecho is not null || r.KmProjeto is not null ? "NaVia"
            : r.Verificacao is null ? "AguardandoVerificacao"
            : r.Verificacao == ResultadoVerificacao.Reprovada ? "Reprovada"
            : r.IdPilha is not null ? "NoArmazem"
            : "Aprovada";

    private async Task<int> LoteDaChegada(int idPedido)
        => (await pecas.LoteDoPedido(idPedido))?.IdLote
           ?? throw new RegraDeNegocioException("A chegada deste feixe não tem lote.");

    private async Task<CadastroPecaDto> Repetida(GravacaoTag repetida)
    {
        var peca = repetida.IdRfid is int idRfid && await pecas.BuscarTag(repetida.TagGravada) is { } tag && tag.IdRfid == idRfid
            ? await BuscarPorId(tag.IdDormente) : null;
        return new CadastroPecaDto("Repetido", "Este registro já tinha chegado antes; nada foi gravado de novo.", peca);
    }
}