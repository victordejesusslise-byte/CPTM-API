using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Api;

/// <summary>
/// A API não confia no front (Victor, 2026-10-23: "dá para editar o código pelo DevTools, isso é uma falha gigante"). Quem mexe no
/// front consegue mostrar um botão escondido ou mandar qualquer coisa para a API; o que não pode é a API aceitar. Estes testes chamam
/// a API direto, como faria um front adulterado.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class SegurancaDaApiTestes(ApiDeTeste api) : IClassFixture<ApiDeTeste>
{
    /// <summary>Rotas que gravam e que todo usuário logado pode chamar, com o motivo.</summary>
    private static readonly Dictionary<string, string> GravamSemCargo = new()
    {
        ["POST api/auth/sair"] = "cada um encerra a própria sessão",
        ["POST api/trocas/{id:int}/decisao"] = "o cargo que aprova é um parâmetro (troca.quem_aprova); o serviço confere"
    };

    private record Rota(string Nome, string Metodo, string Modelo, string[]? Cargos, bool Anonima, bool Multipart, ControllerActionDescriptor? Acao)
    {
        public bool Grava => Metodo is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    private List<Rota> Rotas()
    {
        var fontes = api.Services.GetRequiredService<EndpointDataSource>();
        return fontes.Endpoints.OfType<RouteEndpoint>().SelectMany(e =>
        {
            var metodos = e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["*"];
            // Cargos: cada [Authorize(Roles = ...)] da classe e da ação tem de ser atendido; vale a interseção.
            var listas = e.Metadata.GetOrderedMetadata<IAuthorizeData>().Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                .Select(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToList();
            var cargos = listas.Count == 0 ? null : listas.Aggregate((a, b) => a.Intersect(b).ToArray());
            var anonima = e.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var multipart = e.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes.Any(t => t.StartsWith("multipart/")) == true;
            var modelo = e.RoutePattern.RawText ?? "";
            return metodos.Select(m => new Rota($"{m} {modelo.TrimStart('/')}", m, modelo, cargos, anonima, multipart, e.Metadata.GetMetadata<ControllerActionDescriptor>()));
        }).ToList();
    }

    private static string Endereco(string modelo)
    {
        var url = Regex.Replace(modelo, @"\{[^}]+:int\}", "999999");
        return "/" + Regex.Replace(url, @"\{[^}]+\}", "x").TrimStart('/');
    }

    private static HttpRequestMessage Pedido(Rota rota)
    {
        var pedido = new HttpRequestMessage(new HttpMethod(rota.Metodo), Endereco(rota.Modelo));
        if (rota.Multipart)
            pedido.Content = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "arquivo", "x.jpg" } };
        else if (rota.Metodo is "POST" or "PUT" or "PATCH")
            pedido.Content = JsonContent.Create(new { });
        return pedido;
    }

    [Fact]
    public void So_o_login_e_o_hc_abrem_sem_login()
    {
        var anonimas = Rotas().Where(r => r.Anonima).Select(r => r.Nome).Order().ToList();
        Assert.Equal(["* hc", "POST api/auth/login"], anonimas);
    }

    [Fact]
    public void Toda_rota_que_grava_exige_um_cargo()
    {
        var semCargo = Rotas().Where(r => r.Grava && !r.Anonima && r.Cargos is null && !GravamSemCargo.ContainsKey(r.Nome)).Select(r => r.Nome).ToList();
        Assert.True(semCargo.Count == 0, "Rotas que gravam sem [Authorize(Roles = ...)]: " + string.Join(", ", semCargo));
    }

    [Fact]
    public void As_regras_principais_de_cargo_continuam_as_mesmas()
    {
        // As regras de quem faz o quê (cérebro: 30 Cargos). Afrouxar qualquer uma faz este teste falhar.
        var esperado = new Dictionary<string, string[]>
        {
            ["GET api/usuarios"] = ["Supervisor"],
            ["POST api/usuarios"] = ["Supervisor"],
            ["PUT api/usuarios/{id:int}/ativo"] = ["Supervisor"],
            ["PUT api/parametros/{chave}"] = ["Supervisor"],
            ["POST api/catalogos/leitores"] = ["Supervisor"],
            ["POST api/integracoes/codigos-material"] = ["Supervisor"],
            ["POST api/manutencao/leituras-via"] = ["Supervisor"],
            ["POST api/apresentacao/demonstracao"] = ["Supervisor"],
            ["DELETE api/apresentacao/demonstracao"] = ["Supervisor"],
            ["POST api/classificacoes"] = ["Especialista"],
            ["POST api/inspecoes"] = ["Tecnico"],
            ["POST api/trocas"] = ["Tecnico"],
            ["POST api/trocas/{id:int}/separacao"] = ["OperadorPatio"],
            ["POST api/trocas/{id:int}/execucao"] = ["OperadorVia"],
            ["POST api/pedidos"] = ["OperadorPatio", "Supervisor"],
            ["POST api/dormentes/{id:int}/verificacao"] = ["Especialista", "Supervisor"]
        };
        var rotas = Rotas().ToDictionary(r => r.Nome);
        var diferentes = esperado.Where(e => !rotas.TryGetValue(e.Key, out var r) || r.Cargos is null || !r.Cargos.Order().SequenceEqual(e.Value.Order()))
            .Select(e => $"{e.Key}: esperado {string.Join("/", e.Value)}, está {(rotas.TryGetValue(e.Key, out var r) ? string.Join("/", r.Cargos ?? ["qualquer logado"]) : "sem rota")}")
            .ToList();
        Assert.True(diferentes.Count == 0, string.Join("; ", diferentes));
    }

    [Fact]
    public async Task Cada_cargo_recebe_403_no_que_nao_e_dele()
    {
        var falhas = new List<string>();
        var chamadas = 0;
        foreach (var rota in Rotas().Where(r => r.Cargos is not null))
            foreach (var cargo in Enum.GetValues<Cargo>().Where(c => !rota.Cargos!.Contains(c.ToString())))
            {
                chamadas++;
                var resposta = await api.Cliente(cargo).SendAsync(Pedido(rota));
                if (resposta.StatusCode != HttpStatusCode.Forbidden)
                    falhas.Add($"{cargo} em {rota.Nome}: {(int)resposta.StatusCode}");
            }
        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
        Assert.True(chamadas > 150, $"Só {chamadas} chamadas: as rotas não foram achadas?");
    }

    [Fact]
    public async Task Sem_token_nada_passa()
    {
        var falhas = new List<string>();
        var rotas = Rotas().Where(r => !r.Anonima && r.Acao is not null).ToList();
        foreach (var rota in rotas)
        {
            var resposta = await api.Cliente().SendAsync(Pedido(rota));
            if (resposta.StatusCode != HttpStatusCode.Unauthorized)
                falhas.Add($"{rota.Nome}: {(int)resposta.StatusCode}");
        }
        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
        Assert.True(rotas.Count > 100, $"Só {rotas.Count} rotas: as rotas não foram achadas?");
    }

    [Fact]
    public async Task Token_com_o_cargo_trocado_a_mao_nao_vale()
    {
        var partes = api.Token(Cargo.Tecnico).Split('.');
        var carga = JsonNode.Parse(Base64UrlEncoder.Decode(partes[1]))!;
        carga["role"] = "Supervisor";
        var cargaTrocada = Base64UrlEncoder.Encode(carga.ToJsonString());

        // Mesmo cabeçalho e assinatura, cargo trocado: a assinatura não bate.
        var adulterado = $"{partes[0]}.{cargaTrocada}.{partes[2]}";
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(adulterado).GetAsync("/api/usuarios")).StatusCode);

        // Sem assinatura nenhuma ("alg": "none").
        var semAssinatura = $"{Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{cargaTrocada}.";
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(semAssinatura).GetAsync("/api/usuarios")).StatusCode);

        // O token de verdade do técnico continua valendo, mas sem a tela do supervisor.
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Cliente(Cargo.Tecnico).GetAsync("/api/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Quem_sai_ou_e_desativado_perde_o_token_na_hora()
    {
        await api.CriarUsuario(Cargo.Tecnico, "sai.api@teste.local");
        var token = await api.Entrar("sai.api@teste.local", ApiDeTeste.Senha);
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(token).GetAsync("/api/auth/eu")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Cliente(token).PostAsync("/api/auth/sair", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(token).GetAsync("/api/auth/eu")).StatusCode);

        var id = await api.CriarUsuario(Cargo.OperadorPatio, "desativado.api@teste.local");
        token = await api.Entrar("desativado.api@teste.local", ApiDeTeste.Senha);
        var desativar = await api.Cliente(Cargo.Supervisor).PutAsJsonAsync($"/api/usuarios/{id}/ativo", new { ativo = false });
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente(token).GetAsync("/api/auth/eu")).StatusCode);
    }

    [Fact]
    public async Task Valor_fora_da_lista_e_recusado()
    {
        var supervisor = api.Cliente(Cargo.Supervisor);
        // Tipo de leitor que não existe (a lista do código vai de 1 a 3).
        Assert.Equal(HttpStatusCode.BadRequest, (await supervisor.PostAsJsonAsync("/api/catalogos/leitores", new { idTipoLeitor = 9, codigo = "X-01" })).StatusCode);
        // Cargo inventado.
        Assert.Equal(HttpStatusCode.BadRequest, (await supervisor.PostAsJsonAsync("/api/usuarios",
            new { nome = "Intruso", email = "intruso.api@teste.local", senha = ApiDeTeste.Senha, cargo = "Administrador" })).StatusCode);
        // Senha curta, que o front não deixaria mandar.
        Assert.Equal(HttpStatusCode.BadRequest, (await supervisor.PostAsJsonAsync("/api/usuarios",
            new { nome = "Curta", email = "curta.api@teste.local", senha = "123", cargo = "Tecnico" })).StatusCode);
    }

    [Fact]
    public void Nenhuma_rota_aceita_o_usuario_pelo_corpo()
    {
        // Quem fez a ação sai sempre do token, nunca do que o front manda.
        var suspeitas = Rotas().Where(r => r.Acao is not null)
            .SelectMany(r => r.Acao!.Parameters.Select(p => (r.Nome, p.ParameterType)))
            .Where(x => x.ParameterType.IsClass && x.ParameterType != typeof(string) && x.ParameterType.Namespace?.StartsWith("ViaPermanente") == true)
            .SelectMany(x => x.ParameterType.GetProperties().Where(p => p.Name.StartsWith("IdUsuario") || p.Name == "UsuarioId").Select(p => $"{x.Nome}: {x.ParameterType.Name}.{p.Name}"))
            .Distinct().ToList();
        Assert.True(suspeitas.Count == 0, string.Join("; ", suspeitas));
    }

    [Fact]
    public async Task Toda_resposta_tem_os_cabecalhos_de_seguranca()
    {
        foreach (var resposta in new[] { await api.Cliente().GetAsync("/hc"), await api.Cliente().GetAsync("/api/usuarios") })
        {
            Assert.Equal("nosniff", resposta.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", resposta.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("no-referrer", resposta.Headers.GetValues("Referrer-Policy").Single());
            Assert.Equal("default-src 'none'; frame-ancestors 'none'", resposta.Headers.GetValues("Content-Security-Policy").Single());
        }
    }

    [Fact]
    public async Task A_rota_das_fotos_so_entrega_foto()
    {
        await File.WriteAllBytesAsync(Path.Combine(api.PastaFotos, "foto.jpg"), [0xFF, 0xD8, 0xFF]);
        await File.WriteAllTextAsync(Path.Combine(api.PastaFotos, "segredo.txt"), "não é foto");
        var cliente = api.Cliente(Cargo.Tecnico);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/fotos?caminho=foto.jpg")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/fotos?caminho=segredo.txt")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/fotos?caminho=../../.env")).StatusCode);
    }
}
