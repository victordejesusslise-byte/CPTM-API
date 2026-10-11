using ViaPermanente.Dados;
using ViaPermanente.Dados.Repositories;
using ViaPermanente.Dominio.Dtos.Notas;
using ViaPermanente.Dominio.Dtos.Ofertas;
using ViaPermanente.Dominio.Dtos.Pedidos;
using ViaPermanente.Dominio.Dtos.Usuarios;
using ViaPermanente.Dominio.Models.Compras;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Banco;

/// <summary>Oferta, verificação do pedido e notas no SQL Server de verdade (banco ViaPermanente_Testes).</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class NotaEOfertaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();

    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Oferta_avisa_e_notas_sao_conferidas_com_o_pedido()
    {
        var idUsuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Operador", Email = "operador@teste.local", Senha = "senha-boa-1", Cargo = Cargo.OperadorPatio
        })).Id;
        var fornecedor = new Empresa { Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor, Ativo = true };
        db.Add(fornecedor);
        await db.SaveChangesAsync();
        var ofertas = new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db));
        var pedidos = new PedidoService(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db), ofertas);
        var notas = new NotaFiscalService(new NotaFiscalRepository(db), new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db));

        await ofertas.Registrar(new NovaOfertaDto { IdEmpresa = fornecedor.IdEmpresa, IdEspecie = 1, IdTamanho = 1, VolumeM3Disponivel = 2m });
        List<NovoPedidoItemDto> itens = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 28 }]; // 3,19872 m³
        var verificacao = await pedidos.Verificar(new VerificarPedidoDto { IdEmpresa = fornecedor.IdEmpresa, Itens = itens });
        var pedido = await pedidos.Criar(new NovoPedidoDto { Codigo = "PED-NF-1", IdEmpresa = fornecedor.IdEmpresa, Itens = itens }, idUsuario);

        var primeira = await notas.Registrar(pedido.Id, new NovaNotaDto
        {
            Numero = "1001", Itens = [new() { IdEspecie = 1, IdTamanho = 1, Classe = 1, Quantidade = 14, VolumeM3 = 1.6m }]
        }, idUsuario);
        var segunda = await notas.Registrar(pedido.Id, new NovaNotaDto
        {
            Numero = "1002", Itens = [new() { IdEspecie = 1, IdTamanho = 1, Classe = 1, Quantidade = 20, VolumeM3 = 2.2848m }]
        }, idUsuario);

        Assert.Contains("ofertou 2 m³", Assert.Single(verificacao.Oferta).Mensagem);
        Assert.Equal("Confere", primeira!.StatusConferencia);
        Assert.Contains(primeira.Avisos, a => a.Contains("faltam 14 peças"));
        Assert.Equal("Diverge", segunda!.StatusConferencia);
        Assert.Contains(segunda.Divergencias, d => d.Contains("a nota traz 20 peças e faltam 14"));
        Assert.Equal(2, (await notas.Listar(pedido.Id))!.Count);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => notas.Registrar(pedido.Id, new NovaNotaDto
        {
            Numero = "1001", Itens = [new() { IdEspecie = 1, IdTamanho = 1, Classe = 1, Quantidade = 1, VolumeM3 = 0.11424m }]
        }, idUsuario));
    }
}