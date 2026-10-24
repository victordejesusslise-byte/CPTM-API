using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViaPermanente.Api.Via;
using ViaPermanente.Dados;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Testes.Banco;

namespace ViaPermanente.Testes.Api;

/// <summary>
/// A API inteira em memória, ligada ao banco de teste: sobe com as migrações, cria o supervisor inicial e um usuário de cada cargo,
/// e apaga o banco no fim. É o "front adulterado" (Victor, 2026-10-23): chama a API direto, sem tela nenhuma, como faria quem mexe
/// no código do front pelo DevTools.
/// </summary>
public sealed class ApiDeTeste : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Senha = "senha-de-teste-1";
    private const string EmailSupervisor = "supervisor.api@teste.local";
    private readonly Dictionary<Cargo, string> tokens = [];

    /// <summary>A pasta das fotos dos testes (apagada no fim).</summary>
    public string PastaFotos { get; } = Path.Combine(Path.GetTempPath(), "via-api-testes-" + Guid.NewGuid().ToString("N"));

    private Dictionary<string, string> Configuracao() => new()
    {
        ["ConnectionStrings__ViaPermanente"] = BancoDeTeste.Conexao,
        ["Jwt__Chave"] = "chave-so-dos-testes-da-api-" + Guid.NewGuid().ToString("N"),
        ["UsuarioInicial__Nome"] = "Supervisor da API",
        ["UsuarioInicial__Email"] = EmailSupervisor,
        ["UsuarioInicial__Senha"] = Senha,
        ["Banco__MigrarAoIniciar"] = "true",
        ["Seguranca__ExigirHttps"] = "false",
        ["Seguranca__TentativasPorMinuto"] = "1000",
        ["Fotos__Pasta"] = PastaFotos
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        foreach (var (chave, valor) in Configuracao())
            builder.UseSetting(chave.Replace("__", ":"), valor);
        // A manutenção diária de LEITURA_VIA não roda nos testes.
        builder.ConfigureTestServices(s => s.Remove(s.Single(d => d.ImplementationType == typeof(ManutencaoDiariaHandler))));
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(PastaFotos);
        await using (var db = BancoDeTeste.Contexto())
            await db.Database.EnsureDeletedAsync();
        // As variáveis de ambiente valem mais que o .env (Env.NoClobber no Program): a API não alcança o banco de verdade.
        foreach (var (chave, valor) in Configuracao())
            Environment.SetEnvironmentVariable(chave, valor);

        using (var escopo = Services.CreateScope())
        {
            var conexao = escopo.ServiceProvider.GetRequiredService<ViaPermanenteContext>().Database.GetConnectionString();
            if (conexao != BancoDeTeste.Conexao)
                throw new InvalidOperationException("A API dos testes não está no banco de teste; parei antes de mexer em qualquer coisa.");
        }

        tokens[Cargo.Supervisor] = await Entrar(EmailSupervisor, Senha);
        foreach (var cargo in Enum.GetValues<Cargo>().Where(c => c != Cargo.Supervisor))
        {
            var email = $"{cargo.ToString().ToLowerInvariant()}.api@teste.local";
            await CriarUsuario(cargo, email);
            tokens[cargo] = await Entrar(email, Senha);
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await using (var db = BancoDeTeste.Contexto())
            await db.Database.EnsureDeletedAsync();
        try { Directory.Delete(PastaFotos, true); } catch (IOException) { }
    }

    /// <summary>O token de quem tem o cargo (um usuário por cargo, criado na subida).</summary>
    public string Token(Cargo cargo) => tokens[cargo];

    /// <summary>Um cliente da API com o token dado (ou sem token).</summary>
    public HttpClient Cliente(string? token = null)
    {
        var cliente = CreateClient();
        if (token is not null)
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    public HttpClient Cliente(Cargo cargo) => Cliente(Token(cargo));

    public async Task<string> Entrar(string email, string senha)
    {
        var resposta = await Cliente().PostAsJsonAsync("/api/auth/login", new { email, senha });
        resposta.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString()!;
    }

    /// <summary>O supervisor cadastra alguém; devolve o id.</summary>
    public async Task<int> CriarUsuario(Cargo cargo, string email)
    {
        var resposta = await Cliente(Cargo.Supervisor).PostAsJsonAsync("/api/usuarios",
            new { nome = $"{cargo} da API", email, senha = Senha, cargo = cargo.ToString() });
        resposta.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt32();
    }
}
