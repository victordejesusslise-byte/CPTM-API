using ViaPermanente.Dominio.Models.Compras;

namespace ViaPermanente.Dominio.Repositories;

public interface INotaFiscalRepository
{
    /// <summary>As notas do pedido, na ordem em que foram registradas.</summary>
    Task<IReadOnlyList<NotaFiscal>> Notas(int idPedido);
    Task<IReadOnlyList<NotaFiscalItem>> Itens(IReadOnlyCollection<int> idsNota);
    /// <summary>O fornecedor já tem uma nota com esse número em algum pedido?</summary>
    Task<bool> NumeroEmUso(int idEmpresa, string numero);
    /// <summary>Grava a nota e os itens juntos (numa transação).</summary>
    Task Criar(NotaFiscal nota, IReadOnlyList<NotaFiscalItem> itens);
}