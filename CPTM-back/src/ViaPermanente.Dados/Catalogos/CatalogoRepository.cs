using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dados.Catalogos;

public class CatalogoRepository(ViaPermanenteContext db) : ICatalogoRepository
{
    public Task<IReadOnlyList<TamanhoDormente>> ListarTamanhos() => Task.FromResult(TamanhoDormente.Todos);

    public async Task<IReadOnlyList<MedidaFornecedor>> ListarMedidas()
        => await db.Set<MedidaFornecedor>().AsNoTracking().OrderBy(m => m.IdMedida).ToListAsync();

    public async Task<IReadOnlyList<Especie>> ListarEspecies()
        => await db.Set<Especie>().AsNoTracking().OrderBy(e => e.NomeCientifico).ToListAsync();

    public async Task<IReadOnlyList<Armazem>> ListarArmazens()
        => await db.Set<Armazem>().AsNoTracking().OrderBy(a => a.Km).ToListAsync();

    public async Task<IReadOnlyList<Pilha>> ListarPilhas()
        => await db.Set<Pilha>().AsNoTracking().OrderBy(p => p.IdArmazem).ThenBy(p => p.Codigo).ToListAsync();

    public async Task<IReadOnlyList<Leitor>> ListarLeitores()
        => await db.Set<Leitor>().AsNoTracking().OrderBy(l => l.Codigo).ToListAsync();

    public async Task<IReadOnlyList<ModoLeitura>> ListarModos()
        => await db.Set<ModoLeitura>().AsNoTracking().OrderBy(m => m.IdModo).ToListAsync();

    public async Task<IReadOnlyDictionary<(int IdTamanho, byte Classe), string>> CodigosErp()
    {
        var codigos = new Dictionary<(int, byte), string>();
        foreach (var p in await db.Set<Parametro>().AsNoTracking().Where(p => p.Chave.StartsWith(ParametroService.PrefixoCodigoErp)).ToListAsync())
        {
            var partes = p.Chave[ParametroService.PrefixoCodigoErp.Length..].Split('_');
            if (partes.Length == 2 && int.TryParse(partes[0], out var idTamanho) && byte.TryParse(partes[1], out var classe) && !string.IsNullOrWhiteSpace(p.Valor))
                codigos[(idTamanho, classe)] = p.Valor;
        }
        return codigos;
    }

    public async Task DefinirCodigoErp(int idTamanho, byte classe, string codigoErp)
    {
        db.Add(new Parametro { Chave = ParametroService.ChaveCodigoErp(idTamanho, classe), Valor = codigoErp, AlteradoEm = DateTime.Now });
        await db.SaveChangesAsync();
    }

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