using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Catalogos;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

public class ClassificacaoRepository(ViaPermanenteContext db) : IClassificacaoRepository
{
    public Task<Especie?> Especie(int idEspecie) => db.Set<Especie>().AsNoTracking().FirstOrDefaultAsync(e => e.IdEspecie == idEspecie);

    public Task<ClassificacaoMadeira?> Ultima(int idDormente)
        => db.Set<ClassificacaoMadeira>().AsNoTracking().Where(c => c.IdDormente == idDormente).OrderByDescending(c => c.IdClassificacao).FirstOrDefaultAsync();

    public Task<ClassificacaoMadeira?> PorCliente(Guid idCliente)
        => db.Set<ClassificacaoMadeira>().AsNoTracking().FirstOrDefaultAsync(c => c.IdCliente == idCliente);

    public async Task<ClassificacaoMadeira> Adicionar(ClassificacaoMadeira classificacao)
    {
        db.Add(classificacao);
        await db.SaveChangesAsync();
        return classificacao;
    }

    public async Task<IReadOnlyList<(ClassificacaoMadeira Classificacao, Especie Especie)>> Listar(int? idDormente, DateTime? desde)
    {
        var consulta = from c in db.Set<ClassificacaoMadeira>().AsNoTracking()
                       join e in db.Set<Especie>().AsNoTracking() on c.IdEspecie equals e.IdEspecie
                       select new { c, e };
        if (idDormente is not null) consulta = consulta.Where(x => x.c.IdDormente == idDormente);
        if (desde is not null) consulta = consulta.Where(x => x.c.Data >= desde && x.c.Metodo != MetodoClassificacao.PlacaFornecedor);
        var lista = await consulta.OrderByDescending(x => x.c.IdClassificacao).Take(200).ToListAsync();
        return lista.Select(x => (x.c, x.e)).ToList();
    }
}
