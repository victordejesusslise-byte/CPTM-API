using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// O lote de troca (Victor, 2026-11-11) no SQL Server de verdade. As peças vêm do exemplo da apresentação (todas de 2,80 m de 1ª, na pilha
/// A01 de Santo Amaro); as peças 2, 3 e 4 são colocadas na via, num trecho de 1ª classe, nos km 219,402 a 219,404.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class LoteTrocaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private int supervisor, tecnico, patio, via, idTrecho;
    private readonly int idTamanho = TamanhoDormente.Todos.Single(t => t.ComprimentoM == 2.80m).IdTamanho;

    public async Task InitializeAsync()
    {
        await BancoDeTeste.Recriar(db);
        var usuarios = new UsuarioService(new UsuarioRepository(db));
        async Task<int> Criar(string email, Cargo cargo) => (await usuarios.Criar(new NovoUsuarioDto { Nome = email, Email = email, Senha = "senha-boa-1", Cargo = cargo })).Id;
        (supervisor, tecnico, patio, via) = (await Criar("s@teste.local", Cargo.Supervisor), await Criar("t@teste.local", Cargo.Tecnico),
            await Criar("p@teste.local", Cargo.OperadorPatio), await Criar("v@teste.local", Cargo.OperadorVia));
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(supervisor);

        var (inicio, fim) = (new Estacao { Nome = "Santo Amaro", Linha = "9-Esmeralda", Km = 219.000m, Ordem = 1 }, new Estacao { Nome = "Socorro", Linha = "9-Esmeralda", Km = 220.500m, Ordem = 2 });
        db.AddRange(inicio, fim);
        await db.SaveChangesAsync();
        var trecho = new Trecho { IdEstacaoInicio = inicio.IdEstacao, IdEstacaoFim = fim.IdEstacao, ClasseExigida = 1 };
        db.Add(trecho);
        await db.SaveChangesAsync();
        idTrecho = trecho.IdTrecho;
        foreach (var n in new[] { 2, 3, 4 })
            await db.Set<Dormente>().Where(d => d.IdDormente == db.Set<Rfid>().Where(r => r.TagRfid == Tag(n)).Select(r => r.IdDormente).First())
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IdPilha, (int?)null).SetProperty(d => d.IdTrecho, trecho.IdTrecho).SetProperty(d => d.KmProjeto, 219.400m + n / 1000m));
        db.ChangeTracker.Clear();
    }

    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private static string Tag(int n) => $"BR{n:D10}";
    private static LeituraDto Lida(int n) => new() { Tag = Tag(n) };
    private async Task<int> Peca(int n) => (await db.Set<Rfid>().AsNoTracking().SingleAsync(r => r.TagRfid == Tag(n))).IdDormente;
    private LoteTrocaService Lotes() { db.ChangeTracker.Clear(); return BancoDeTeste.Lotes(db); }
    private NovoLoteTrocaDto Novo(int quantidade) => new() { IdTrecho = idTrecho, KmInicio = 219.400m, KmFim = 219.405m, Quantidade = quantidade, IdTamanho = idTamanho, Motivo = "Dormentes podres" };

    private async Task<LoteTrocaDto> Aprovado(int quantidade)
    {
        var lote = await Lotes().Pedir(Novo(quantidade), tecnico);
        return (await Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = true }, Cargo.Supervisor, supervisor))!;
    }

    [Fact]
    public async Task Pedido_confere_trecho_km_quantidade_e_motivo()
    {
        var certo = Novo(10);
        foreach (var errado in new[]
                 {
                     new NovoLoteTrocaDto { IdTrecho = 999_999, KmInicio = 219.4m, KmFim = 219.5m, Quantidade = 10, IdTamanho = idTamanho, Motivo = "x" },
                     new NovoLoteTrocaDto { IdTrecho = idTrecho, KmInicio = 219.5m, KmFim = 219.4m, Quantidade = 10, IdTamanho = idTamanho, Motivo = "x" },
                     new NovoLoteTrocaDto { IdTrecho = idTrecho, KmInicio = 219.4m, KmFim = 219.5m, Quantidade = 0, IdTamanho = idTamanho, Motivo = "x" },
                     new NovoLoteTrocaDto { IdTrecho = idTrecho, KmInicio = 219.4m, KmFim = 219.5m, Quantidade = 10, IdTamanho = 999, Motivo = "x" },
                     new NovoLoteTrocaDto { IdTrecho = idTrecho, KmInicio = 219.4m, KmFim = 219.5m, Quantidade = 10, IdTamanho = idTamanho, Motivo = "  " },
                 })
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Pedir(errado, tecnico));

        var lote = await Lotes().Pedir(certo, tecnico);
        Assert.Equal(("Pedido", "LT-0001", 10, (int?)null), (lote.Status, lote.Codigo, lote.QuantidadePedida, lote.QuantidadeAprovada));
        Assert.Equal(("Santo Amaro – Socorro", (byte)1, 2.80m), (lote.Trecho, lote.ClasseExigida, lote.ComprimentoM));
        Assert.Equal([await Peca(2), await Peca(3), await Peca(4)], lote.PecasDaFaixa.Select(p => p.Id));
        Assert.Contains(await Lotes().Listar(StatusLoteTroca.Pedido), l => l.Id == lote.Id);
    }

    [Fact]
    public async Task Aprovar_menos_exige_o_porque()
    {
        var lote = await Lotes().Pedir(Novo(10), tecnico);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = true }, Cargo.OperadorVia, via));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = true, Quantidade = 8 }, Cargo.Supervisor, supervisor));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = true, Quantidade = 11, Motivo = "mais" }, Cargo.Supervisor, supervisor));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = true, Quantidade = 0, Motivo = "nada" }, Cargo.Supervisor, supervisor));

        var aprovado = (await Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = true, Quantidade = 8, Motivo = "2 ainda aguentam" }, Cargo.Tecnico, tecnico))!;
        Assert.Equal(("Aprovado", (int?)8, "2 ainda aguentam", (int?)tecnico), (aprovado.Status, aprovado.QuantidadeAprovada, aprovado.MotivoMenos, aprovado.IdUsuarioAprovou));
        Assert.NotNull(aprovado.NoiteDoPlano);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = false, Motivo = "não" }, Cargo.Supervisor, supervisor));

        var outro = await Lotes().Pedir(Novo(3), tecnico);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Decidir(outro.Id, new DecisaoLoteTrocaDto { Aprovar = false }, Cargo.Supervisor, supervisor));
        var reprovado = (await Lotes().Decidir(outro.Id, new DecisaoLoteTrocaDto { Aprovar = false, Motivo = "Interdição cancelada" }, Cargo.Supervisor, supervisor))!;
        Assert.Equal(("Reprovado", "Interdição cancelada"), (reprovado.Status, reprovado.MotivoReprovacao));
    }

    [Fact]
    public async Task Separar_ate_a_quantidade_aprovada()
    {
        var lote = await Aprovado(2);
        var compativeis = (await Lotes().ListarCompativeis(lote.Id))!;
        Assert.Equal(23, compativeis.Count); // 26 aprovadas no exemplo, menos as três que estão na via
        Assert.True(compativeis[0].Sugerida);

        var (a, b, c) = (compativeis[0].Peca, compativeis[1].Peca, compativeis[2].Peca);
        var naVia = await Peca(2);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Separar(lote.Id, new SepararTrocaDto { IdDormente = naVia, Leitura = Lida(2) }, patio));
        var separado = (await Lotes().Separar(lote.Id, new SepararTrocaDto { IdDormente = a.Id, Leitura = new() { Tag = a.Tag } }, patio))!;
        Assert.Equal(("Separado", 1), (separado.Status, separado.Separadas.Count));
        Assert.Null((await BancoDeTeste.Pecas(db).BuscarPorId(a.Id))!.IdPilha); // saiu da pilha de verdade
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Separar(lote.Id, new SepararTrocaDto { IdDormente = b.Id, Leitura = new() { Tag = c.Tag } }, patio)); // tag de outra peça
        separado = (await Lotes().Separar(lote.Id, new SepararTrocaDto { IdDormente = b.Id, Leitura = new() { Tag = b.Tag } }, patio))!;
        Assert.Equal(new[] { a.Id, b.Id }.Order(), separado.Separadas.Select(p => p.Id).Order());

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Separar(lote.Id, new SepararTrocaDto { IdDormente = c.Id, Leitura = new() { Tag = c.Tag } }, patio));
        Assert.DoesNotContain((await Lotes().ListarCompativeis(lote.Id))!, c => c.Peca.Id == separado.Separadas[0].Id);
    }

    // ---------- fechar o lote ----------

    /// <summary>O lote aprovado com as primeiras peças compatíveis já separadas.</summary>
    private async Task<LoteTrocaDto> Separado(int aprovadas, int separar, NovoLoteTrocaDto? pedido = null)
    {
        var lote = await Lotes().Pedir(pedido ?? Novo(aprovadas), tecnico);
        lote = (await Lotes().Decidir(lote.Id, new DecisaoLoteTrocaDto { Aprovar = true }, Cargo.Supervisor, supervisor))!;
        foreach (var c in (await Lotes().ListarCompativeis(lote.Id))!.Take(separar).ToList())
            lote = (await Lotes().Separar(lote.Id, new SepararTrocaDto { IdDormente = c.Peca.Id, Leitura = new() { Tag = c.Peca.Tag } }, patio))!;
        return lote;
    }

    private static ParDoLoteDto Par(int saiu, PecaDoLoteDto entra, string foto = "lotes/1/a.jpg")
        => new() { LeituraSaiu = Lida(saiu), LeituraEntrou = new() { Tag = entra.Tag }, FotoArquivo = foto };

    private static FecharLoteTrocaDto Fechamento(params ParDoLoteDto[] pares) => new() { Pares = [.. pares], Equipe = "Turma de Via 3" };

    [Fact]
    public async Task Fecha_o_lote_e_cada_par_vira_uma_troca()
    {
        var lote = await Separado(2, 2);
        var (entra, sobra) = (lote.Separadas[0], lote.Separadas[1]);
        var pilha = (await db.Set<Dormente>().AsNoTracking().SingleAsync(d => d.IdDormente == sobra.Id)).IdPilha; // nula: está separada

        var feito = (await Lotes().Fechar(lote.Id, Fechamento(Par(2, entra)), via))!;

        Assert.Equal(("Feito", "Turma de Via 3", (int?)via), (feito.Status, feito.Equipe, feito.IdUsuarioFez));
        var troca = Assert.Single(feito.Trocas);
        Assert.Equal(("Feita", "Lote", await Peca(2), (int?)entra.Id), (troca.Status, troca.Origem, troca.IdDormenteSaiu, troca.IdDormenteEntrou));
        Assert.Equal(lote.Id, (await db.Set<Troca>().AsNoTracking().SingleAsync(t => t.IdTroca == troca.Id)).IdLoteTroca);
        Assert.Empty(feito.Separadas);
        Assert.Equal([sobra.Id], feito.Devolvidas.Select(p => p.Id));
        Assert.Null(pilha);
        Assert.Equal("A01", (await BancoDeTeste.Pecas(db).BuscarPorId(sobra.Id))!.Pilha); // a sobra voltou para a pilha
        var (nova, velha) = (await BancoDeTeste.Pecas(db).BuscarPorId(entra.Id), await BancoDeTeste.Pecas(db).BuscarPorId(await Peca(2)));
        Assert.Equal(("NaVia", (int?)idTrecho, 219.402m, (int?)null), (nova!.Situacao, nova.IdTrecho, nova.KmProjeto, nova.IdPilha));
        Assert.Equal("Retirada", velha!.Situacao);
        Assert.Equal(["Separada", "Saida"], await db.Set<Movimentacao>().AsNoTracking().Where(m => m.IdDormente == entra.Id && m.IdLoteTroca == lote.Id)
            .OrderBy(m => m.IdMovimentacao).Select(m => m.Tipo.ToString()).ToListAsync());
    }

    [Fact]
    public async Task Fechar_confere_id_por_id()
    {
        var lote = await Separado(2, 2);
        var (s1, s2) = (lote.Separadas[0], lote.Separadas[1]);
        var naoSeparada = (await Lotes().ListarCompativeis(lote.Id))![0].Peca;
        var errados = new[]
        {
            Fechamento(Par(2, new PecaDoLoteDto(naoSeparada.Id, naoSeparada.Codigo, naoSeparada.Tag, null, idTamanho, null))), // a que entra não foi separada
            Fechamento(Par(2, s1), Par(2, s2)),                                                                                  // a mesma peça sai duas vezes
            Fechamento(Par(2, s1), Par(3, s1)),                                                                                  // a mesma peça entra duas vezes
            Fechamento(Par(2, s1), Par(3, s2), Par(4, s1)),                                                                      // mais pares que os aprovados
            Fechamento(Par(2, s1, foto: " ")),                                                                                   // sem foto
            Fechamento(new ParDoLoteDto { LeituraSaiu = new() { Tag = "BR9999999999" }, LeituraEntrou = new() { Tag = s1.Tag }, FotoArquivo = "f.jpg" }), // tag de peça nenhuma
            new FecharLoteTrocaDto { Pares = [Par(2, s1)], Equipe = " " },                                                       // sem equipe
            Fechamento(),                                                                                                        // nada trocado
        };
        foreach (var errado in errados)
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Fechar(lote.Id, errado, via));

        // fora da faixa: o lote só vai do km 219,400 ao 219,402, e a peça 3 está no 219,403
        var estreito = await Separado(1, 1, new NovoLoteTrocaDto { IdTrecho = idTrecho, KmInicio = 219.400m, KmFim = 219.402m, Quantidade = 1, IdTamanho = idTamanho, Motivo = "Podre" });
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Fechar(estreito.Id, Fechamento(Par(3, estreito.Separadas[0])), via));

        var nada = (await Lotes().BuscarPorId(lote.Id))!;
        Assert.Equal(("Separado", 0, 2), (nada.Status, nada.Trocas.Count, nada.Separadas.Count)); // nada foi gravado
    }

    [Fact]
    public async Task Avulso_aberto_fecha_como_feito_no_lote()
    {
        TrocaService Avulsas() { db.ChangeTracker.Clear(); return BancoDeTeste.Trocas(db); }
        var avulso = await Avulsas().Pedir(new NovaTrocaDto { IdDormente = await Peca(2), Motivo = "Dormente rachado" }, tecnico);
        await Avulsas().Decidir(avulso.Id, new DecisaoTrocaDto { Aprovar = true }, Cargo.Supervisor, supervisor);
        var paraOAvulso = (await Avulsas().ListarCompativeis(avulso.Id))![0].Peca;
        await Avulsas().Separar(avulso.Id, new SepararTrocaDto { IdDormente = paraOAvulso.Id, Leitura = new() { Tag = paraOAvulso.Tag } }, patio);

        var lote = await Separado(1, 1);
        Assert.Equal([await Peca(2)], (await Lotes().BuscarPorId(lote.Id))!.AvulsasAbertas.Select(p => p.Id));
        await Lotes().Fechar(lote.Id, Fechamento(Par(2, lote.Separadas[0])), via);

        var fechado = (await Avulsas().BuscarPorId(avulso.Id))!;
        Assert.Equal(("Cancelada", "Feita no lote LT-0001"), (fechado.Status, fechado.Cancelamento));
        Assert.Equal("A01", (await BancoDeTeste.Pecas(db).BuscarPorId(paraOAvulso.Id))!.Pilha); // a peça separada para o avulso voltou
    }

    [Fact]
    public async Task Lote_mandado_duas_vezes_entra_uma()
    {
        var lote = await Separado(1, 1);
        var fechamento = new FecharLoteTrocaDto { Pares = [Par(2, lote.Separadas[0])], Equipe = "Turma de Via 3", IdCliente = Guid.NewGuid(), RegistradoEm = DateTime.Now.AddHours(-3) };
        Assert.Equal("Feito", (await Lotes().Fechar(lote.Id, fechamento, via))!.Status);
        Assert.Equal("Feito", (await Lotes().Fechar(lote.Id, fechamento, via))!.Status);
        Assert.Single(await db.Set<Troca>().AsNoTracking().Where(t => t.IdLoteTroca == lote.Id).ToListAsync());
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Fechar(lote.Id, new FecharLoteTrocaDto { Pares = fechamento.Pares, Equipe = "Outra", IdCliente = Guid.NewGuid() }, via));
    }

    [Fact]
    public async Task Tag_que_nao_responde_vai_pelo_id_da_faixa()
    {
        var lote = await Separado(1, 1);
        var foraDaFaixa = (await Lotes().ListarCompativeis(lote.Id))![0].Peca.Id;
        ParDoLoteDto SemTag(int idSaiu) => new() { TagSaiuNaoResponde = true, IdDormenteSaiu = idSaiu, LeituraEntrou = new() { Tag = lote.Separadas[0].Tag }, FotoArquivo = "f.jpg" };

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Lotes().Fechar(lote.Id, Fechamento(SemTag(foraDaFaixa)), via));
        var feito = (await Lotes().Fechar(lote.Id, Fechamento(SemTag(await Peca(3))), via))!;
        Assert.Equal(await Peca(3), Assert.Single(feito.Trocas).IdDormenteSaiu);
        Assert.Contains(await db.Set<LeituraDormente>().AsNoTracking().Where(l => l.IdDormente == feito.Trocas[0].IdDormenteSaiu).ToListAsync(), l => !l.Leitura);
    }
}
