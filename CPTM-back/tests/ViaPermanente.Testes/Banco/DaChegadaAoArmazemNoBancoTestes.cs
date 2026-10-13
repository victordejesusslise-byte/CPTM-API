using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Armazens;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Chegadas;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pedidos;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Armazens;
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
/// A cadeia da peça no SQL Server de verdade: chegada → cadastro com a tag → verificação de qualidade → pilha.
/// Pedido de 4 peças de 2,80 m de 1ª, em dois feixes de 2 (Ocotea neesiana e Hymenaea courbaril), em Santo Amaro.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class DaChegadaAoArmazemNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private int idUsuario, idLeitor, idPedido;

    public async Task InitializeAsync()
    {
        await BancoDeTeste.Recriar(db);
        idUsuario = (await new UsuarioService(new UsuarioRepository(db)).Criar(new NovoUsuarioDto
        {
            Nome = "Supervisor", Email = "supervisor@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor
        })).Id;
        idLeitor = (await new CatalogoService(new CatalogoRepository(db)).RegistrarLeitor(new NovoLeitorDto
        {
            IdTipoLeitor = 2, Codigo = "MOD-01", NumeroSerie = "SN-0001"
        })).Id;
        var fornecedor = new Empresa { Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor, Ativo = true };
        db.Add(fornecedor);
        await db.SaveChangesAsync();
        var ofertas = new OfertaService(new OfertaRepository(db), new PedidoRepository(db), new CatalogoRepository(db));
        idPedido = (await new PedidoService(new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db), ofertas)
            .Criar(new NovoPedidoDto { Codigo = "PED-CADEIA", IdEmpresa = fornecedor.IdEmpresa, Itens = [new() { IdTamanho = 1, Classe = 1, Quantidade = 4 }] }, idUsuario)).Id;
    }

    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private ChegadaService Chegadas() => new(new ChegadaRepository(db), new PedidoRepository(db), new NotaFiscalRepository(db), new CatalogoRepository(db));
    private PecaService Pecas() => new(new PecaRepository(db), new CatalogoRepository(db));
    private QualidadeService Qualidade() => new(new PecaRepository(db), new LeituraService(new PecaRepository(db)), Pecas());
    private ArmazemService Armazem() => new(new PecaRepository(db), new CatalogoRepository(db), new LeituraService(new PecaRepository(db)), Pecas(), new ArmazemRepository(db));

    private async Task<ChegadaDto> Chegar(int informada = 4)
    {
        var nota = await new NotaFiscalService(new NotaFiscalRepository(db), new PedidoRepository(db), new CatalogoRepository(db), new ParametroRepository(db))
            .Registrar(idPedido, new NovaNotaDto
            {
                Numero = "NF-1", Itens = [new() { IdEspecie = 1, IdTamanho = 1, Classe = 1, Quantidade = 2, VolumeM3 = 0.22848m },
                                          new() { IdEspecie = 3, IdTamanho = 1, Classe = 1, Quantidade = 2, VolumeM3 = 0.22848m }]
            }, idUsuario);
        return (await Chegadas().Registrar(idPedido, new NovaChegadaDto
        {
            IdNota = nota!.Id, IdArmazem = 3, DocumentoOrigemTipo = "DOF", DocumentoOrigemNumero = "123", QuantidadeRecebida = informada,
            Feixes = [new() { NumeroFeixe = "01", Quantidade = 2, IdEspecie = 1, IdTamanho = 1, Tratamento = "CCA" },
                      new() { NumeroFeixe = "02", Quantidade = 2, IdEspecie = 3, IdTamanho = 1, Tratamento = "CCA" }]
        }, idUsuario))!;
    }

    private async Task<CadastroPecaDto> CadastrarComModulo(int idFeixe, int tagsVistas = 1, string? relida = "igual")
    {
        db.ChangeTracker.Clear();
        var tag = await Pecas().ProximaTag();
        return await Pecas().Cadastrar(new NovaPecaDto
        {
            IdFeixe = idFeixe, TagFornecedor = "TG-001", TagFornecedorLeitura = MetodoCaptura.Digitado, Metodo = MetodoCaptura.ModuloUhf,
            Tag = tag, TagRelida = relida == "igual" ? tag : relida, TagsVistas = tagsVistas, IdLeitor = idLeitor, IdModo = 1
        }, idUsuario);
    }

    private VerificarPecaDto Verificacao(string tag, ResultadoVerificacao resultado, string? motivo = null, string? foto = null, int tagsVistas = 1) => new()
    {
        Leitura = new LeituraDto { Tag = tag, TagsVistas = tagsVistas, IdLeitor = idLeitor, IdModo = 1 },
        AtaqueBiologico = AtaqueBiologicoVerificacao.Nenhum, Rachadura = resultado == ResultadoVerificacao.Reprovada ? RachaduraVerificacao.Grave : RachaduraVerificacao.Nenhuma,
        PlacaAntirrachante = true, PlacaConfere = true, Resultado = resultado, Motivo = motivo, FotoArquivo = foto
    };

    [Fact]
    public async Task Chegada_cria_o_lote_e_confere_ou_diverge()
    {
        var certa = await Chegar();

        Assert.True(certa.Confere);
        Assert.Equal("L-PED-CADEIA", certa.Lote);
        Assert.Equal("Santo Amaro", certa.Armazem);
        Assert.All(certa.Feixes, f => Assert.Equal(0, f.Cadastradas));
        Assert.Contains("Pedido: 4 de 1ª e 0 de 2ª classe; chegaram até aqui 4 de 1ª e 0 de 2ª.", certa.Avisos);

        var excesso = await Chegadas().Registrar(idPedido, new NovaChegadaDto
        {
            IdArmazem = 3, QuantidadeRecebida = 3, Feixes = [new() { NumeroFeixe = "03", Quantidade = 2, IdEspecie = 1, IdTamanho = 1 }]
        }, idUsuario);
        Assert.False(excesso!.Confere);
        Assert.Contains("os feixes somam 2", excesso.Divergencias[0]);
        Assert.Contains(excesso.Divergencias, d => d.Contains("já chegaram 6 peças de 4 pedidas"));
        Assert.Equal(1, await db.Set<Lote>().CountAsync());
    }

    [Fact]
    public async Task Cadastro_so_vincula_com_alvo_unico_e_releitura_igual()
    {
        var feixe = (await Chegar()).Feixes[0];

        var ok = await CadastrarComModulo(feixe.Id);
        var bloqueada = await CadastrarComModulo(feixe.Id, tagsVistas: 2);
        var divergente = await CadastrarComModulo(feixe.Id, relida: "BR9999999999");
        var outra = await CadastrarComModulo(feixe.Id);

        Assert.Equal("Ok", ok.Resultado);
        Assert.Equal("BR0000000001", ok.Peca!.Tag);
        Assert.Equal("AguardandoVerificacao", ok.Peca.Situacao);
        Assert.Equal("Ocotea neesiana", ok.Peca.Especie);
        Assert.StartsWith("DM-E", ok.Peca.Codigo);
        Assert.Equal("Bloqueada", bloqueada.Resultado);
        Assert.Null(bloqueada.Peca);
        Assert.Equal("Divergente", divergente.Resultado);  // gastou o BR0000000002
        Assert.Equal("BR0000000003", outra.Peca!.Tag);
        Assert.Equal(4, await db.Set<GravacaoTag>().CountAsync());
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => CadastrarComModulo(feixe.Id)); // feixe de 2 já completo
        Assert.Equal(1, await db.Set<ClassificacaoMadeira>().CountAsync(c => c.Metodo == MetodoClassificacao.PlacaFornecedor && c.IdDormente == ok.Peca.Id));
    }

    [Fact]
    public async Task Cadastro_com_a_pistola_le_a_tag_impressa()
    {
        var feixe = (await Chegar()).Feixes[1];
        var r = await Pecas().Cadastrar(new NovaPecaDto { IdFeixe = feixe.Id, Metodo = MetodoCaptura.Pistola, Tag = "PST-TAG-77", IdLeitor = idLeitor }, idUsuario);

        Assert.Equal("Hymenaea courbaril", r.Peca!.Especie);
        Assert.Equal(r.Peca.Id, (await Pecas().BuscarPorTag("PST-TAG-77"))!.Id);
        Assert.Equal(1, await db.Set<LeituraDormente>().CountAsync(l => l.MetodoCaptura == MetodoCaptura.Pistola));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Pecas().Cadastrar(new NovaPecaDto { IdFeixe = feixe.Id, Metodo = MetodoCaptura.Pistola, Tag = "PST-TAG-77" }, idUsuario));
    }

    [Fact]
    public async Task Verificacao_e_estoque_peca_a_peca()
    {
        var feixe = (await Chegar()).Feixes[0];
        var aprovada = (await CadastrarComModulo(feixe.Id)).Peca!;
        var reprovada = (await CadastrarComModulo(feixe.Id)).Peca!;

        // Peca_sem_verificacao_nao_aparece_no_estoque
        Assert.Empty(await Pecas().Estoque());
        // a leitura precisa ser desta peça e com alvo único; as duas ficam gravadas
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Qualidade().Verificar(aprovada.Id, Verificacao(reprovada.Tag!, ResultadoVerificacao.Aprovada), idUsuario));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Qualidade().Verificar(aprovada.Id, Verificacao(aprovada.Tag!, ResultadoVerificacao.Aprovada, tagsVistas: 2), idUsuario));
        Assert.Equal(1, await db.Set<LeituraDormente>().CountAsync(l => l.Ambigua && l.IdDormente == null));
        // Reprovar_sem_motivo_ou_sem_foto_falha
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Qualidade().Verificar(reprovada.Id, Verificacao(reprovada.Tag!, ResultadoVerificacao.Reprovada, motivo: "Rachadura"), idUsuario));

        var ok = await Qualidade().Verificar(aprovada.Id, Verificacao(aprovada.Tag!, ResultadoVerificacao.Aprovada), idUsuario);
        var ruim = await Qualidade().Verificar(reprovada.Id, Verificacao(reprovada.Tag!, ResultadoVerificacao.Reprovada, "Rachadura grave no topo", "fotos/p2.jpg"), idUsuario);

        Assert.Equal("Aprovada", ok!.Situacao);
        // Peca_reprovada_desativa_a_tag_e_nao_aparece_no_estoque
        Assert.Equal("Reprovada", ruim!.Situacao);
        Assert.Null(ruim.Tag);
        Assert.False((await db.Set<Rfid>().AsNoTracking().SingleAsync(r => r.IdDormente == reprovada.Id)).Ativo);
        Assert.Equal([aprovada.Id], (await Pecas().Estoque()).Select(p => p.Id));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Qualidade().Verificar(aprovada.Id, Verificacao(aprovada.Tag!, ResultadoVerificacao.Aprovada), idUsuario));
    }

    [Fact]
    public async Task So_a_aprovada_entra_na_pilha_do_tamanho_e_da_classe()
    {
        var feixe = (await Chegar()).Feixes[0];
        var peca = (await CadastrarComModulo(feixe.Id)).Peca!;
        var leitura = new LeituraDto { Tag = peca.Tag!, IdLeitor = idLeitor, IdModo = 1 };

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Armazem().Guardar(peca.Id, new GuardarPecaDto { Leitura = leitura }, idUsuario));
        await Qualidade().Verificar(peca.Id, Verificacao(peca.Tag!, ResultadoVerificacao.Aprovada), idUsuario);
        var pilhaDe300 = await db.Set<Pilha>().FirstAsync(p => p.IdArmazem == 3 && p.Codigo == "B01");
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Armazem().Guardar(peca.Id, new GuardarPecaDto { Leitura = leitura, IdPilha = pilhaDe300.IdPilha }, idUsuario));

        var guardada = await Armazem().Guardar(peca.Id, new GuardarPecaDto { Leitura = leitura }, idUsuario);

        Assert.Equal("NoArmazem", guardada!.Situacao);
        Assert.Equal("A01", guardada.Pilha);
        Assert.Equal("Santo Amaro", guardada.Armazem);
        Assert.Equal(1, await db.Set<Movimentacao>().CountAsync(m => m.IdDormente == peca.Id && m.Tipo == TipoMovimentacao.Entrada && m.IdLeitura != null));
        Assert.Equal(guardada.Id, (await Pecas().Estoque(idArmazem: 3)).Single().Id);
    }
}