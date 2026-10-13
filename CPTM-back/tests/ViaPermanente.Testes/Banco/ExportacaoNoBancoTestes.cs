using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Ambiental;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Integracoes;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Ambiental;
using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Integracoes;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class ExportacaoNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Exporta_o_relatorio_do_ibama_e_o_estoque_do_erp()
    {
        var id = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto { Nome = "S", Email = "s@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor })).Id;
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(id);
        var relatorios = new RelatorioAmbientalService(new RelatorioAmbientalRepository(db), new CatalogoRepository(db));
        var relatorio = await relatorios.Gerar(DateOnly.FromDateTime(DateTime.Now), id);
        var exportacao = new ExportacaoService(new PlanilhaExportacaoRepository(), new IntegracaoLogRepository(db), relatorios, BancoDeTeste.Pecas(db), new CatalogoRepository(db));
        await exportacao.NovoCodigoMaterial(new NovoCodigoMaterialDto { IdTamanho = 1, Classe = 1, CodigoErp = "MAT-280-1" });
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => exportacao.NovoCodigoMaterial(new NovoCodigoMaterialDto { IdTamanho = 1, Classe = 1, CodigoErp = "OUTRO" }));

        var ibama = (await exportacao.Ibama(relatorio.Id))!;
        var erp = await exportacao.Erp();

        using var folhaIbama = new XLWorkbook(new MemoryStream(ibama.Conteudo));
        Assert.Equal(3, folhaIbama.Worksheets.First().RowsUsed().Count()); // cabeçalho + 2 espécies
        using var folhaErp = new XLWorkbook(new MemoryStream(erp.Conteudo));
        var linhas = folhaErp.Worksheets.First().RowsUsed().Skip(1).ToList();
        Assert.Equal(26, linhas.Count);
        Assert.All(linhas, l => Assert.Equal("MAT-280-1", l.Cell(1).GetString()));
        Assert.Equal(2, await db.Set<IntegracaoLog>().CountAsync(l => l.Direcao == DirecaoIntegracao.Saida));
        Assert.Null(await exportacao.Ibama(999));
    }
}