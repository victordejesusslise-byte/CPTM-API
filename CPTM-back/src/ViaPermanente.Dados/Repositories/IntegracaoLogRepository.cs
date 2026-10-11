using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Ambiental;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class IntegracaoLogRepository(ViaPermanenteContext db) : IIntegracaoLogRepository
{
    public async Task Registrar(IReadOnlyList<IntegracaoLog> linhas)
    {
        db.AddRange(linhas);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<IntegracaoLog>> Ultimos(int quantidade)
        => await db.Set<IntegracaoLog>().AsNoTracking().OrderByDescending(l => l.IdLog).Take(quantidade).ToListAsync();
}