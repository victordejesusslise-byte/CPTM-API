using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Models.Via;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class CadastroRepository(ViaPermanenteContext db) : ICadastroRepository
{
    public async Task<IReadOnlyList<Empresa>> Empresas() => await db.Set<Empresa>().AsNoTracking().ToListAsync();
    public async Task<IReadOnlyList<Contrato>> Contratos() => await db.Set<Contrato>().AsNoTracking().ToListAsync();
    public async Task<IReadOnlyList<Estacao>> Estacoes() => await db.Set<Estacao>().AsNoTracking().ToListAsync();
    public async Task<IReadOnlyList<Trecho>> Trechos() => await db.Set<Trecho>().AsNoTracking().ToListAsync();

    public async Task Adicionar<T>(T entidade) where T : class
    {
        db.Add(entidade);
        await db.SaveChangesAsync();
    }

    public Task AtivarEmpresa(int idEmpresa, bool ativo)
        => db.Set<Empresa>().Where(e => e.IdEmpresa == idEmpresa).ExecuteUpdateAsync(s => s.SetProperty(e => e.Ativo, ativo));
}