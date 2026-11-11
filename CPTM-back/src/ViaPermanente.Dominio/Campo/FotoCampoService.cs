using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// As fotos dos registros de campo (app Android, Etapas 3 e 6, 2026-10-24): a da troca (peça nova no lugar, ou a da recusa), a da
/// inspeção e a da vistoria. O celular manda a foto primeiro e depois o registro com o caminho que ela devolve, também quando
/// guardou tudo sem rede. Ficam na pasta das fotos, fora do banco.
/// </summary>
public class FotoCampoService(IFotoRepository fotos, ITrocaRepository trocas, IAlertaRepository alertas, IPecaRepository pecas, ILoteTrocaRepository? lotes = null)
{
    // Separada: antes de fazer ou recusar. Feita e Recusada: o celular mandou de novo depois de a rede cair.
    private static readonly StatusTroca[] TrocaAceitaFoto = [StatusTroca.Separada, StatusTroca.Feita, StatusTroca.Recusada];
    private static readonly StatusLoteTroca[] LoteAceitaFoto = [StatusLoteTroca.Separado, StatusLoteTroca.Feito, StatusLoteTroca.Recusado];

    /// <summary>Uma foto de um dormente do lote (a peça nova no lugar) ou a da recusa do lote; nulo se o lote não existe.</summary>
    public async Task<string?> DoLote(int idLote, byte[] conteudo, string extensao)
    {
        var lote = lotes is null ? null : (await lotes.Listar(idLote: idLote)).FirstOrDefault();
        if (lote is null) return null;
        if (!LoteAceitaFoto.Contains(lote.Status)) throw new RegraDeNegocioException("A foto do lote é tirada na via, com as peças separadas.");
        return fotos.Salvar(conteudo, $"lotes/{idLote}", FotoPecaService.ExtensaoDaFoto(conteudo, extensao));
    }

    /// <summary>Guarda a foto da troca e devolve o caminho; nulo se a troca não existe.</summary>
    public async Task<string?> DaTroca(int idTroca, byte[] conteudo, string extensao)
    {
        var troca = (await trocas.Listar(idTroca: idTroca)).FirstOrDefault();
        if (troca is null) return null;
        if (!TrocaAceitaFoto.Contains(troca.Status)) throw new RegraDeNegocioException("A foto da troca é tirada na via, com a peça separada.");
        return fotos.Salvar(conteudo, $"trocas/{idTroca}", FotoPecaService.ExtensaoDaFoto(conteudo, extensao));
    }

    /// <summary>A foto da vistoria do alerta; nulo se o alerta não existe.</summary>
    public async Task<string?> DaVistoria(int idAlerta, byte[] conteudo, string extensao)
    {
        if ((await alertas.Listar(null, null, idAlerta)).Count == 0) return null;
        return fotos.Salvar(conteudo, $"alertas/{idAlerta}", FotoPecaService.ExtensaoDaFoto(conteudo, extensao));
    }

    /// <summary>A foto da peça na inspeção; nulo se a peça não existe.</summary>
    public async Task<string?> DaInspecao(int idDormente, byte[] conteudo, string extensao)
    {
        if (await pecas.Buscar(idDormente) is null) return null;
        return fotos.Salvar(conteudo, $"pecas/{idDormente}/inspecao", FotoPecaService.ExtensaoDaFoto(conteudo, extensao));
    }
}
