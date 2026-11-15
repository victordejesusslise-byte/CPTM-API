using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// Guarda as fotos do campo (troca, lote, inspeção e vistoria) na pasta das fotos, fora do banco.
/// O celular manda a foto primeiro e depois o registro com o caminho que ela devolve, também quando ficou sem rede.
/// </summary>
public class FotoCampoService(IFotoRepository fotos, ITrocaRepository trocas, IAlertaRepository alertas, IPecaRepository pecas, ILoteTrocaRepository? lotes = null)
{
    // Separada: antes de fazer ou recusar. Feita e Recusada: o celular mandou de novo depois de a rede cair.
    private static readonly StatusTroca[] TrocaAceitaFoto = [StatusTroca.Separada, StatusTroca.Feita, StatusTroca.Recusada];
    // O mesmo para o lote: com as peças separadas, ou já feito ou recusado (reenvio).
    private static readonly StatusLoteTroca[] LoteAceitaFoto = [StatusLoteTroca.Separado, StatusLoteTroca.Feito, StatusLoteTroca.Recusado];

    /// <summary>Uma foto de um dormente do lote (a peça nova no lugar) ou a da recusa do lote; nulo se o lote não existe.</summary>
    public async Task<string?> DoLote(int idLote, byte[] conteudo, string extensao)
    {
        // Sem o repositório dos lotes, ou lote que não existe: nulo (vira 404).
        var lote = lotes is null ? null : (await lotes.Listar(idLote: idLote)).FirstOrDefault();
        if (lote is null) return null;
        // A foto do lote só é tirada na via, depois da separação.
        if (!LoteAceitaFoto.Contains(lote.Status)) throw new RegraDeNegocioException("A foto do lote é tirada na via, com as peças separadas.");
        return fotos.Salvar(conteudo, $"lotes/{idLote}", FotoPecaService.ExtensaoDaFoto(conteudo, extensao));
    }

    /// <summary>Guarda a foto da troca e devolve o caminho; nulo se a troca não existe.</summary>
    public async Task<string?> DaTroca(int idTroca, byte[] conteudo, string extensao)
    {
        var troca = (await trocas.Listar(idTroca: idTroca)).FirstOrDefault();
        if (troca is null) return null;
        // A foto da troca só é tirada na via, com a peça já separada.
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
