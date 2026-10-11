using System.Linq.Expressions;
using ViaPermanente.Dominio.Models.Apresentacao;

namespace ViaPermanente.Dominio.Repositories;

/// <summary>Acesso a dados da apresentação: contar o fluxo e gravar o pedido de exemplo.</summary>
public interface IApresentacaoRepository
{
    /// <summary>Conta as linhas de cada etapa. Com idPedido, só as daquele pedido.</summary>
    Task<ContagemFluxo> Contar(int? idPedido);

    Task<T?> Primeiro<T>(Expression<Func<T, bool>> filtro) where T : class;

    /// <summary>Grava e devolve a mesma entidade, já com o id do banco.</summary>
    Task<T> Adicionar<T>(T entidade) where T : class;

    /// <summary>Grava várias de uma vez; cada uma volta com o id do banco.</summary>
    Task AdicionarVarios<T>(IReadOnlyList<T> entidades) where T : class;

    /// <summary>Grava as mudanças feitas em entidades já adicionadas.</summary>
    Task Salvar();

    /// <summary>O maior número de tag UHF já usado (BR e a sequência), ou nulo.</summary>
    Task<string?> UltimaTag();

    /// <summary>Tudo ou nada: se algo falhar, nada fica gravado.</summary>
    Task EmTransacao(Func<Task> acao);
}