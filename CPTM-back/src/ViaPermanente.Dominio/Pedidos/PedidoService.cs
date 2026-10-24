using System.Globalization;
using System.Text.Json;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pedidos;

/// <summary>
/// O pedido de compra (etapa 01): itens por tamanho e classe, ligado ao contrato do fornecedor.
/// Acima do limite de 2ª classe o pedido não é registrado. Cancelar só antes da primeira chegada, com motivo.
/// </summary>
public class PedidoService(IPedidoRepository pedidos, ICatalogoRepository catalogo, IParametroRepository parametros, OfertaService ofertas)
{
    public const string ChaveLimitePct = "limite_segunda_classe_pct";
    public const string ChaveLimiteUnidade = "limite_segunda_classe_unidade";

    /// <summary>
    /// O aviso da tela enquanto o pedido é montado, sem gravar nada: a 2ª classe (acima do limite o pedido não entra)
    /// e, com o fornecedor, a oferta dele (só avisa).
    /// </summary>
    public async Task<VerificacaoPedidoDto> Verificar(VerificarPedidoDto pedido)
    {
        var tamanhos = await TamanhosAtivos();
        var (unidade, limite) = await LimiteVigente();
        var segundaClasse = ParaDto(LimiteSegundaClasse.Validar(ParaLimite(pedido.Itens, tamanhos), unidade, limite));
        var oferta = pedido.IdEmpresa is int idEmpresa ? await ofertas.Avisos(idEmpresa, pedido.Itens) : [];
        return new VerificacaoPedidoDto(segundaClasse, oferta);
    }

    public async Task<PedidoDto> Criar(NovoPedidoDto novo, int idUsuario)
    {
        var codigo = (novo.Codigo ?? "").Trim();
        if (codigo.Length == 0)
            throw new RegraDeNegocioException("Informe o código do pedido.");
        if (await pedidos.CodigoEmUso(codigo))
            throw new RegraDeNegocioException($"Já existe um pedido com o código {codigo}.");

        var empresa = await pedidos.BuscarEmpresa(novo.IdEmpresa)
            ?? throw new RegraDeNegocioException("Fornecedor não encontrado.");
        if (empresa.Papel != PapelEmpresa.Fornecedor || !empresa.Ativo)
            throw new RegraDeNegocioException($"{empresa.Nome} não é um fornecedor ativo.");
        if (novo.IdContrato is int idContrato)
        {
            var contrato = await pedidos.BuscarContrato(idContrato)
                ?? throw new RegraDeNegocioException("Contrato não encontrado.");
            if (contrato.IdEmpresa != empresa.IdEmpresa)
                throw new RegraDeNegocioException($"O contrato {contrato.NumeroContrato} é de outro fornecedor.");
        }

        if (novo.Itens.Count == 0)
            throw new RegraDeNegocioException("O pedido precisa de pelo menos um item.");
        var tamanhos = await TamanhosAtivos();
        var especies = (await catalogo.ListarEspecies()).Where(e => e.Ativo).ToDictionary(e => e.IdEspecie);
        foreach (var item in novo.Itens)
            ConferirItem(item, tamanhos, especies);

        var (unidade, limite) = await LimiteVigente();
        var resultado = LimiteSegundaClasse.Validar(ParaLimite(novo.Itens, tamanhos), unidade, limite);
        if (!resultado.Valido)
            throw new RegraDeNegocioException(resultado.Mensagem);

        var agora = DateTime.Now;
        var pedido = new Pedido
        {
            IdEmpresa = empresa.IdEmpresa, IdUsuario = idUsuario, IdContrato = novo.IdContrato, Codigo = codigo,
            DataCompra = novo.DataCompra ?? DateOnly.FromDateTime(agora), Quantidade = novo.Itens.Sum(i => i.Quantidade),
            CriadoEm = agora, Status = StatusPedido.Aberto
        };
        await pedidos.Criar(pedido, novo.Itens.Select(i => new PedidoItem
        {
            IdTamanho = i.IdTamanho, Classe = i.Classe, IdEspecie = i.IdEspecie, Quantidade = i.Quantidade
        }).ToList());

        return (await BuscarPorId(pedido.IdPedido))!;
    }

