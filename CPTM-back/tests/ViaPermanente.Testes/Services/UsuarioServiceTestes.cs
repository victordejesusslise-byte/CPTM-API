using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

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
        Assert.NotEqual("senha-boa-1", repo.Linhas[0].SenhaHash);
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
        Assert.Equal(Cargo.Supervisor, Assert.Single(repo.Linhas).TipoCargo);
    }

    [Fact]
    public async Task Login_confere_senha_e_usuario_ativo()
    {
        await new UsuarioService(repo).Criar(Novo());
        var auth = new AuthService(repo);

        Assert.NotNull((await auth.Entrar(new LoginDto { Email = "ANA@cptm.local", Senha = "senha-boa-1" })).Usuario);
        Assert.Null((await auth.Entrar(new LoginDto { Email = "ana@cptm.local", Senha = "errada-123" })).Usuario);
        Assert.Null((await auth.Entrar(new LoginDto { Email = "ninguem@cptm.local", Senha = "senha-boa-1" })).Usuario);

        repo.Linhas[0].TentativasFalhas = 0; // a senha errada acima contou uma
        repo.Linhas[0].Ativo = false;
        Assert.Null((await auth.Entrar(new LoginDto { Email = "ana@cptm.local", Senha = "senha-boa-1" })).Usuario);
    }

    private static LoginDto Ana(string senha = "senha-boa-1") => new() { Email = "ana@cptm.local", Senha = senha };

    /// <summary>Segurança (2026-10-18): depois de 5 senhas erradas seguidas, a conta espera 30 s, depois 1 min, depois 5 min; nem a senha certa entra enquanto espera.</summary>
    [Fact]
    public async Task Senha_errada_seguida_faz_a_conta_esperar_cada_vez_mais()
    {
        await new UsuarioService(repo).Criar(Novo());
        var auth = new AuthService(repo);
        var agora = new DateTime(2026, 10, 18, 10, 0, 0);

        for (var i = 0; i < 4; i++)
            Assert.Null((await auth.Entrar(Ana("errada-123"), agora)).EsperarAte);
        await auth.Entrar(Ana("errada-123"), agora); // a 5ª
        var bloqueado = await auth.Entrar(Ana(), agora.AddSeconds(10));
        Assert.Null(bloqueado.Usuario);
        Assert.Equal(agora.AddSeconds(30), bloqueado.EsperarAte);

        await auth.Entrar(Ana("errada-123"), agora.AddSeconds(31)); // a 6ª: 1 minuto
        Assert.Equal(agora.AddSeconds(31).AddMinutes(1), repo.Linhas[0].EsperarAte);
        await auth.Entrar(Ana("errada-123"), agora.AddMinutes(2)); // a 7ª: 5 minutos
        Assert.Equal(agora.AddMinutes(7), repo.Linhas[0].EsperarAte);

        var certo = await auth.Entrar(Ana(), agora.AddMinutes(8));
        Assert.NotNull(certo.Usuario);
        Assert.Equal(0, repo.Linhas[0].TentativasFalhas);
        Assert.Null(repo.Linhas[0].EsperarAte);
    }

    /// <summary>Segurança (2026-10-18): o token vale só com a conta ativa e a versão da sessão atual; sair e desativar derrubam na hora.</summary>
    [Fact]
    public async Task Sair_e_desativar_derrubam_a_sessao_na_hora()
    {
        var supervisor = await new UsuarioService(repo).Criar(Novo("chefe@cptm.local"));
        var ana = await new UsuarioService(repo).Criar(Novo());
        var auth = new AuthService(repo);
        var versao = (await auth.Entrar(Ana())).VersaoSessao;
        Assert.True(await auth.SessaoValida(ana.Id, versao));

        await auth.Sair(ana.Id);
        Assert.False(await auth.SessaoValida(ana.Id, versao));

        var nova = (await auth.Entrar(Ana())).VersaoSessao;
        Assert.True(await auth.SessaoValida(ana.Id, nova));
        await new UsuarioService(repo).DefinirAtivo(ana.Id, false, supervisor.Id);
        Assert.False(await auth.SessaoValida(ana.Id, nova));
        Assert.Null((await auth.Entrar(Ana())).Usuario);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => new UsuarioService(repo).DefinirAtivo(supervisor.Id, false, supervisor.Id));
        Assert.False(await auth.SessaoValida(999, 0));
    }
}
