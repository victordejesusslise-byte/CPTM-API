using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>A classificação da madeira no SQL Server: corrige sem apagar a da placa, e a peça passa a mostrar a espécie nova.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class ClassificacaoNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Corrige_a_especie_sem_apagar_a_da_placa_e_acha_a_peca_pelo_codigo()
    {
        var usuario = await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Especialista de teste", Email = "especialista@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Especialista
        });
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(usuario.Id);
        var peca = await db.Set<Dormente>().OrderBy(d => d.IdDormente).FirstAsync();
        var tag = await db.Set<Rfid>().Where(r => r.IdDormente == peca.IdDormente).Select(r => r.TagRfid).FirstAsync();
        var outra = await db.Set<Especie>().FirstAsync(e => e.NomeCientifico == "Hymenaea courbaril");
        var pecas = new PecaService(new PecaRepository(db), new CatalogoRepository(db));
        var servico = new ClassificacaoService(new ClassificacaoRepository(db), new PecaRepository(db));

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.Classificar(new NovaClassificacaoDto
        {
            IdDormente = peca.IdDormente, IdEspecie = outra.IdEspecie, Leitura = new LeituraDto { Tag = "BR9999999999" }
        }, usuario.Id));
        var nova = await servico.Classificar(new NovaClassificacaoDto
        {
            IdDormente = peca.IdDormente, IdEspecie = outra.IdEspecie, Metodo = MetodoClassificacao.Laboratorio, Leitura = new LeituraDto { Tag = tag }
        }, usuario.Id);

        var historico = await servico.DaPeca(peca.IdDormente);
        Assert.Equal(2, historico.Count);
        Assert.Equal(historico[1].Id, nova.Anterior);
        Assert.Single(await servico.Recentes(30));
        var aberta = await pecas.BuscarPorId(peca.IdDormente);
        Assert.Equal(outra.IdEspecie, aberta!.IdEspecie);
        Assert.Equal(aberta.Id, (await pecas.BuscarPorCodigo(aberta.Codigo))!.Id);
        Assert.Null(await pecas.BuscarPorCodigo(aberta.Codigo.Replace("-E", "-E9")));
    }
}
