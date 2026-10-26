using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pecas;

public interface IPecaRepository
{
    Task<Feixe?> BuscarFeixe(int idFeixe);
    Task<Recebimento?> BuscarRecebimento(int idRecebimento);
    Task<int> PecasNoFeixe(int idFeixe);
    Task<Lote?> LoteDoPedido(int idPedido);

    /// <summary>O número já está numa tag, ou já foi gravado (ok ou divergente)?</summary>
    Task<bool> TagEmUso(string tag);
    /// <summary>O maior número de tag já usado (nas tags e nas gravações ok ou divergentes).</summary>
    Task<string?> MaiorTag();
    Task<GravacaoTag?> GravacaoPorCliente(Guid idCliente);
    /// <summary>Grava uma tentativa que não vinculou tag (bloqueada, falhou, divergente).</summary>
    Task RegistrarGravacao(GravacaoTag gravacao);
    /// <summary>Grava a peça, a tag, a primeira classificação e a gravação ou a leitura, juntas.</summary>
    Task Cadastrar(Dormente dormente, Rfid tag, ClassificacaoMadeira classificacao, GravacaoTag? gravacao, LeituraDormente? leitura);

    Task<Dormente?> Buscar(int idDormente);
    Task<Rfid?> TagAtiva(int idDormente);
    /// <summary>A tag pelo número, ativa ou não.</summary>
    Task<Rfid?> BuscarTag(string numero);
    Task<VerificacaoQualidade?> Verificacao(int idDormente);

    /// <summary>Grava uma leitura sozinha (a ambígua, ou a de outra peça, que não muda nada).</summary>
    Task RegistrarLeitura(LeituraDormente leitura);
    /// <summary>Grava a leitura e a verificação; na reprovada, desativa a tag. Tudo junto.</summary>
    Task Verificar(VerificacaoQualidade verificacao, LeituraDormente leitura, int? idRfidDesativar);
    /// <summary>Grava a leitura, a movimentação de entrada e a pilha da peça, juntas.</summary>
    Task Guardar(int idDormente, int idPilha, DateTime entradaEm, Movimentacao movimentacao, LeituraDormente leitura);

    /// <summary>As peças com tudo o que a tela mostra. soDisponiveis: aprovadas, com tag ativa, fora da via.</summary>
    /// <summary>
    /// Os ids da cópia das linhas, em ordem e a partir de depoisDe: as peças cadastradas (com tag ativa) que estão na via. Com desde, só as
    /// que mudaram depois (e também as que saíram da via, para o celular apagar).
    /// </summary>
    Task<IReadOnlyList<int>> IdsDaLinha(DateTime? desde, int depoisDe, int limite);
    Task<IReadOnlyList<PecaResumo>> Resumos(int? idDormente = null, int? idFeixe = null, bool soDisponiveis = false, IReadOnlyCollection<int>? ids = null);
}