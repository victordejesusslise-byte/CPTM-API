using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Integracoes;

namespace ViaPermanente.Dados.Integracoes;

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