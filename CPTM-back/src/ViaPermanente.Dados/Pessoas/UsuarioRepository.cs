using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Pessoas;

public class UsuarioRepository(ViaPermanenteContext db) : IUsuarioRepository
{
    public Task<Usuario?> BuscarPorEmail(string emailNormalizado)
        => db.Set<Usuario>().AsNoTracking().FirstOrDefaultAsync(u => u.Email == emailNormalizado);

    public Task<Usuario?> BuscarPorId(int idUsuario)
        => db.Set<Usuario>().AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

    public async Task<IReadOnlyList<Usuario>> Listar()
        => await db.Set<Usuario>().AsNoTracking().OrderBy(u => u.Nome).ToListAsync();

    public Task<bool> ExisteAlgum() => db.Set<Usuario>().AnyAsync();

    public Task<bool> EmailEmUso(string emailNormalizado)
        => db.Set<Usuario>().AnyAsync(u => u.Email == emailNormalizado);

    public async Task<Usuario> Criar(Usuario usuario)
    {
        db.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task<(bool Ativo, int VersaoSessao)?> Sessao(int idUsuario)
    {
        var s = await db.Set<Usuario>().AsNoTracking().Where(u => u.IdUsuario == idUsuario)
            .Select(u => new { u.Ativo, u.VersaoSessao }).FirstOrDefaultAsync();
        return s is null ? null : (s.Ativo, s.VersaoSessao);
    }

    public Task EncerrarSessoes(int idUsuario)
        => db.Set<Usuario>().Where(u => u.IdUsuario == idUsuario).ExecuteUpdateAsync(s => s.SetProperty(u => u.VersaoSessao, u => u.VersaoSessao + 1));

    public Task DefinirAtivo(int idUsuario, bool ativo)
        => db.Set<Usuario>().Where(u => u.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Ativo, ativo).SetProperty(u => u.VersaoSessao, u => u.VersaoSessao + 1));

    public Task RegistrarTentativas(int idUsuario, int tentativas, DateTime? esperarAte)
        => db.Set<Usuario>().Where(u => u.IdUsuario == idUsuario)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.TentativasFalhas, tentativas).SetProperty(u => u.EsperarAte, esperarAte));
}