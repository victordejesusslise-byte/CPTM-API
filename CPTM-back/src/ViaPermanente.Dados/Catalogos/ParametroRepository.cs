using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Models;

namespace ViaPermanente.Dados.Catalogos;

public class ParametroRepository(ViaPermanenteContext db) : IParametroRepository
{
    public async Task<IReadOnlyList<Parametro>> Listar()
        => await db.Set<Parametro>().AsNoTracking().OrderBy(p => p.Chave).ToListAsync();

    public Task<Parametro?> Buscar(string chave) => db.Set<Parametro>().FirstOrDefaultAsync(p => p.Chave == chave);

    public Task Salvar(Parametro parametro) => db.SaveChangesAsync();
}