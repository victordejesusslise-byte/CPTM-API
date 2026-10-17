using System.Globalization;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Chegadas.Dtos;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Pedidos;

namespace ViaPermanente.Dominio.Chegadas;

/// <summary>
/// A placa do feixe lida pela foto, conferida pelo operador (Placa lida pela foto, conferida pelo operador). O OCR roda no servidor,
/// com o Tesseract e o modelo ativo de MODELO_OCR; cada foto é uma leitura. O campo com confiança de ocr_confianca_minima ou mais e que
/// confere com o pedido vem pronto; o resto vem aberto. A confirmação grava, uma vez, o valor confirmado e se foi corrigido: é a amostra de treino.
/// </summary>
public class OcrPlacaService(IOcrRepository ocr, IFotoRepository fotos, ILeituraOcrRepository leituras, IPedidoRepository pedidos,
    ICatalogoRepository catalogo, IParametroRepository parametros)
{
    public const string ChaveConfiancaMinima = "ocr_confianca_minima";

    public async Task<LeituraOcrDto> Ler(byte[] foto, string extensao, int? idPedido, int idUsuario, Guid? idCliente = null, DateTime? registradoEm = null)
    {
        if (foto.Length == 0)
            throw new RegraDeNegocioException("Envie a foto da placa.");
        var modelo = (await leituras.Modelos()).FirstOrDefault(m => m.Ativo) ?? throw new RegraDeNegocioException("Não há modelo de OCR ativo.");
        var pedido = idPedido is int id ? await Pedido(id) ?? throw new RegraDeNegocioException("Pedido não encontrado.") : null;

        IReadOnlyList<ResultadoOcr> resultados;
        try { resultados = ocr.Ler(foto, modelo.ModeloBase); }
        catch (Exception erro) when (erro is not RegraDeNegocioException)
        {
            throw new RegraDeNegocioException($"A foto não pôde ser lida: {erro.Message}");
        }

        // Cada leitura da foto dá os seus campos. De cada campo fica o que confere com o pedido; entre esses, o valor que mais leituras
        // acharam (a maioria vence) e, no empate, o da leitura que vem antes: o repositório põe primeiro as leituras mais certeiras.
        var lidos = resultados.Select(r => PlacaDoFeixe.Extrair(r.Linhas)).ToList();
        var candidatos = lidos.SelectMany((campos, leitura) => campos.Select(c => (Lido: c, Leitura: leitura))).ToList();
        if (pedido is not null && candidatos.All(c => c.Lido.Campo != CampoOcr.Fornecedor)
            && PlacaDoFeixe.AcharFornecedor(resultados.SelectMany(r => r.Linhas), pedido.Fornecedor) is { } fornecedor)
            candidatos.Add((fornecedor, resultados.Count));
        var escolhidos = candidatos
            .Select(c => (c.Lido, c.Leitura, Confere: PlacaDoFeixe.Confere(c.Lido.Campo, c.Lido.Valor, pedido)))
            .GroupBy(c => c.Lido.Campo)
            .Select(campo => campo
                .GroupBy(c => c.Confere switch { true => 0, null => 1, false => 2 }).MinBy(g => g.Key)!
                .GroupBy(c => PlacaDoFeixe.Chave(c.Lido.Valor)).OrderByDescending(g => g.Count()).ThenBy(g => g.Min(c => c.Leitura)).First()
                .GroupBy(c => c.Lido.Valor).OrderByDescending(g => g.Count()).ThenBy(g => g.Min(c => c.Leitura)).First()
                .MaxBy(c => c.Lido.Confianca))
            .ToList();
        // O texto guardado é o da leitura que achou mais campos; a confiança média é a dos campos escolhidos.
        var melhor = resultados[lidos.Select((c, i) => (c.Count, i)).OrderByDescending(x => x.Count).First().i];

        var agora = DateTime.Now;
        var leitura = new LeituraOcr
        {
            Origem = OrigemLeituraOcr.PlacaFeixe, FotoArquivo = fotos.Salvar(foto, "ocr", extensao), TextoLido = melhor.Texto.Trim(),
            ConfiancaMedia = Math.Round(escolhidos.Count > 0 ? escolhidos.Average(c => c.Lido.Confianca) : melhor.ConfiancaMedia, 2),
            IdModelo = modelo.IdModelo, IdUsuario = idUsuario, Data = agora, IdCliente = idCliente, RegistradoEm = registradoEm ?? agora, RecebidoEm = agora
        };
        var campos = escolhidos.Select(c => new OcrCampo
        {
            Campo = c.Lido.Campo, ValorLido = c.Lido.Valor, Confianca = Math.Round(c.Lido.Confianca, 2), ConferePedido = c.Confere,
            CaixaX = c.Lido.Linha.X, CaixaY = c.Lido.Linha.Y, CaixaLargura = c.Lido.Linha.Largura, CaixaAltura = c.Lido.Linha.Altura
        }).ToList();
        await leituras.Gravar(leitura, campos);
        return (await BuscarPorId(leitura.IdLeituraOcr))!;
    }

    /// <summary>O operador confirma (uma vez). Devolve nulo se a leitura não existe.</summary>
    public async Task<LeituraOcrDto?> Confirmar(int idLeitura, ConfirmarPlacaDto dto)
    {
        if (await leituras.Buscar(idLeitura) is null)
            return null;
        var campos = await leituras.Campos(idLeitura);
        if (campos.Any(c => c.ValorConfirmado is not null))
            throw new RegraDeNegocioException("Esta leitura já foi confirmada.");
        var enviados = new Dictionary<CampoOcr, string>();
        foreach (var c in dto.Campos)
            enviados[Enum.TryParse<CampoOcr>(c.Campo, true, out var campo) ? campo : throw new RegraDeNegocioException($"Campo {c.Campo} não existe.")] = (c.Valor ?? "").Trim();
        var confirmacoes = campos.Select(c =>
        {
            var valor = enviados.TryGetValue(c.Campo, out var v) ? v : c.ValorLido ?? "";
            return (c.IdCampo, valor, valor != (c.ValorLido ?? ""));
        }).ToList();
        await leituras.Confirmar(idLeitura, confirmacoes, dto.IdRecebimento, dto.IdFeixe);
        return await BuscarPorId(idLeitura);
    }

    public async Task<LeituraOcrDto?> BuscarPorId(int idLeitura)
    {
        var leitura = await leituras.Buscar(idLeitura);
        if (leitura is null)
            return null;
        var minimo = await ConfiancaMinima();
        var versao = (await leituras.Modelos()).FirstOrDefault(m => m.IdModelo == leitura.IdModelo)?.Versao ?? 0;
        var campos = await leituras.Campos(idLeitura);
        return new LeituraOcrDto(leitura.IdLeituraOcr, leitura.FotoArquivo, leitura.TextoLido, leitura.ConfiancaMedia, versao,
            campos.Any(c => c.ValorConfirmado is not null),
            campos.OrderBy(c => c.Campo).Select(c => new CampoOcrDto(c.IdCampo, c.Campo.ToString(), c.ValorLido, c.Confianca, c.ConferePedido,
                c.Confianca >= minimo && c.ConferePedido == true, c.ValorConfirmado, c.Corrigido, c.CaixaX, c.CaixaY, c.CaixaLargura, c.CaixaAltura)).ToList());
    }

    public async Task<IReadOnlyList<ModeloOcrDto>> Modelos()
        => (await leituras.Modelos()).Select(m => new ModeloOcrDto(m.IdModelo, m.Nome, m.Versao, m.ModeloBase, m.TreinadoEm, m.Amostras, m.AcertoTeste, m.Ativo)).ToList();

    private async Task<PedidoParaPlaca?> Pedido(int idPedido)
    {
        var pedido = (await pedidos.Listar(idPedido)).FirstOrDefault();
        if (pedido is null)
            return null;
        var contrato = pedido.IdContrato is int c ? await pedidos.BuscarContrato(c) : null;
        var itens = await pedidos.Itens([idPedido]);
        var especies = (await catalogo.ListarEspecies()).ToDictionary(e => e.IdEspecie);
        var tamanhos = (await catalogo.ListarTamanhos()).ToDictionary(t => t.IdTamanho, t => t.ComprimentoM);
        return new PedidoParaPlaca((await pedidos.BuscarEmpresa(pedido.IdEmpresa))?.Nome ?? "", contrato?.NumeroAta, contrato?.NumeroContrato,
            itens.Where(i => i.IdEspecie is int e && especies.ContainsKey(e)).Select(i => (especies[i.IdEspecie!.Value].NomeCientifico, i.Classe)).ToList(),
            itens.Select(i => i.Classe).Distinct().ToList(), itens.Select(i => tamanhos[i.IdTamanho]).Distinct().ToList());
    }

    private async Task<decimal> ConfiancaMinima()
        => decimal.TryParse((await parametros.Buscar(ChaveConfiancaMinima))?.Valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 97;
}