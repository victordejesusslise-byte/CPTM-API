using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Chegadas.Dtos;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Chegadas;

/// <summary>
/// Etapa 02: a carga chega num armazém, com a nota, o documento de origem e os feixes (dados da placa).
/// A primeira chegada cria o lote (O lote nasce da chegada) e trava o pedido. A conferência é recalculada a cada consulta.
/// </summary>
public class ChegadaService(IChegadaRepository chegadas, IPedidoRepository pedidos, INotaFiscalRepository notas, ICatalogoRepository catalogo)
{
    /// <summary>Devolve nulo se o pedido não existe.</summary>
    public async Task<ChegadaDto?> Registrar(int idPedido, NovaChegadaDto nova, int idUsuario)
    {
        var pedido = (await pedidos.Listar(idPedido)).FirstOrDefault();
        if (pedido is null)
            return null;
        if (pedido.Status == StatusPedido.Cancelado)
            throw new RegraDeNegocioException("O pedido está cancelado: não recebe carga.");
        if (nova.QuantidadeRecebida <= 0)
            throw new RegraDeNegocioException("Informe quantas peças chegaram.");
        if (nova.Feixes.Count == 0)
            throw new RegraDeNegocioException("Registre pelo menos um feixe.");
        if ((await catalogo.ListarArmazens()).All(a => a.IdArmazem != nova.IdArmazem || !a.Ativo))
            throw new RegraDeNegocioException("Armazém não encontrado.");
        if (nova.IdNota is int idNota && (await notas.Notas(idPedido)).All(n => n.IdNota != idNota))
            throw new RegraDeNegocioException("A nota não é deste pedido.");

        var tamanhos = (await catalogo.ListarTamanhos()).Where(t => t.Ativo).ToDictionary(t => t.IdTamanho);
        var especies = (await catalogo.ListarEspecies()).Where(e => e.Ativo).ToDictionary(e => e.IdEspecie);
        var numerosJaRegistrados = (await chegadas.Feixes((await chegadas.Listar(idPedido)).Select(r => r.IdRecebimento).ToList()))
            .Select(f => f.NumeroFeixe).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var feixe in nova.Feixes)
        {
            var numero = (feixe.NumeroFeixe ?? "").Trim();
            if (numero.Length == 0)
                throw new RegraDeNegocioException("Informe o número de cada feixe.");
            if (!numerosJaRegistrados.Add(numero))
                throw new RegraDeNegocioException($"O feixe {numero} já foi registrado neste pedido.");
            if (feixe.Quantidade <= 0)
                throw new RegraDeNegocioException($"O feixe {numero} precisa ter peças.");
            if (!especies.ContainsKey(feixe.IdEspecie))
                throw new RegraDeNegocioException($"Feixe {numero}: espécie não encontrada.");
            if (!tamanhos.ContainsKey(feixe.IdTamanho))
                throw new RegraDeNegocioException($"Feixe {numero}: tamanho de dormente não encontrado.");
            await ConferirEmpresa(feixe.IdEmpresaTratamento, PapelEmpresa.Tratadora, numero);
            await ConferirEmpresa(feixe.IdEmpresaInspecao, PapelEmpresa.Inspetora, numero);
        }

        var agora = DateTime.Now;
        var lote = await chegadas.LoteDoPedido(idPedido);
        var loteNovo = lote is null
            ? new Lote { IdEmpresa = pedido.IdEmpresa, IdPedido = idPedido, Codigo = $"L-{pedido.Codigo}", DataCriacao = agora, Descricao = $"Carga do pedido {pedido.Codigo}" }
            : null;
        var recebimento = new Recebimento
        {
            IdPedido = idPedido, IdUsuario = idUsuario, IdNota = nova.IdNota, IdArmazem = nova.IdArmazem,
            DocumentoOrigemTipo = Limpo(nova.DocumentoOrigemTipo), DocumentoOrigemNumero = Limpo(nova.DocumentoOrigemNumero),
            DataRecebimento = nova.DataRecebimento ?? agora, QuantidadeRecebida = nova.QuantidadeRecebida, Observacao = Limpo(nova.Observacao)
        };
        await chegadas.Registrar(recebimento, loteNovo, nova.Feixes.Select(f => new Feixe
        {
            NumeroFeixe = f.NumeroFeixe.Trim(), Quantidade = f.Quantidade, IdEspecie = f.IdEspecie, IdTamanho = f.IdTamanho,
            Tratamento = Limpo(f.Tratamento), IdEmpresaTratamento = f.IdEmpresaTratamento, IdEmpresaInspecao = f.IdEmpresaInspecao,
            AnoInspecao = f.AnoInspecao
        }).ToList());

