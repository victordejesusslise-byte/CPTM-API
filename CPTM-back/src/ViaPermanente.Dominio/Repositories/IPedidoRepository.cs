using ViaPermanente.Dominio.Models.Compras;

namespace ViaPermanente.Dominio.Repositories;

public interface IPedidoRepository
{
    Task<Empresa?> BuscarEmpresa(int idEmpresa);
    Task<Contrato?> BuscarContrato(int idContrato);
    Task<bool> CodigoEmUso(string codigo);
    Task<Pedido?> BuscarPorCodigo(string codigo);

    /// <summary>Grava o pedido e os itens juntos (numa transação).</summary>
    Task Criar(Pedido pedido, IReadOnlyList<PedidoItem> itens);

    /// <summary>Os pedidos, do mais novo para o mais antigo; com id, só aquele.</summary>
    Task<IReadOnlyList<Pedido>> Listar(int? idPedido = null);
    Task<IReadOnlyList<PedidoItem>> Itens(IReadOnlyCollection<int> idsPedido);
    /// <summary>Peças informadas nas chegadas de cada pedido.</summary>
    Task<IReadOnlyDictionary<int, int>> PecasRecebidas(IReadOnlyCollection<int> idsPedido);
    Task<bool> TemRecebimento(int idPedido);
    Task<IReadOnlyDictionary<int, string>> NomesEmpresas(IReadOnlyCollection<int> ids);
    Task<IReadOnlyDictionary<int, string>> NumerosContratos(IReadOnlyCollection<int> ids);

    /// <summary>Grava o pedido corrigido, troca os itens e grava o histórico, tudo junto.</summary>
    Task Corrigir(Pedido pedido, IReadOnlyList<PedidoItem> itens, PedidoHistorico historico);

    Task<IReadOnlyList<PedidoHistorico>> Historico(int idPedido);

    /// <summary>Grava o status cancelado e o histórico juntos.</summary>
    Task Cancelar(int idPedido, PedidoHistorico historico);
}