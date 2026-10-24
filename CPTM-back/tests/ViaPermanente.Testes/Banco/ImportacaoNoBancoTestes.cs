using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Integracoes;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Ambiental.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Integracoes;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>Importação da nota pela planilha (modelo provisório) no SQL Server de verdade.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class ImportacaoNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private static MemoryStream Planilha(params object[][] linhas)
    {
        using var livro = new XLWorkbook(new MemoryStream(new PlanilhaNotaRepository().Modelo()));
        var folha = livro.Worksheets.First();
        for (var i = 0; i < linhas.Length; i++)
            for (var j = 0; j < linhas[i].Length; j++)
                folha.Cell(i + 2, j + 1).Value = XLCellValue.FromObject(linhas[i][j]);
        var saida = new MemoryStream();
        livro.SaveAs(saida);
        saida.Position = 0;
        return saida;
    }

    [Fact]
    public async Task Planilha_de_nota_gera_nota_e_itens_e_linha_invalida_vai_para_o_log()
    {
        var idUsuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto { Nome = "O", Email = "o@teste.local", Senha = "senha-boa-1", Cargo = Cargo.OperadorPatio })).Id;
        var fornecedor = new Empresa { Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor, Ativo = true };
        db.Add(fornecedor);
        await db.SaveChangesAsync();
        var padrao = await BancoDeTeste.Padrao(db, fornecedor.IdEmpresa);
        var ofertas = new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db));
        await new PedidoService(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db), ofertas)
            .Criar(new NovoPedidoDto { Codigo = "PED-PL", IdEmpresa = fornecedor.IdEmpresa, Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 28, IdMedida = padrao }] }, idUsuario);
        var notas = new NotaFiscalService(new NotaFiscalRepository(db), new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db));
        var importacao = new ImportacaoNotaService(new PlanilhaNotaRepository(), new IntegracaoLogRepository(db), new PedidoRepository(db), new CatalogoRepository(db), notas);

        var resultado = await importacao.Importar(Planilha(
            ["PED-PL", "NF-10", new DateTime(2026, 10, 5), 15000, "Ocotea neesiana", 2.8, 1, 14, 1.59936],
            ["PED-PL", "NF-10", new DateTime(2026, 10, 5), 15000, "Hymenaea courbaril", 2.8, 1, 14, 1.59936],
            ["PED-PL", "NF-11", new DateTime(2026, 10, 6), 900, "Madeira desconhecida", 2.8, 1, 2, 0.2],
            ["PED-XX", "NF-12", new DateTime(2026, 10, 6), 900, "Ocotea neesiana", 2.8, 1, 2, 0.2]), "notas-outubro.xlsx", idUsuario);

        var nota = Assert.Single(resultado.Notas);
        Assert.Equal(("NF-10", "Confere"), (nota.Numero, nota.StatusConferencia));
        Assert.Equal(2, resultado.Erros.Count);
        Assert.Contains(resultado.Erros, e => e.Contains("Linha 4") && e.Contains("Madeira desconhecida"));
        Assert.Contains(resultado.Erros, e => e.Contains("PED-XX não existe"));
        var gravada = await db.Set<NotaFiscal>().AsNoTracking().SingleAsync();
        Assert.Equal((OrigemNota.Planilha, "notas-outubro.xlsx"), (gravada.Origem, gravada.Arquivo));
        Assert.Equal(2, await db.Set<NotaFiscalItem>().CountAsync());
        Assert.Equal((1, 2), (await db.Set<IntegracaoLog>().CountAsync(l => l.Status == StatusIntegracao.Ok), await db.Set<IntegracaoLog>().CountAsync(l => l.Status == StatusIntegracao.Erro)));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => importacao.Importar(new MemoryStream([1, 2, 3]), "lixo.xlsx", idUsuario));
    }
}