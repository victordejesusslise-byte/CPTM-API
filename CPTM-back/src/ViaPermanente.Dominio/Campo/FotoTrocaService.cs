using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Laudos;
using ViaPermanente.Dominio.Pecas;

namespace ViaPermanente.Dominio.Campo;

/// <summary>
/// A foto da troca (app Android, Etapa 3, 2026-10-24): a da peça nova no lugar (execução) ou a do que a via viu (recusa).
/// O celular manda a foto primeiro e depois o registro com o caminho que ela devolve. Fica na pasta das fotos, fora do banco.
/// </summary>
public class FotoTrocaService(IFotoRepository fotos, ITrocaRepository trocas)
{
    // Separada: antes de fazer ou recusar. Feita e Recusada: o celular mandou de novo depois de a rede cair.
    private static readonly StatusTroca[] AceitaFoto = [StatusTroca.Separada, StatusTroca.Feita, StatusTroca.Recusada];

    /// <summary>Guarda a foto e devolve o caminho; nulo se a troca não existe.</summary>
    public async Task<string?> Salvar(int idTroca, byte[] conteudo, string extensao)
    {
        var troca = (await trocas.Listar(idTroca: idTroca)).FirstOrDefault();
        if (troca is null) return null;
        if (!AceitaFoto.Contains(troca.Status)) throw new RegraDeNegocioException("A foto da troca é tirada na via, com a peça separada.");
        return fotos.Salvar(conteudo, $"trocas/{idTroca}", FotoPecaService.ExtensaoDaFoto(conteudo, extensao));
    }
}
