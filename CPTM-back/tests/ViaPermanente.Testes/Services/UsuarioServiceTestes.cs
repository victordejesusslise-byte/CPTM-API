using ViaPermanente.Dominio.Dtos.Auth;
using ViaPermanente.Dominio.Dtos.Usuarios;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Services;

public class UsuarioServiceTestes
{
    private readonly UsuarioRepositoryFalso repo = new();

    private static NovoUsuarioDto Novo(string email = "Ana@CPTM.local", string senha = "senha-boa-1")
        => new() { Nome = "Ana", Email = email, Senha = senha, Cargo = Cargo.Tecnico };

    [Fact]
    public async Task Cria_com_email_normalizado_e_senha_em_hash()
    {
        var criado = await new UsuarioService(repo).Criar(Novo());

        Assert.Equal("ana@cptm.local", criado.Email);
        Assert.Equal("Tecnico", criado.Cargo);
        Assert.NotEqual("senha-boa-1", repo.Linhas[0].Usuario.SenhaHash);
    }

    [Fact]
    public async Task Senha_curta_e_recusada()
        => await Assert.ThrowsAsync<RegraDeNegocioException>(() => new UsuarioService(repo).Criar(Novo(senha: "1234567")));

    [Fact]
    public async Task Email_repetido_e_recusado()
    {
        var service = new UsuarioService(repo);
        await service.Criar(Novo());
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => service.Criar(Novo(email: " ana@cptm.local ")));
    }

    [Fact]
    public async Task Supervisor_inicial_so_quando_o_banco_esta_vazio()
    {
        var service = new UsuarioService(repo);

        Assert.True(await service.GarantirSupervisorInicial("Chefe", "chefe@cptm.local", "senha-boa-1"));
        Assert.False(await service.GarantirSupervisorInicial("Outro", "outro@cptm.local", "senha-boa-1"));
        Assert.Equal(Cargo.Supervisor, Assert.Single(repo.Linhas).Usuario.TipoCargo);
    }

    [Fact]
    public async Task Login_confere_senha_e_usuario_ativo()
    {
        await new UsuarioService(repo).Criar(Novo());
        var auth = new AuthService(repo);

        Assert.NotNull(await auth.Entrar(new LoginDto { Email = "ANA@cptm.local", Senha = "senha-boa-1" }));
        Assert.Null(await auth.Entrar(new LoginDto { Email = "ana@cptm.local", Senha = "errada-123" }));
        Assert.Null(await auth.Entrar(new LoginDto { Email = "ninguem@cptm.local", Senha = "senha-boa-1" }));

        repo.Linhas[0].Usuario.Ativo = false;
        Assert.Null(await auth.Entrar(new LoginDto { Email = "ana@cptm.local", Senha = "senha-boa-1" }));
    }
}