using ViaPermanente.Dados;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Testes.Banco;

[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class CadastroNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Empresas_contratos_estacoes_e_trechos()
    {
        var s = new CadastroService(new CadastroRepository(db));
        var tg = await s.NovaEmpresa(new NovaEmpresaDto { Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor });
        var ufv = await s.NovaEmpresa(new NovaEmpresaDto { Nome = "UFV", Papel = PapelEmpresa.Inspetora });
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.NovaEmpresa(new NovaEmpresaDto { Nome = "tg–três guri", Papel = PapelEmpresa.Fornecedor }));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.NovoContrato(new NovoContratoDto { IdEmpresa = ufv.Id, NumeroAta = "A", NumeroContrato = "C" }));
        Assert.Equal("TG–Três Guri", (await s.NovoContrato(new NovoContratoDto { IdEmpresa = tg.Id, NumeroAta = "AR00224", NumeroContrato = "AR00224-03" })).Empresa);
        Assert.Single(await s.Empresas(PapelEmpresa.Fornecedor));

        var vo = await s.NovaEstacao(new NovaEstacaoDto { Nome = "Vila Olímpia", Km = 213.120m, Ordem = 5 });
        var be = await s.NovaEstacao(new NovaEstacaoDto { Nome = "Berrini", Km = 214.300m, Ordem = 6 });
        var sa = await s.NovaEstacao(new NovaEstacaoDto { Nome = "Santo Amaro", Km = 219.000m, Ordem = 8 });
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.NovoTrecho(new NovoTrechoDto { IdEstacaoInicio = vo.Id, IdEstacaoFim = sa.Id, ClasseExigida = 1 }));
        var trecho = await s.NovoTrecho(new NovoTrechoDto { IdEstacaoInicio = vo.Id, IdEstacaoFim = be.Id, ClasseExigida = 1 });
        Assert.Equal(("Vila Olímpia", "Berrini"), (trecho.EstacaoInicio, trecho.EstacaoFim));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.NovoTrecho(new NovoTrechoDto { IdEstacaoInicio = vo.Id, IdEstacaoFim = be.Id, ClasseExigida = 2 }));

        Assert.True(await s.AtivarEmpresa(ufv.Id, false));
        Assert.False((await s.Empresas(PapelEmpresa.Inspetora)).Single().Ativo);
    }
}