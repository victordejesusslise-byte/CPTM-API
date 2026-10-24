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
        var s = new CadastroService(new CadastroRepository(db), new CatalogoRepository(db));
        // O fornecedor entra com os padrões de medida dele (pelo menos um).
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.NovaEmpresa(new NovaEmpresaDto { Nome = "Sem Padrão", Papel = PapelEmpresa.Fornecedor }));
        var tg = await s.NovaEmpresa(new NovaEmpresaDto
        {
            Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor,
            Medidas = [new() { LarguraM = 0.24m, AlturaM = 0.17m }, new() { IdEspecie = 14, LarguraM = 0.22m, AlturaM = 0.16m }]
        });
        Assert.Equal(["0,24 × 0,17 m", "0,22 × 0,16 m"], tg.Medidas.Select(m => m.Rotulo));
        Assert.Equal((await new CatalogoRepository(db).ListarEspecies()).Single(e => e.IdEspecie == 14).NomeCientifico, tg.Medidas[1].Especie);
        var outro = await s.NovaMedida(tg.Id, new NovaMedidaDto { LarguraM = 0.25m, AlturaM = 0.18m });
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.NovaMedida(tg.Id, new NovaMedidaDto { LarguraM = 0.25m, AlturaM = 0.18m }));
        Assert.True(await s.AtivarMedida(outro!.Id, false));
        Assert.False((await s.Empresas(PapelEmpresa.Fornecedor)).Single().Medidas.Single(m => m.Id == outro.Id).Ativo);
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