    public async Task<IReadOnlyList<PedidoDto>> Listar() => await Montar(await pedidos.Listar());

    public async Task<PedidoDto?> BuscarPorId(int idPedido) => (await Montar(await pedidos.Listar(idPedido))).FirstOrDefault();

    /// <summary>Devolve nulo se o pedido não existe. Depois da primeira chegada o pedido trava (Pedido trava quando a carga chega).</summary>
    public async Task<PedidoDto?> Cancelar(int idPedido, string motivo, int idUsuario)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new RegraDeNegocioException("Informe o motivo do cancelamento.");
        var pedido = (await pedidos.Listar(idPedido)).FirstOrDefault();
        if (pedido is null)
            return null;
        if (pedido.Status == StatusPedido.Cancelado)
            throw new RegraDeNegocioException("O pedido já está cancelado.");
        if (await pedidos.TemRecebimento(idPedido))
            throw new RegraDeNegocioException("A carga deste pedido já chegou: o pedido não pode mais ser cancelado.");

        var antes = Retrato(pedido);
        pedido.Status = StatusPedido.Cancelado;
        await pedidos.Cancelar(idPedido, new PedidoHistorico
        {
            IdPedido = idPedido, IdUsuario = idUsuario, Data = DateTime.Now,
            RetratoAntes = antes, RetratoDepois = Retrato(pedido), Motivo = motivo.Trim()
        });
        return await BuscarPorId(idPedido);
    }

    /// <summary>
    /// Corrige o contrato, a data e os itens, só antes da primeira chegada e com motivo. Confere os itens e a 2ª classe de novo,
    /// e guarda o retrato de antes e de depois (com os itens) em PEDIDO_HISTORICO. Devolve nulo se o pedido não existe.
    /// </summary>
    public async Task<PedidoDto?> Corrigir(int idPedido, CorrigirPedidoDto dto, int idUsuario)
    {
        var pedido = (await pedidos.Listar(idPedido)).FirstOrDefault();
        if (pedido is null)
            return null;
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new RegraDeNegocioException("Informe o motivo da correção.");
        if (pedido.Status == StatusPedido.Cancelado)
            throw new RegraDeNegocioException("O pedido está cancelado.");
        if (await pedidos.TemRecebimento(idPedido))
            throw new RegraDeNegocioException("A carga deste pedido já chegou: o pedido não pode mais ser corrigido.");
        if (dto.IdContrato is int idContrato)
        {
            var contrato = await pedidos.BuscarContrato(idContrato) ?? throw new RegraDeNegocioException("Contrato não encontrado.");
            if (contrato.IdEmpresa != pedido.IdEmpresa)
                throw new RegraDeNegocioException($"O contrato {contrato.NumeroContrato} é de outro fornecedor.");
        }
        if (dto.Itens.Count == 0)
            throw new RegraDeNegocioException("O pedido precisa de pelo menos um item.");
        var tamanhos = await TamanhosAtivos();
        var especies = (await catalogo.ListarEspecies()).Where(e => e.Ativo).ToDictionary(e => e.IdEspecie);
        foreach (var item in dto.Itens)
            ConferirItem(item, tamanhos, especies);
        var (unidade, limite) = await LimiteVigente();
        var resultado = LimiteSegundaClasse.Validar(ParaLimite(dto.Itens, tamanhos), unidade, limite);
        if (!resultado.Valido)
            throw new RegraDeNegocioException(resultado.Mensagem);

        var antes = Retrato(pedido, await pedidos.Itens([idPedido]));
        var novosItens = dto.Itens.Select(i => new PedidoItem { IdTamanho = i.IdTamanho, Classe = i.Classe, IdEspecie = i.IdEspecie, Quantidade = i.Quantidade }).ToList();
        pedido.IdContrato = dto.IdContrato;
        pedido.DataCompra = dto.DataCompra ?? pedido.DataCompra;
        pedido.Quantidade = novosItens.Sum(i => i.Quantidade);
        await pedidos.Corrigir(pedido, novosItens, new PedidoHistorico
        {
            IdPedido = idPedido, IdUsuario = idUsuario, Data = DateTime.Now, Motivo = dto.Motivo.Trim(),
            RetratoAntes = antes, RetratoDepois = Retrato(pedido, novosItens)
        });
        return await BuscarPorId(idPedido);
    }

    /// <summary>As correções e o cancelamento do pedido, do mais antigo para o mais novo. Nulo se o pedido não existe.</summary>
    public async Task<IReadOnlyList<PedidoHistoricoDto>?> Historico(int idPedido)
        => (await pedidos.Listar(idPedido)).Count == 0 ? null
            : (await pedidos.Historico(idPedido)).Select(h => new PedidoHistoricoDto(h.IdHistorico, h.Data, h.IdUsuario, h.Motivo, h.RetratoAntes, h.RetratoDepois)).ToList();

    // ---------- montagem da resposta ----------

    private async Task<IReadOnlyList<PedidoDto>> Montar(IReadOnlyList<Pedido> lista)
    {
        if (lista.Count == 0)
            return [];
        var ids = lista.Select(p => p.IdPedido).ToList();
        var itens = (await pedidos.Itens(ids)).ToLookup(i => i.IdPedido);
        var recebidas = await pedidos.PecasRecebidas(ids);
        var empresas = await pedidos.NomesEmpresas(lista.Select(p => p.IdEmpresa).Distinct().ToList());
        var contratos = await pedidos.NumerosContratos(lista.Where(p => p.IdContrato != null).Select(p => p.IdContrato!.Value).Distinct().ToList());
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho);
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        var (unidade, limite) = await LimiteVigente();

        return lista.Select(p =>
        {
            var dele = itens[p.IdPedido].Select(i =>
            {
                var t = tamanhos[i.IdTamanho];
                var especie = i.IdEspecie is int e && especies.TryGetValue(e, out var achada) ? achada.NomeCientifico : null;
                return new PedidoItemDto(i.IdPedidoItem, i.IdTamanho, t.ComprimentoM, i.Classe, i.IdEspecie, especie,
                    i.Quantidade, i.Quantidade * M3(t));
            }).ToList();
            var paraLimite = itens[p.IdPedido].Select(i => new ItemParaLimite(i.Classe, i.Quantidade, M3(tamanhos[i.IdTamanho]))).ToList();
            var pecasRecebidas = recebidas.GetValueOrDefault(p.IdPedido);
            return new PedidoDto(p.IdPedido, p.Codigo, p.IdEmpresa, empresas.GetValueOrDefault(p.IdEmpresa, ""),
                p.IdContrato, p.IdContrato is int c ? contratos.GetValueOrDefault(c) : null,
                p.DataCompra, p.CriadoEm, p.Quantidade, dele.Sum(i => i.VolumeM3),
                p.Status.ToString(), Situacao(p, pecasRecebidas), pecasRecebidas,
                ParaDto(LimiteSegundaClasse.Validar(paraLimite, unidade, limite)), dele);
        }).ToList();
    }

    /// <summary>Estado derivado: cancelado vem do status; recebido e parcial vêm das chegadas.</summary>
    private static string Situacao(Pedido p, int pecasRecebidas)
        => p.Status == StatusPedido.Cancelado ? "Cancelado"
            : pecasRecebidas >= p.Quantidade && p.Quantidade > 0 ? "Recebido"
            : pecasRecebidas > 0 ? "RecebidoParcial"
            : p.Status.ToString();

    // ---------- regras dos itens e do parâmetro ----------

    private static void ConferirItem(NovoPedidoItemDto item, IReadOnlyDictionary<int, TamanhoDormente> tamanhos, IReadOnlyDictionary<int, Especie> especies)
    {
        if (item.Quantidade <= 0)
            throw new RegraDeNegocioException("A quantidade de cada item precisa ser maior que zero.");
        if (item.Classe is not (1 or 2))
            throw new RegraDeNegocioException("A classe é 1 ou 2.");
        if (!tamanhos.ContainsKey(item.IdTamanho))
            throw new RegraDeNegocioException("Tamanho de dormente não encontrado.");
        if (item.IdEspecie is int idEspecie)
        {
            // A classe vem do pedido: qualquer uma das espécies pode ser de 1ª ou de 2ª (Victor, 2026-10-16).
            _ = especies.GetValueOrDefault(idEspecie) ?? throw new RegraDeNegocioException("Espécie não encontrada.");
        }
    }

    private async Task<IReadOnlyDictionary<int, TamanhoDormente>> TamanhosAtivos()
        => (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho);

    private static List<ItemParaLimite> ParaLimite(IEnumerable<NovoPedidoItemDto> itens, IReadOnlyDictionary<int, TamanhoDormente> tamanhos)
        => itens.Select(i => new ItemParaLimite(i.Classe, i.Quantidade,
                tamanhos.TryGetValue(i.IdTamanho, out var t) ? M3(t) : throw new RegraDeNegocioException("Tamanho de dormente não encontrado.")))
            .ToList();

    /// <summary>A unidade e o limite que valem hoje, lidos de PARAMETRO ("20" vira 0,20; "m3" ou "pecas").</summary>
    private async Task<(UnidadeLimite Unidade, decimal Limite)> LimiteVigente()
    {
        var pct = (await parametros.Buscar(ChaveLimitePct))?.Valor;
        var unidade = (await parametros.Buscar(ChaveLimiteUnidade))?.Valor?.Trim().ToLowerInvariant();
        if (!decimal.TryParse(pct, NumberStyles.Number, CultureInfo.InvariantCulture, out var limite) || limite is < 0 or > 100)
            throw new RegraDeNegocioException($"O parâmetro {ChaveLimitePct} precisa ser um número de 0 a 100.");
        return unidade switch
        {
            "m3" => (UnidadeLimite.MetroCubico, limite / 100),
            "pecas" => (UnidadeLimite.Pecas, limite / 100),
            _ => throw new RegraDeNegocioException($"O parâmetro {ChaveLimiteUnidade} precisa ser \"m3\" ou \"pecas\".")
        };
    }

    private static decimal M3(TamanhoDormente t) => VolumeHelper.M3(t.ComprimentoM, t.LarguraM, t.AlturaM);

    private static LimiteDto ParaDto(ResultadoLimite r)
        => new(r.Valido, Math.Round(r.Percentual * 100, 1), r.Unidade == UnidadeLimite.Pecas ? "pecas" : "m3", r.Limite * 100, r.Mensagem);

    private static string Retrato(Pedido p)
        => JsonSerializer.Serialize(new { p.Codigo, p.IdEmpresa, p.IdContrato, p.DataCompra, p.Quantidade, Status = p.Status.ToString() });

    /// <summary>O retrato com os itens: o "quantidade 42 → 47" sai da comparação dos dois.</summary>
    private static string Retrato(Pedido p, IEnumerable<PedidoItem> itens)
        => JsonSerializer.Serialize(new
        {
            p.Codigo, p.IdEmpresa, p.IdContrato, p.DataCompra, p.Quantidade, Status = p.Status.ToString(),
            Itens = itens.Select(i => new { i.IdTamanho, i.Classe, i.IdEspecie, i.Quantidade })
        });
}