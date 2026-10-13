using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dados.Pedidos;

public class OfertaRepository(ViaPermanenteContext db) : IOfertaRepository
{
    public async Task<IReadOnlyList<OfertaFornecedor>> Listar(int? idEmpresa)
        => await db.Set<OfertaFornecedor>().AsNoTracking()
            .Where(o => idEmpresa == null || o.IdEmpresa == idEmpresa)
            .ToListAsync();

    public async Task Adicionar(OfertaFornecedor oferta)
    {
        db.Add(oferta);
        await db.SaveChangesAsync();
    }
}