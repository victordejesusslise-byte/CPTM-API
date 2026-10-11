using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Catalogos;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class ParametroRepository(ViaPermanenteContext db) : IParametroRepository
{
    public async Task<IReadOnlyList<Parametro>> Listar()
        => await db.Set<Parametro>().AsNoTracking().OrderBy(p => p.Chave).ToListAsync();

    public Task<Parametro?> Buscar(string chave) => db.Set<Parametro>().FirstOrDefaultAsync(p => p.Chave == chave);

    public Task Salvar(Parametro parametro) => db.SaveChangesAsync();
}