using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dados.Catalogos;

public class CatalogoRepository(ViaPermanenteContext db) : ICatalogoRepository
{
    public async Task<IReadOnlyList<TamanhoDormente>> ListarTamanhos()
        => await db.Set<TamanhoDormente>().AsNoTracking().OrderBy(t => t.ComprimentoM).ToListAsync();

    public async Task<IReadOnlyList<Especie>> ListarEspecies()
        => await db.Set<Especie>().AsNoTracking().OrderBy(e => e.NomeCientifico).ToListAsync();

    public async Task<IReadOnlyList<Armazem>> ListarArmazens()
        => await db.Set<Armazem>().AsNoTracking().OrderBy(a => a.Km).ToListAsync();

    public async Task<IReadOnlyList<Pilha>> ListarPilhas()
        => await db.Set<Pilha>().AsNoTracking().OrderBy(p => p.IdArmazem).ThenBy(p => p.Codigo).ToListAsync();

    public async Task<IReadOnlyList<Leitor>> ListarLeitores()
        => await db.Set<Leitor>().AsNoTracking().OrderBy(l => l.Codigo).ToListAsync();

    public async Task<IReadOnlyList<TipoLeitor>> ListarTiposLeitor()
        => await db.Set<TipoLeitor>().AsNoTracking().OrderBy(t => t.IdTipoLeitor).ToListAsync();

    public async Task<IReadOnlyList<ModoLeitura>> ListarModos()
        => await db.Set<ModoLeitura>().AsNoTracking().OrderBy(m => m.IdModo).ToListAsync();

    public Task DefinirCodigoErp(int idTamanho, byte classe, string codigoErp)
        => classe == 1
            ? db.Set<TamanhoDormente>().Where(t => t.IdTamanho == idTamanho).ExecuteUpdateAsync(s => s.SetProperty(t => t.CodigoErpClasse1, codigoErp))
            : db.Set<TamanhoDormente>().Where(t => t.IdTamanho == idTamanho).ExecuteUpdateAsync(s => s.SetProperty(t => t.CodigoErpClasse2, codigoErp));

    public async Task AdicionarLeitor(Leitor leitor)
    {
        db.Add(leitor);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyDictionary<int, int>> ContarPecasPorPilha()
        => await db.Set<Dormente>().AsNoTracking()
            .Where(d => d.IdPilha != null)
            .GroupBy(d => d.IdPilha!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
}