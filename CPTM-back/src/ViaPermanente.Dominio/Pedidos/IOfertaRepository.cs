using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pedidos;

public interface IOfertaRepository
{
    /// <summary>Todas as ofertas (o histórico); com idEmpresa, só as daquele fornecedor.</summary>
    Task<IReadOnlyList<OfertaFornecedor>> Listar(int? idEmpresa);
    Task Adicionar(OfertaFornecedor oferta);
}