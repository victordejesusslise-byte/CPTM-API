using ViaPermanente.Dados;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// Segurança no SQL Server (2026-10-18, os achados da auditoria do Codex): a espera por senhas erradas e a versão da sessão são gravadas
/// de verdade, e a sessão cai ao sair e ao desativar a conta.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class SegurancaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Espera_e_sessao_ficam_no_banco()
    {
        var repo = new UsuarioRepository(db);
        var usuarios = new UsuarioService(repo);
        var supervisor = await usuarios.Criar(new NovoUsuarioDto { Nome = "Chefe", Email = "chefe@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor });
        var tecnico = await usuarios.Criar(new NovoUsuarioDto { Nome = "Téc", Email = "tec@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Tecnico });
        var auth = new AuthService(repo);
        var agora = DateTime.Now;

        for (var i = 0; i < AuthService.ErrosLivres; i++)
            await auth.Entrar(new LoginDto { Email = "tec@teste.local", Senha = "errada-123" }, agora);
        db.ChangeTracker.Clear();
        var esperando = await auth.Entrar(new LoginDto { Email = "tec@teste.local", Senha = "senha-boa-1" }, agora.AddSeconds(5));
        Assert.Null(esperando.Usuario);
        Assert.NotNull(esperando.EsperarAte);
        Assert.NotNull((await usuarios.BuscarPorId(tecnico.Id))!.EsperarAte); // o supervisor vê em Usuários

        db.ChangeTracker.Clear();
        var entrou = await auth.Entrar(new LoginDto { Email = "tec@teste.local", Senha = "senha-boa-1" }, agora.AddMinutes(1));
        Assert.NotNull(entrou.Usuario);
        Assert.True(await auth.SessaoValida(tecnico.Id, entrou.VersaoSessao));

        await auth.Sair(tecnico.Id);
        Assert.False(await auth.SessaoValida(tecnico.Id, entrou.VersaoSessao));

        db.ChangeTracker.Clear();
        var outra = await auth.Entrar(new LoginDto { Email = "tec@teste.local", Senha = "senha-boa-1" });
        await usuarios.DefinirAtivo(tecnico.Id, false, supervisor.Id);
        Assert.False(await auth.SessaoValida(tecnico.Id, outra.VersaoSessao));
        db.ChangeTracker.Clear();
        Assert.False((await usuarios.BuscarPorId(tecnico.Id))!.Ativo);

        await usuarios.DefinirAtivo(tecnico.Id, true, supervisor.Id);
        db.ChangeTracker.Clear();
        Assert.NotNull((await auth.Entrar(new LoginDto { Email = "tec@teste.local", Senha = "senha-boa-1" })).Usuario);
    }
}