        return (await Listar(idPedido))!.First(c => c.Id == recebimento.IdRecebimento);
    }

    /// <summary>As chegadas do pedido, cada uma conferida com a nota, com os feixes e com o pedido. Nulo se o pedido não existe.</summary>
    public async Task<IReadOnlyList<ChegadaDto>?> Listar(int idPedido)
    {
        if ((await pedidos.Listar(idPedido)).Count == 0)
            return null;
        var lista = await chegadas.Listar(idPedido);
        if (lista.Count == 0)
            return [];

        var feixes = (await chegadas.Feixes(lista.Select(r => r.IdRecebimento).ToList())).ToLookup(f => f.IdRecebimento);
        var cadastradas = await chegadas.PecasCadastradas(feixes.SelectMany(g => g).Select(f => f.IdFeixe).ToList());
        var notasDoPedido = await notas.Notas(idPedido);
        var itensDasNotas = (await notas.Itens(notasDoPedido.Select(n => n.IdNota).ToList())).ToLookup(i => i.IdNota);
        var pedidoItens = await pedidos.Itens([idPedido]);
        var lote = await chegadas.LoteDoPedido(idPedido);
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho);
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        var armazens = (await catalogo.ListarArmazens()).ToDictionary(a => a.IdArmazem, a => a.Nome);

        var anteriores = new List<(int IdTamanho, byte Classe, int Quantidade)>();
        var resposta = new List<ChegadaDto>();
        foreach (var r in lista)
        {
            var dela = feixes[r.IdRecebimento].ToList();
            var porClasse = dela.Select(f => (f.IdTamanho, especies[f.IdEspecie].Classe, f.Quantidade)).ToList();
            int? quantidadeNota = r.IdNota is int n ? itensDasNotas[n].Sum(i => i.Quantidade) : null;

            var divergencias = new List<string>();
            if (ConferenciaChegada.Conferir(quantidadeNota, r.QuantidadeRecebida, dela.Select(f => f.Quantidade).ToList()) is { } divergencia)
                divergencias.Add(divergencia.Mensagem);
            divergencias.AddRange(ConferenciaChegada.ContraPedido(pedidoItens, anteriores, porClasse, tamanhos));
            anteriores.AddRange(porClasse);

            var avisos = new List<string> { Esperado(pedidoItens, anteriores) };
            if (r.IdNota is null)
                avisos.Add("Chegada sem nota: a quantidade foi conferida só com os feixes.");

            resposta.Add(new ChegadaDto(r.IdRecebimento, r.IdPedido, r.IdNota,
                r.IdNota is int idN ? notasDoPedido.FirstOrDefault(x => x.IdNota == idN)?.Numero : null,
                r.IdArmazem, armazens.GetValueOrDefault(r.IdArmazem, ""), r.DocumentoOrigemTipo, r.DocumentoOrigemNumero,
                r.DataRecebimento, r.QuantidadeRecebida, r.Observacao, lote?.IdLote ?? 0, lote?.Codigo ?? "",
                dela.Select(f => ParaDto(f, cadastradas.GetValueOrDefault(f.IdFeixe), especies[f.IdEspecie], tamanhos[f.IdTamanho])).ToList(),
                divergencias.Count == 0, divergencias, avisos));
        }
        return resposta;
    }

    /// <summary>"Mostra o esperado de 1ª e 2ª classe": o pedido e o que já chegou, por classe.</summary>
    private static string Esperado(IReadOnlyList<PedidoItem> pedido, IReadOnlyList<(int IdTamanho, byte Classe, int Quantidade)> chegou)
        => $"Pedido: {pedido.Where(p => p.Classe == 1).Sum(p => p.Quantidade)} de 1ª e {pedido.Where(p => p.Classe == 2).Sum(p => p.Quantidade)} de 2ª classe; " +
           $"chegaram até aqui {chegou.Where(c => c.Classe == 1).Sum(c => c.Quantidade)} de 1ª e {chegou.Where(c => c.Classe == 2).Sum(c => c.Quantidade)} de 2ª.";

    private async Task ConferirEmpresa(int? idEmpresa, PapelEmpresa papel, string feixe)
    {
        if (idEmpresa is not int id)
            return;
        var empresa = await pedidos.BuscarEmpresa(id)
            ?? throw new RegraDeNegocioException($"Feixe {feixe}: empresa {id} não encontrada.");
        if (empresa.Papel != papel)
            throw new RegraDeNegocioException($"Feixe {feixe}: {empresa.Nome} não é {(papel == PapelEmpresa.Tratadora ? "tratadora" : "inspetora")}.");
    }

    private static FeixeDto ParaDto(Feixe f, int cadastradas, Especie especie, TamanhoDormente tamanho)
        => new(f.IdFeixe, f.NumeroFeixe, f.Quantidade, cadastradas, f.IdEspecie, especie.NomeCientifico, especie.Classe,
            f.IdTamanho, tamanho.ComprimentoM, f.Tratamento, f.IdEmpresaTratamento, f.IdEmpresaInspecao, f.AnoInspecao);

    private static string? Limpo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}