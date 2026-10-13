using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Pessoas;

public class UsuarioRepository(ViaPermanenteContext db) : IUsuarioRepository
{
    /// <summary>Junta USUARIO e PESSOA, filtrando antes de montar o resultado (o EF não filtra depois do construtor).</summary>
    private IQueryable<UsuarioComPessoa> ComPessoa(Expression<Func<Pessoa, bool>> filtroPessoa, Expression<Func<Usuario, bool>> filtroUsuario)
        => from u in db.Set<Usuario>().AsNoTracking().Where(filtroUsuario)
           join p in db.Set<Pessoa>().AsNoTracking().Where(filtroPessoa) on u.IdPessoa equals p.IdPessoa
           orderby p.Nome
           select new UsuarioComPessoa(u, p);

    public Task<UsuarioComPessoa?> BuscarPorEmail(string emailNormalizado)
        => ComPessoa(p => p.Email == emailNormalizado, u => true).FirstOrDefaultAsync();

    public Task<UsuarioComPessoa?> BuscarPorId(int idUsuario)
        => ComPessoa(p => true, u => u.IdUsuario == idUsuario).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<UsuarioComPessoa>> Listar()
        => await ComPessoa(p => true, u => true).ToListAsync();

    public Task<bool> ExisteAlgum() => db.Set<Usuario>().AnyAsync();

    public Task<bool> EmailEmUso(string emailNormalizado)
        => db.Set<Pessoa>().AnyAsync(p => p.Email == emailNormalizado);

    public async Task<UsuarioComPessoa> Criar(Pessoa pessoa, Usuario usuario)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(pessoa);
        await db.SaveChangesAsync();
        usuario.IdPessoa = pessoa.IdPessoa;
        db.Add(usuario);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
        return new UsuarioComPessoa(usuario, pessoa);
    }
}