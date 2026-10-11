using ViaPermanente.Dominio.Models.Compras;

namespace ViaPermanente.Dominio.Repositories;

public interface IOfertaRepository
{
    /// <summary>Todas as ofertas (o histórico); com idEmpresa, só as daquele fornecedor.</summary>
    Task<IReadOnlyList<OfertaFornecedor>> Listar(int? idEmpresa);
    Task Adicionar(OfertaFornecedor oferta);
}