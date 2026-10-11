using ViaPermanente.Dominio.Models.Chegada;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Models.Patio;

namespace ViaPermanente.Dominio.Repositories;

public interface IChegadaRepository
{
    /// <summary>As chegadas do pedido, na ordem em que foram registradas.</summary>
    Task<IReadOnlyList<Recebimento>> Listar(int idPedido);
    Task<IReadOnlyList<Feixe>> Feixes(IReadOnlyCollection<int> idsRecebimento);
    /// <summary>Quantas peças de cada feixe já foram cadastradas.</summary>
    Task<IReadOnlyDictionary<int, int>> PecasCadastradas(IReadOnlyCollection<int> idsFeixe);
    Task<Lote?> LoteDoPedido(int idPedido);
    /// <summary>Grava a chegada, o lote (só na primeira) e os feixes juntos.</summary>
    Task Registrar(Recebimento recebimento, Lote? loteNovo, IReadOnlyList<Feixe> feixes);
}