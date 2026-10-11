using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

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