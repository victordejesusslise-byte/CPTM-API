using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Integracoes.Dtos;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Integracoes;

/// <summary>
/// Importa as notas fiscais da planilha que a API deles gera (Integração só por planilha). As linhas da mesma nota viram uma nota,
/// com origem planilha, conferida com o pedido como a digitada. Uma nota com linha inválida não entra (para não ficar pela metade):
/// o motivo vai para INTEGRACAO_LOG. Toda nota importada também fica no log.
/// </summary>
public class ImportacaoNotaService(IPlanilhaNotaRepository planilha, IIntegracaoLogRepository log, IPedidoRepository pedidos,
    ICatalogoRepository catalogo, NotaFiscalService notas)
{
    public async Task<ImportacaoNotasDto> Importar(Stream arquivo, string nomeArquivo, int idUsuario)
    {
        IReadOnlyList<LinhaNotaPlanilha> linhas;
        try { linhas = planilha.Ler(arquivo); }
        catch (Exception erro) when (erro is not RegraDeNegocioException)
        {
            await Registrar([], [$"A planilha não pôde ser lida: {erro.Message}"], nomeArquivo);
            throw new RegraDeNegocioException("A planilha não pôde ser lida. Use o modelo de GET /api/integracoes/notas/modelo.");
        }

        var especies = (await catalogo.ListarEspecies()).Where(e => e.Ativo).ToDictionary(e => e.NomeCientifico.Trim().ToLowerInvariant());
        var tamanhos = (await catalogo.ListarTamanhos()).Where(t => t.Ativo).ToDictionary(t => t.ComprimentoM);
        var importadas = new List<NotaImportadaDto>();
        var erros = new List<string>();

        foreach (var nota in linhas.GroupBy(l => (Pedido: l.Pedido?.Trim() ?? "", Nota: l.Nota?.Trim() ?? "")))
        {
            var problemas = new List<string>();
            var pedido = nota.Key.Pedido.Length == 0 ? null : await pedidos.BuscarPorCodigo(nota.Key.Pedido);
            if (nota.Key.Pedido.Length == 0 || nota.Key.Nota.Length == 0)
                problemas.Add($"Linhas {string.Join(", ", nota.Select(l => l.Linha))}: falta o pedido ou o número da nota.");
            else if (pedido is null)
                problemas.Add($"Nota {nota.Key.Nota}: o pedido {nota.Key.Pedido} não existe.");

            var itens = new List<NovaNotaItemDto>();
            foreach (var l in nota)
            {
                if (l.Erro is not null) { problemas.Add($"Linha {l.Linha}: {l.Erro}"); continue; }
                if (l.Especie is null || !especies.TryGetValue(l.Especie.Trim().ToLowerInvariant(), out var especie)) { problemas.Add($"Linha {l.Linha}: espécie \"{l.Especie}\" não encontrada."); continue; }
                if (l.ComprimentoM is not decimal c || !tamanhos.TryGetValue(c, out var tamanho)) { problemas.Add($"Linha {l.Linha}: comprimento {l.ComprimentoM} m não está no catálogo."); continue; }
                if (l.Classe is not (1 or 2)) { problemas.Add($"Linha {l.Linha}: a classe é 1 ou 2."); continue; }
                if (l.Quantidade is not > 0) { problemas.Add($"Linha {l.Linha}: a quantidade precisa ser maior que zero."); continue; }
                itens.Add(new NovaNotaItemDto { IdEspecie = especie.IdEspecie, IdTamanho = tamanho.IdTamanho, Classe = (byte)l.Classe!, Quantidade = l.Quantidade!.Value, VolumeM3 = l.VolumeM3 ?? 0 });
            }

            if (problemas.Count > 0 || pedido is null)
            {
                erros.AddRange(problemas.Select(p => $"{p} A nota {nota.Key.Nota} não foi importada."));
                continue;
            }
            try
            {
                var primeira = nota.First();
                var registrada = (await notas.Registrar(pedido.IdPedido, new NovaNotaDto
                {
                    Numero = nota.Key.Nota, DataEmissao = primeira.Emissao, Valor = primeira.Valor, Arquivo = nomeArquivo, Itens = itens
                }, idUsuario, OrigemNota.Planilha))!;
                importadas.Add(new NotaImportadaDto(pedido.Codigo, registrada.Numero, registrada.Id, registrada.StatusConferencia, registrada.Divergencias));
            }
            catch (RegraDeNegocioException regra)
            {
                erros.Add($"Nota {nota.Key.Nota} do pedido {nota.Key.Pedido}: {regra.Message} A nota não foi importada.");
            }
        }

        await Registrar(importadas, erros, nomeArquivo);
        return new ImportacaoNotasDto(nomeArquivo, importadas, erros);
    }

    public byte[] Modelo() => planilha.Modelo();

    public async Task<IReadOnlyList<IntegracaoLogDto>> Log(int quantidade = 100)
        => (await log.Ultimos(quantidade)).Select(l => new IntegracaoLogDto(l.IdLog, l.Data, l.Sistema.ToString(), l.Direcao.ToString(), l.Tipo.ToString(),
            l.Status.ToString(), l.Mensagem, l.Arquivo)).ToList();

    private Task Registrar(IReadOnlyList<NotaImportadaDto> importadas, IReadOnlyList<string> erros, string arquivo)
    {
        var agora = DateTime.Now;
        IntegracaoLog Linha(StatusIntegracao status, string mensagem) => new()
        {
            Sistema = SistemaIntegracao.PlanilhaNota, Direcao = DirecaoIntegracao.Entrada, Tipo = TipoIntegracao.Nota,
            Data = agora, Status = status, Mensagem = mensagem.Length > 1000 ? mensagem[..1000] : mensagem, Arquivo = arquivo
        };
        return log.Registrar(importadas.Select(n => Linha(StatusIntegracao.Ok, $"Nota {n.Numero} do pedido {n.Pedido} importada: {n.StatusConferencia}."))
            .Concat(erros.Select(e => Linha(StatusIntegracao.Erro, e))).ToList());
    }
}