using Microsoft.EntityFrameworkCore;
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

    /// <summary>A sigla da empresa (Victor, 2026-11-14) monta a tag do fornecedor: opcional, de 2 a 6 letras ou números, sempre em maiúsculas.</summary>
    [Fact]
    public async Task A_sigla_da_empresa_vai_em_maiusculas_e_e_conferida()
    {
        var s = new CadastroService(new CadastroRepository(db), new CatalogoRepository(db));
        var tg = await s.NovaEmpresa(new NovaEmpresaDto
        {
            Nome = "TG–Três Guri", Sigla = " tg ", Papel = PapelEmpresa.Fornecedor, Medidas = [new() { LarguraM = 0.24m, AlturaM = 0.17m }]
        });
        Assert.Equal("TG", tg.Sigla);
        Assert.Equal("TG", (await s.Empresas(PapelEmpresa.Fornecedor)).Single().Sigla);
        Assert.Null((await s.NovaEmpresa(new NovaEmpresaDto { Nome = "UFV", Sigla = "  ", Papel = PapelEmpresa.Inspetora })).Sigla);
        foreach (var ruim in new[] { "T", "TGTGTGT", "T-G", "TÊ" })
        {
            var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
                s.NovaEmpresa(new NovaEmpresaDto { Nome = "Outra " + ruim, Sigla = ruim, Papel = PapelEmpresa.Inspetora }));
            Assert.Contains("2 a 6 letras ou números", erro.Message);
        }

        // A sigla muda depois: a empresa que veio de antes, sem sigla no começo do nome, ganha uma; vazia, fica sem.
        Assert.Equal("TGR", (await s.MudarSigla(tg.Id, "tgr"))!.Sigla);
        Assert.Null((await s.MudarSigla(tg.Id, ""))!.Sigla);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => s.MudarSigla(tg.Id, "T G"));
        Assert.Null(await s.MudarSigla(999999, "TG"));

        // O banco também só aceita de 2 a 6 letras ou números em maiúsculas.
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("UPDATE EMPRESA SET sigla = 'tg'"));
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("UPDATE EMPRESA SET sigla = 'T-G'"));
    }
}