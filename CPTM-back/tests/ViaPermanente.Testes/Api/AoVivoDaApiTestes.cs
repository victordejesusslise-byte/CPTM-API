using System.Net;
using System.Net.Http.Json;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Api;

/// <summary>O que o app Android usa além das telas (Etapa 4): o plano da noite, a versão e o ao vivo, todos só com login.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class AoVivoDaApiTestes(ApiDeTeste api) : IClassFixture<ApiDeTeste>
{
    private const string Negociar = "/hub/campo/negotiate?negotiateVersion=1";

    [Fact]
    public async Task O_plano_da_noite_e_a_versao_exigem_login()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente().GetAsync("/api/campo/plano")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente().GetAsync("/api/app/versao")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente().GetAsync("/api/campo/linha")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente(Cargo.OperadorVia).GetAsync("/api/campo/linha?limite=10")).StatusCode);

        var plano = await api.Cliente(Cargo.OperadorVia).GetFromJsonAsync<PlanoLido>("/api/campo/plano");
        Assert.NotNull(plano!.Trocas);
        Assert.NotNull(plano.Alertas);
        var versao = await api.Cliente(Cargo.OperadorVia).GetFromJsonAsync<VersaoLida>("/api/app/versao");
        Assert.False(string.IsNullOrWhiteSpace(versao!.Versao));
    }

    [Fact]
    public async Task O_ao_vivo_so_conecta_com_o_token()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente().PostAsync(Negociar, null)).StatusCode);
        // O WebSocket não leva cabeçalho: o token vai na consulta, e só vale nas rotas /hub.
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente().PostAsync($"{Negociar}&access_token={api.Token(Cargo.OperadorVia)}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente().GetAsync($"/api/campo/plano?access_token={api.Token(Cargo.OperadorVia)}")).StatusCode);
    }

    private record PlanoLido(DateOnly Noite, List<object> Trocas, List<object> Alertas);
    private record VersaoLida(string Versao, string Baixar);
}
