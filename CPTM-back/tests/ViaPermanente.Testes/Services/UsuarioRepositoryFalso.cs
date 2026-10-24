using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Services;

/// <summary>Repositório em memória para testar os services sem banco.</summary>
internal class UsuarioRepositoryFalso : IUsuarioRepository
{
    public List<Usuario> Linhas { get; } = [];

    public Task<Usuario?> BuscarPorEmail(string emailNormalizado)
        => Task.FromResult(Linhas.FirstOrDefault(x => x.Email == emailNormalizado));

    public Task<Usuario?> BuscarPorId(int idUsuario)
        => Task.FromResult(Linhas.FirstOrDefault(x => x.IdUsuario == idUsuario));

    public Task<IReadOnlyList<Usuario>> Listar() => Task.FromResult<IReadOnlyList<Usuario>>(Linhas);

    public Task<bool> ExisteAlgum() => Task.FromResult(Linhas.Count > 0);

    public Task<bool> EmailEmUso(string emailNormalizado) => Task.FromResult(Linhas.Any(x => x.Email == emailNormalizado));

    public Task<Usuario> Criar(Usuario usuario)
    {
        usuario.IdUsuario = Linhas.Count + 1;
        Linhas.Add(usuario);
        return Task.FromResult(usuario);
    }

    private Usuario? U(int id) => Linhas.FirstOrDefault(x => x.IdUsuario == id);

    public Task<(bool Ativo, int VersaoSessao)?> Sessao(int idUsuario)
        => Task.FromResult(U(idUsuario) is { } u ? (u.Ativo, u.VersaoSessao) : ((bool, int)?)null);

    public Task EncerrarSessoes(int idUsuario) { if (U(idUsuario) is { } u) u.VersaoSessao++; return Task.CompletedTask; }

    public Task DefinirAtivo(int idUsuario, bool ativo) { if (U(idUsuario) is { } u) { u.Ativo = ativo; u.VersaoSessao++; } return Task.CompletedTask; }

    public Task RegistrarTentativas(int idUsuario, int tentativas, DateTime? esperarAte)
    {
        if (U(idUsuario) is { } u) { u.TentativasFalhas = tentativas; u.EsperarAte = esperarAte; }
        return Task.CompletedTask;
    }
}