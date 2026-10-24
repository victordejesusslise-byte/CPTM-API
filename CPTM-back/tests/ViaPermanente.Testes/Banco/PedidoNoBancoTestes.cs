using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>O pedido no SQL Server de verdade (banco ViaPermanente_Testes, criado e apagado).</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class PedidoNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private PedidoService Pedidos() => new(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db),
        new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db)));

    [Fact]
    public async Task Cria_lista_e_cancela_com_historico()
    {
        var idUsuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Operador", Email = "operador@teste.local", Senha = "senha-boa-1", Cargo = Cargo.OperadorPatio
        })).Id;
        var fornecedor = new Empresa { Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor, Ativo = true };
        db.Add(fornecedor);
        await db.SaveChangesAsync();
        var padrao = await BancoDeTeste.Padrao(db, fornecedor.IdEmpresa);
        var service = Pedidos();

        var criado = await service.Criar(new NovoPedidoDto
        {
            Codigo = "PED-BANCO-1", IdEmpresa = fornecedor.IdEmpresa,
            Itens = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 24, IdMedida = padrao }, new() { IdTamanho = 1, Classe = 2, Quantidade = 4, IdMedida = padrao }]
        }, idUsuario);
        var corrigido = await service.Corrigir(criado.Id, new CorrigirPedidoDto
        {
            Motivo = "Mais peças de 1ª", Itens = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 30, IdMedida = padrao }, new() { IdTamanho = 1, Classe = 2, Quantidade = 4, IdMedida = padrao }]
        }, idUsuario);
        Assert.Equal(34, corrigido!.Quantidade);
        Assert.Equal(2, await db.Set<PedidoItem>().CountAsync(i => i.IdPedido == criado.Id));
        var cancelado = await service.Cancelar(criado.Id, "Teste de cancelamento", idUsuario);

        Assert.Equal(28, criado.Quantidade);
        Assert.Equal(14.3m, criado.SegundaClasse.Percentual);
        Assert.Equal("Ocotea neesiana", criado.Itens[0].Especie);
        Assert.Single(await service.Listar());
        Assert.Equal("Cancelado", cancelado!.Situacao);
        Assert.Equal(2, (await service.Historico(criado.Id))!.Count); // a correção e o cancelamento
    }

    [Fact]
    public async Task Pedido_com_chegada_nao_cancela()
    {
        var idUsuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        })).Id;
        var exemplo = await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(idUsuario);
        db.ChangeTracker.Clear();
        var service = Pedidos();

        Assert.Equal("Recebido", (await service.BuscarPorId(exemplo.IdPedido))!.Situacao);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => service.Cancelar(exemplo.IdPedido, "não pode", idUsuario));
    }
}