using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Armazens;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Chegadas;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Armazens;
using ViaPermanente.Dominio.Armazens.Dtos;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Chegadas;
using ViaPermanente.Dominio.Chegadas.Dtos;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Dtos;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// Guardar o lote inteiro na pilha (Victor, 2026-11-17), no SQL Server de verdade: uma tag lida acha o lote, que aparece separado por
/// tamanho e classe; cada grupo vai para a pilha escolhida, no mesmo armazém, sem ler a tag de cada peça.
/// Pedido PED-LOTE em Santo Amaro (armazém 3): 3 peças de 2,80 m e 2 de 4,00 m, todas de 1ª.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class GuardarLoteNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private int idUsuario, idPedido;
    // As peças cadastradas: id e tag, na ordem (as 3 primeiras de 2,80 m, as 2 últimas de 4,00 m).
    private readonly List<(int Id, string Tag)> pecas = [];

    public async Task InitializeAsync()
    {
        await BancoDeTeste.Recriar(db);
        idUsuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        })).Id;
        // O cadastro da peça pede o módulo do celular cadastrado.
        var idLeitor = (await new CatalogoService(new CatalogoRepository(db)).RegistrarLeitor(new NovoLeitorDto
        {
            IdTipoLeitor = (int)TipoLeitor.ModuloRfid, Codigo = "MOD-01", NumeroSerie = "SN-0001"
        })).Id;
        var fornecedor = new Empresa { Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor, Ativo = true };
        db.Add(fornecedor);
        await db.SaveChangesAsync();
        var padrao = await BancoDeTeste.Padrao(db, fornecedor.IdEmpresa);
        var ofertas = new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db));
        idPedido = (await new PedidoService(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db), ofertas)
            .Criar(new NovoPedidoDto
            {
                Codigo = "PED-LOTE", IdEmpresa = fornecedor.IdEmpresa,
                Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 3, IdMedida = padrao }, new() { IdTamanho = 3, Classe = 1, Quantidade = 2, IdMedida = padrao }]
            }, idUsuario)).Id;

        // Chegada em Santo Amaro: um feixe de cada tamanho; cada peça cadastrada com a tag dela.
        var chegada = (await new ChegadaService(new ChegadaRepository(db), new PedidoRepository(db), new NotaFiscalRepository(db), new CatalogoRepository(db), new LeituraOcrRepository(db))
            .Registrar(idPedido, new NovaChegadaDto
            {
                IdArmazem = 3, QuantidadeRecebida = 5,
                Feixes = [new() { NumeroFeixe = "01", Quantidade = 3, IdEspecie = 1, IdTamanho = 1 },
                          new() { NumeroFeixe = "02", Quantidade = 2, IdEspecie = 1, IdTamanho = 3 }]
            }, idUsuario))!;
        foreach (var (feixe, quantas) in new[] { (chegada.Feixes[0].Id, 3), (chegada.Feixes[1].Id, 2) })
            for (var i = 0; i < quantas; i++)
            {
                db.ChangeTracker.Clear();
                var tag = await Pecas().ProximaTag();
                var peca = (await Pecas().Cadastrar(new NovaPecaDto
                {
                    IdFeixe = feixe, TagFornecedor = "TG-001", TagFornecedorLeitura = MetodoCaptura.Digitado, Metodo = MetodoCaptura.ModuloUhf,
                    Tag = tag, TagRelida = tag, TagsVistas = 1, IdLeitor = idLeitor, IdModo = 1
                }, idUsuario)).Peca!;
                pecas.Add((peca.Id, tag));
            }

        // Verificação: aprova as 3 de 2,80 m e a primeira de 4,00 m; reprova a última de 4,00 m.
        for (var i = 0; i < pecas.Count; i++)
            await Verificar(i, i == 4 ? ResultadoVerificacao.Reprovada : ResultadoVerificacao.Aprovada);
    }

    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private PecaService Pecas() => new(new PecaRepository(db), new CatalogoRepository(db));
    private ArmazemService Armazem() { db.ChangeTracker.Clear(); return new(new PecaRepository(db), new CatalogoRepository(db), new LeituraService(new PecaRepository(db)), Pecas(), new ArmazemRepository(db)); }
    private async Task<int> Pilha(string codigo) => (await db.Set<Pilha>().AsNoTracking().SingleAsync(p => p.IdArmazem == 3 && p.Codigo == codigo)).IdPilha;

    private async Task Verificar(int i, ResultadoVerificacao resultado)
    {
        db.ChangeTracker.Clear();
        await new QualidadeService(new PecaRepository(db), new LeituraService(new PecaRepository(db)), Pecas()).Verificar(pecas[i].Id, new VerificarPecaDto
        {
            Leitura = new LeituraDto { Tag = pecas[i].Tag, TagsVistas = 1, IdModo = 1 },
            AtaqueBiologico = AtaqueBiologicoVerificacao.Nenhum,
            Rachadura = resultado == ResultadoVerificacao.Reprovada ? RachaduraVerificacao.Grave : RachaduraVerificacao.Nenhuma,
            PlacaAntirrachante = true, PlacaConfere = true, Resultado = resultado,
            Motivo = resultado == ResultadoVerificacao.Reprovada ? "Rachadura grave" : null, FotoArquivo = resultado == ResultadoVerificacao.Reprovada ? "fotos/r.jpg" : null
        }, idUsuario);
    }

    [Fact]
    public async Task A_tag_lida_acha_o_lote_separado_por_tamanho_e_classe()
    {
        // Uma de 2,80 m já foi guardada sozinha, como hoje.
        await Armazem().Guardar(pecas[0].Id, new GuardarPecaDto { Leitura = new() { Tag = pecas[0].Tag } }, idUsuario);

        var lote = (await Armazem().LoteDaTag(pecas[1].Tag))!;

        Assert.Equal("L-PED-LOTE", lote.CodigoLote);
        Assert.Equal(pecas[1].Id, lote.IdDormenteLido);
        Assert.Equal(3, lote.IdArmazem);
        Assert.Equal(5, lote.Pecas);
        var curtas = Assert.Single(lote.Grupos, g => g.IdTamanho == 1);
        Assert.Equal((2.80m, (byte)1, 2, 1, 0), (curtas.ComprimentoM, curtas.Classe, curtas.AGuardar, curtas.JaNaPilha, curtas.NaoAprovadas));
        var longas = Assert.Single(lote.Grupos, g => g.IdTamanho == 3);
        Assert.Equal((4.00m, 1, 0, 1), (longas.ComprimentoM, longas.AGuardar, longas.JaNaPilha, longas.NaoAprovadas));
    }

    [Fact]
    public async Task Tag_sem_peca_nao_acha_lote()
        => Assert.Null(await Armazem().LoteDaTag("BR9999999999"));

    [Fact]
    public async Task Guardar_o_lote_poe_cada_grupo_na_pilha_escolhida_sem_ler_cada_tag()
    {
        var (a01, c01) = (await Pilha("A01"), await Pilha("C01"));

        var depois = await Armazem().GuardarLote(new GuardarLoteDto
        {
            IdArmazem = 3, Leitura = new() { Tag = pecas[1].Tag },
            Grupos = [new() { IdTamanho = 1, Classe = 1, IdPilha = a01 }, new() { IdTamanho = 3, Classe = 1, IdPilha = c01 }]
        }, idUsuario);

        // As 4 aprovadas entram; a reprovada fica de fora.
        var lugares = await db.Set<Dormente>().AsNoTracking().Where(d => pecas.Select(p => p.Id).Contains(d.IdDormente))
            .OrderBy(d => d.IdDormente).Select(d => d.IdPilha).ToListAsync();
        Assert.Equal([a01, a01, a01, c01, null], lugares);
        Assert.All(depois.Grupos, g => Assert.Equal(0, g.AGuardar));
        // Uma movimentação de entrada por peça; só a da peça lida tem a leitura.
        var entradas = await db.Set<Movimentacao>().AsNoTracking().Where(m => m.Tipo == TipoMovimentacao.Entrada).ToListAsync();
        Assert.Equal(4, entradas.Count);
        Assert.Equal(pecas[1].Id, Assert.Single(entradas, m => m.IdLeitura != null).IdDormente);
    }

    [Fact]
    public async Task Guardar_o_lote_recusa_pilha_errada_e_tag_de_fora()
    {
        var b01 = await Pilha("B01"); // 3,00 m de 1ª: outro tamanho
        var outroArmazem = (await db.Set<Pilha>().AsNoTracking().SingleAsync(p => p.IdArmazem == 2 && p.Codigo == "A01")).IdPilha;

        var tamanho = await Assert.ThrowsAsync<RegraDeNegocioException>(() => Armazem().GuardarLote(new GuardarLoteDto
        {
            IdArmazem = 3, Leitura = new() { Tag = pecas[1].Tag }, Grupos = [new() { IdTamanho = 1, Classe = 1, IdPilha = b01 }]
        }, idUsuario));
        Assert.Contains("B01", tamanho.Message);
        var armazem = await Assert.ThrowsAsync<RegraDeNegocioException>(() => Armazem().GuardarLote(new GuardarLoteDto
        {
            IdArmazem = 3, Leitura = new() { Tag = pecas[1].Tag }, Grupos = [new() { IdTamanho = 1, Classe = 1, IdPilha = outroArmazem }]
        }, idUsuario));
        Assert.Contains("armazém", armazem.Message);
        var a01 = await Pilha("A01");
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Armazem().GuardarLote(new GuardarLoteDto
        {
            IdArmazem = 3, Leitura = new() { Tag = "BR9999999999" }, Grupos = [new() { IdTamanho = 1, Classe = 1, IdPilha = a01 }]
        }, idUsuario));
        Assert.Equal(0, await db.Set<Movimentacao>().CountAsync());
    }
}
