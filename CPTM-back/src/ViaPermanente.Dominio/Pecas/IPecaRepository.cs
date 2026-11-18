using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>O acesso ao banco para a peça: cadastro, etiqueta, leituras, verificação, armazém e as consultas da tela.</summary>
public interface IPecaRepository
{
    /// <summary>O feixe pelo id. Nulo: não existe.</summary>
    Task<Feixe?> BuscarFeixe(int idFeixe);
    /// <summary>A chegada pelo id. Nula: não existe.</summary>
    Task<Recebimento?> BuscarRecebimento(int idRecebimento);
    /// <summary>Quantas peças do feixe já foram cadastradas.</summary>
    Task<int> PecasNoFeixe(int idFeixe);
    /// <summary>O lote do pedido (o primeiro). Nulo: o pedido ainda não chegou.</summary>
    Task<Lote?> LoteDoPedido(int idPedido);
    /// <summary>O pedido do feixe, com a sigla do fornecedor e as peças já cadastradas do pedido inteiro. Nulo: feixe não encontrado.</summary>
    Task<PedidoDoFeixe?> PedidoDoFeixe(int idFeixe);

    /// <summary>O número já está numa etiqueta, ou já foi gravado (ok ou divergente)?</summary>
    Task<bool> TagEmUso(string tag);
    /// <summary>O maior número de tag já usado (nas linhas ok ou divergentes da GRAVACAO_TAG).</summary>
    Task<string?> MaiorTag();
    /// <summary>A gravação já recebida com este id do celular. Nula: é um envio novo.</summary>
    Task<GravacaoTag?> GravacaoPorCliente(Guid idCliente);
    /// <summary>Grava uma tentativa que não ligou etiqueta à peça (bloqueada, falhou, divergente).</summary>
    Task RegistrarGravacao(GravacaoTag gravacao);
    /// <summary>
    /// Grava a peça, o código dela (depois que o número existe), a gravação ok (a etiqueta vigente da peça) e a primeira classificação,
    /// numa transação.
    /// </summary>
    Task Cadastrar(Dormente dormente, GravacaoTag gravacao, ClassificacaoMadeira classificacao);

    /// <summary>A peça pelo id. Nula: não existe.</summary>
    Task<Dormente?> Buscar(int idDormente);
    /// <summary>A peça pelo código impresso (DM-E01-L01-0028), direto na coluna DORMENTE.codigo. Nulo: nenhuma peça tem esse código.</summary>
    Task<int?> IdPeloCodigo(string codigo);
    /// <summary>A etiqueta vigente da peça (GRAVACAO_TAG com vigente = 1).</summary>
    Task<TagDaPeca?> TagVigente(int idDormente);
    /// <summary>A etiqueta pelo número, vigente ou não (a vigente primeiro).</summary>
    Task<TagDaPeca?> BuscarTag(string numero);
    /// <summary>A verificação de qualidade da peça (a primeira). Nula: ainda não verificada.</summary>
    Task<VerificacaoQualidade?> Verificacao(int idDormente);

    /// <summary>Grava uma leitura sozinha (a ambígua, ou a de outra peça, que não muda nada).</summary>
    Task RegistrarLeitura(LeituraDormente leitura);
    /// <summary>
    /// Grava a leitura e a verificação; na reprovada, a etiqueta vigente perde a marca e entra a linha Desligada. Tudo junto.
    /// </summary>
    Task Verificar(VerificacaoQualidade verificacao, LeituraDormente leitura, GravacaoTag? desligada);
    /// <summary>Grava a leitura, a movimentação de entrada e a pilha da peça, juntas.</summary>
    Task Guardar(int idDormente, int idPilha, DateTime entradaEm, Movimentacao movimentacao, LeituraDormente leitura);
    /// <summary>
    /// Guarda o lote de uma vez (Victor, 2026-11-17): grava a leitura da peça lida, uma movimentação de entrada por peça (só a da lida
    /// leva a leitura) e a pilha de cada peça. Tudo junto: se falhar, nada fica gravado.
    /// </summary>
    Task GuardarVarias(IReadOnlyList<Movimentacao> entradas, DateTime entradaEm, LeituraDormente leitura);

    /// <summary>As peças com tudo o que a tela mostra. soDisponiveis: aprovadas, com tag vigente, fora da via. idLote: só as peças desse lote.</summary>
    Task<IReadOnlyList<PecaResumo>> Resumos(int? idDormente = null, int? idFeixe = null, bool soDisponiveis = false, IReadOnlyCollection<int>? ids = null, int? idLote = null);
}