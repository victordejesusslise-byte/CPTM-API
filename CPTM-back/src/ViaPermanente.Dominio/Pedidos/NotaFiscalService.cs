using System.Globalization;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pedidos;

/// <summary>
/// A nota fiscal do fornecedor, anexada ao pedido e conferida na hora (Confere ou Diverge), pela ConferenciaNota.
/// Por enquanto digitada; a importação da planilha que a API deles gera vem na Tarefa 19.
/// </summary>
public class NotaFiscalService(INotaFiscalRepository notas, IPedidoRepository pedidos, ICatalogoRepository catalogo, IParametroRepository parametros)
{
    public const string ChaveToleranciaVolume = "nota_tolerancia_volume_pct";

    /// <summary>Devolve nulo se o pedido não existe.</summary>
    public async Task<NotaDto?> Registrar(int idPedido, NovaNotaDto nova, int idUsuario, OrigemNota origem = OrigemNota.Manual)
    {
        var pedido = (await pedidos.Listar(idPedido)).FirstOrDefault();
        if (pedido is null)
            return null;
        if (pedido.Status == StatusPedido.Cancelado)
            throw new RegraDeNegocioException("O pedido está cancelado: não recebe nota.");

        var numero = (nova.Numero ?? "").Trim();
        if (numero.Length == 0)
            throw new RegraDeNegocioException("Informe o número da nota.");
        if (await notas.NumeroEmUso(pedido.IdEmpresa, numero))
            throw new RegraDeNegocioException($"Este fornecedor já tem a nota {numero} registrada.");
        if (nova.Itens.Count == 0)
            throw new RegraDeNegocioException("A nota precisa de pelo menos um item.");

        var (tamanhos, especies) = await Catalogo();
        foreach (var item in nova.Itens)
        {
            if (item.Quantidade <= 0)
                throw new RegraDeNegocioException("A quantidade de cada item precisa ser maior que zero.");
            if (item.VolumeM3 < 0)
                throw new RegraDeNegocioException("O volume não pode ser negativo.");
            if (item.Classe is not (1 or 2))
                throw new RegraDeNegocioException("A classe é 1 ou 2.");
            if (!tamanhos.ContainsKey(item.IdTamanho))
                throw new RegraDeNegocioException("Tamanho de dormente não encontrado.");
            if (!especies.ContainsKey(item.IdEspecie))
                throw new RegraDeNegocioException("Espécie não encontrada.");
        }

        var itens = nova.Itens.Select(i => new NotaFiscalItem
        {
            IdEspecie = i.IdEspecie, IdTamanho = i.IdTamanho, Classe = i.Classe, Quantidade = i.Quantidade, VolumeM3 = i.VolumeM3
        }).ToList();
        var anteriores = await notas.Itens((await notas.Notas(idPedido)).Select(n => n.IdNota).ToList());
        var resultado = ConferenciaNota.Conferir(await pedidos.Itens([idPedido]), anteriores, itens, tamanhos, especies, await Tolerancia());

        var nota = new NotaFiscal
        {
            IdPedido = idPedido, Numero = numero, DataEmissao = nova.DataEmissao ?? DateOnly.FromDateTime(DateTime.Now),
            Valor = nova.Valor, Arquivo = nova.Arquivo, Origem = origem,
            StatusConferencia = resultado.Confere ? StatusConferenciaNota.Confere : StatusConferenciaNota.Diverge,
            IdUsuarioConferiu = idUsuario, DataConferencia = DateTime.Now
        };
        await notas.Criar(nota, itens);
        return (await Listar(idPedido))!.First(n => n.Id == nota.IdNota);
    }

    /// <summary>
    /// As notas do pedido, cada uma conferida contra o pedido e as notas registradas antes dela.
    /// Devolve nulo se o pedido não existe.
    /// </summary>
    public async Task<IReadOnlyList<NotaDto>?> Listar(int idPedido)
    {
        if ((await pedidos.Listar(idPedido)).Count == 0)
            return null;
        var lista = await notas.Notas(idPedido);
        if (lista.Count == 0)
            return [];

        var itens = (await notas.Itens(lista.Select(n => n.IdNota).ToList())).ToLookup(i => i.IdNota);
        var pedidoItens = await pedidos.Itens([idPedido]);
        var (tamanhos, especies) = await Catalogo();
        var tolerancia = await Tolerancia();
        var anteriores = new List<NotaFiscalItem>();
        var resposta = new List<NotaDto>();

        foreach (var nota in lista)
        {
            var dela = itens[nota.IdNota].ToList();
            var conferencia = ConferenciaNota.Conferir(pedidoItens, anteriores, dela, tamanhos, especies, tolerancia);
            resposta.Add(new NotaDto(nota.IdNota, nota.IdPedido, nota.Numero, nota.DataEmissao, nota.Valor, nota.Arquivo,
                nota.Origem.ToString(), nota.StatusConferencia.ToString(), nota.IdUsuarioConferiu, nota.DataConferencia,
                dela.Select(i =>
                {
                    var t = tamanhos[i.IdTamanho];
                    return new NotaItemDto(i.IdNotaItem, i.IdEspecie, especies[i.IdEspecie].NomeCientifico, i.IdTamanho, t.ComprimentoM,
                        i.Classe, i.Quantidade, i.VolumeM3, i.Quantidade * VolumeHelper.M3(t.ComprimentoM, t.LarguraM, t.AlturaM));
                }).ToList(),
                conferencia.Divergencias, conferencia.Avisos));
            anteriores.AddRange(dela);
        }
        return resposta;
    }

    private async Task<(Dictionary<int, TamanhoDormente>, Dictionary<int, Especie>)> Catalogo()
        => ((await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho), (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie));

    /// <summary>A tolerância de volume de PARAMETRO ("2" vira 0,02).</summary>
    private async Task<decimal> Tolerancia()
    {
        var valor = (await parametros.Buscar(ChaveToleranciaVolume))?.Valor;
        if (!decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var pct) || pct is < 0 or > 100)
            throw new RegraDeNegocioException($"O parâmetro {ChaveToleranciaVolume} precisa ser um número de 0 a 100.");
        return pct / 100;
    }
}