using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;

namespace ViaPermanente.Testes.Services;

public class PedidoServiceTestes
{
    private readonly PedidoRepositoryFalso repo = new();

    private readonly OfertaRepositoryFalso ofertas = new();

    private PedidoService Service(string unidade = "m3")
        => new(repo, new CatalogoRepositoryFalso(), new ParametroRepositoryFalso(unidade), new OfertaService(ofertas, repo, new CatalogoRepositoryFalso()));

    /// <summary>8 peças de 2,80 m de 1ª + 2 de 4,80 m de 2ª: 20% em peças, 30% em m³.</summary>
    private static NovoPedidoDto OitoMaisDois(string codigo = "PED-1") => new()
    {
        Codigo = codigo, IdEmpresa = 1, IdContrato = 1,
        Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 8, IdMedida = 1 }, new() { IdTamanho = 4, Classe = 2, Quantidade = 2, IdMedida = 1 }]
    };

    [Fact]
    public async Task Acima_do_limite_em_m3_nao_registra()
    {
        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("m3").Criar(OitoMaisDois(), 1));
        Assert.Contains("30% do m³", erro.Message);
        Assert.Empty(repo.Pedidos);
    }

    [Fact]
    public async Task Mesmo_pedido_registra_quando_o_parametro_conta_pecas()
    {
        var criado = await Service("pecas").Criar(OitoMaisDois(), 1);

        Assert.Equal(10, criado.Quantidade);
        Assert.Equal("Aberto", criado.Situacao);
        Assert.Equal(20m, criado.SegundaClasse.Percentual);
        Assert.Equal("AR00224-03", criado.Contrato);
        Assert.Equal(8 * 0.11424m + 2 * 0.19584m, criado.VolumeM3);
    }

    [Fact]
    public async Task Qualquer_especie_pode_ser_pedida_de_1a_ou_de_2a()
    {
        // A classe vem do pedido (Victor, 2026-10-16): o eucalipto (antes de 2ª) pode ser pedido de 1ª.
        var novo = new NovoPedidoDto { Codigo = "PED-2", IdEmpresa = 1, Itens = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 14, Quantidade = 5, IdMedida = 1 }] };
        Assert.Null(await Record.ExceptionAsync(() => Service().Criar(novo, 1)));
    }

    [Fact]
    public async Task So_fornecedor_recebe_pedido_e_contrato_tem_de_ser_dele()
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("pecas").Criar(new NovoPedidoDto
        {
            Codigo = "PED-3", IdEmpresa = 2, Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 5, IdMedida = 1 }]
        }, 1));
        repo.Empresas.Add(new() { IdEmpresa = 3, Nome = "Outro", Papel = Dominio.Pedidos.Models.PapelEmpresa.Fornecedor, Ativo = true });
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("pecas").Criar(new NovoPedidoDto
        {
            Codigo = "PED-4", IdEmpresa = 3, IdContrato = 1, Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 5, IdMedida = 1 }]
        }, 1));
    }

    [Fact]
    public async Task Codigo_repetido_e_recusado()
    {
        await Service("pecas").Criar(OitoMaisDois(), 1);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("pecas").Criar(OitoMaisDois(), 1));
    }

    [Fact]
    public async Task Cancelar_grava_historico_com_motivo()
    {
        var criado = await Service("pecas").Criar(OitoMaisDois(), 1);

        var cancelado = await Service("pecas").Cancelar(criado.Id, "Fornecedor sem estoque", 1);

        Assert.Equal("Cancelado", cancelado!.Situacao);
        var historico = Assert.Single(repo.HistoricoGravado);
        Assert.Contains("Aberto", historico.RetratoAntes);
        Assert.Contains("Cancelado", historico.RetratoDepois);
    }

    [Fact]
    public async Task Depois_da_chegada_o_pedido_trava()
    {
        var criado = await Service("pecas").Criar(OitoMaisDois(), 1);
        repo.Recebidas[criado.Id] = 4;

        Assert.Equal("RecebidoParcial", (await Service("pecas").BuscarPorId(criado.Id))!.Situacao);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("pecas").Cancelar(criado.Id, "mudou de ideia", 1));
    }

    [Fact]
    public async Task Verificar_mostra_limite_e_oferta_sem_gravar()
    {
        ofertas.Linhas.Add(new() { IdOferta = 1, IdEmpresa = 1, IdEspecie = 1, IdTamanho = 1, VolumeM3Disponivel = 0.5m, Data = new DateOnly(2026, 10, 1) });

        var verificacao = await Service("m3").Verificar(new VerificarPedidoDto { IdEmpresa = 1, Itens = OitoMaisDois().Itens });

        Assert.False(verificacao.SegundaClasse.Valido);
        Assert.Equal(30m, verificacao.SegundaClasse.Percentual);
        Assert.Equal(2, verificacao.Oferta.Count); // 2,80 de 1ª passa do ofertado; 4,80 de 2ª sem oferta
        Assert.Empty(repo.Pedidos);
    }

    [Fact]
    public async Task Correcao_troca_os_itens_e_guarda_o_antes_e_o_depois()
    {
        var criado = await Service("pecas").Criar(OitoMaisDois(), 1);

        var corrigido = await Service("pecas").Corrigir(criado.Id, new CorrigirPedidoDto
        {
            Motivo = "Fornecedor tem mais peças de 1ª", Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 40, IdMedida = 1 }, new() { IdTamanho = 4, Classe = 2, Quantidade = 2, IdMedida = 1 }]
        }, 1);

        Assert.Equal(42, corrigido!.Quantidade);
        Assert.Equal(2, corrigido.Itens.Count);
        var historico = Assert.Single(repo.HistoricoGravado);
        Assert.Contains("\"Quantidade\":10", historico.RetratoAntes);
        Assert.Contains("\"Quantidade\":42", historico.RetratoDepois);
        Assert.Contains("\"Quantidade\":40", historico.RetratoDepois);
    }

    [Fact]
    public async Task Correcao_exige_motivo_respeita_a_segunda_classe_e_trava_na_chegada()
    {
        var criado = await Service("pecas").Criar(OitoMaisDois(), 1);
        var acimaDoLimite = new CorrigirPedidoDto { Motivo = "x", Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 5, IdMedida = 1 }, new() { IdTamanho = 1, Classe = 2, Quantidade = 5, IdMedida = 1 }] };

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("pecas").Corrigir(criado.Id, new CorrigirPedidoDto { Itens = OitoMaisDois().Itens }, 1));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("pecas").Corrigir(criado.Id, acimaDoLimite, 1));
        repo.Recebidas[criado.Id] = 2;
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service("pecas").Corrigir(criado.Id, new CorrigirPedidoDto { Motivo = "tarde", Itens = OitoMaisDois().Itens }, 1));
        Assert.Null(await Service("pecas").Corrigir(999, new CorrigirPedidoDto { Motivo = "x", Itens = OitoMaisDois().Itens }, 1));
    }

    [Theory]
    [InlineData(3, "Escolha um padrão de medida deste fornecedor")] // padrão de outra empresa
    [InlineData(99, "Escolha um padrão de medida deste fornecedor")] // padrão que não existe
    [InlineData(4, "está desativado")]
    [InlineData(2, "é de outra espécie")] // o padrão 2 é só do Eucalyptus; o item é de Ocotea
    public async Task Padrao_de_medida_errado_nao_registra(int idMedida, string mensagem)
    {
        var novo = new NovoPedidoDto { Codigo = "PED-PADRAO", IdEmpresa = 1, Itens = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 1, Quantidade = 5, IdMedida = idMedida }] };
        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() => Service().Criar(novo, 1));
        Assert.Contains(mensagem, erro.Message);
        Assert.Empty(repo.Pedidos);
    }

    [Fact]
    public async Task O_m3_do_item_vem_do_padrao_escolhido()
    {
        // 10 peças de 2,80 m de Eucalyptus no padrão dele (0,22 × 0,16): 10 × 2,80 × 0,22 × 0,16 = 0,9856 m³.
        var novo = new NovoPedidoDto { Codigo = "PED-EUC", IdEmpresa = 1, Itens = [new() { IdTamanho = 1, Classe = 1, IdEspecie = 14, Quantidade = 10, IdMedida = 2 }] };
        var pedido = await Service().Criar(novo, 1);
        var item = Assert.Single(pedido.Itens);
        Assert.Equal(0.9856m, item.VolumeM3);
        Assert.Equal("0,22 × 0,16 m", item.Medida);
    }
}
