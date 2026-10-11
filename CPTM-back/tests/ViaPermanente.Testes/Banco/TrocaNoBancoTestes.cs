using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Repositories;
using ViaPermanente.Dominio.Dtos.Inspecoes;
using ViaPermanente.Dominio.Dtos.Pecas;
using ViaPermanente.Dominio.Dtos.Trocas;
using ViaPermanente.Dominio.Dtos.Usuarios;
using ViaPermanente.Dominio.Models.Campo;
using ViaPermanente.Dominio.Models.Patio;
using ViaPermanente.Dominio.Models.Pessoas;
using ViaPermanente.Dominio.Models.Via;
using ViaPermanente.Dominio.Services;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// A troca em quatro passos no SQL Server de verdade. As peças vêm do exemplo da apresentação (todas de 2,80 m de 1ª,
/// na pilha A01 de Santo Amaro); a peça 2 e a 3 são colocadas na via, num trecho de 1ª classe.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class TrocaNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private int tecnico, outroTecnico, patio, via;

    public async Task InitializeAsync()
    {
        await BancoDeTeste.Recriar(db);
        var usuarios = new UsuarioService(new UsuarioRepository(db));
        async Task<int> Criar(string email, Cargo cargo) => (await usuarios.Criar(new NovoUsuarioDto { Nome = email, Email = email, Senha = "senha-boa-1", Cargo = cargo })).Id;
        var supervisor = await Criar("s@teste.local", Cargo.Supervisor);
        (tecnico, outroTecnico, patio, via) = (await Criar("t@teste.local", Cargo.Tecnico), await Criar("t2@teste.local", Cargo.Tecnico),
            await Criar("p@teste.local", Cargo.OperadorPatio), await Criar("v@teste.local", Cargo.OperadorVia));
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(supervisor);

        var (inicio, fim) = (new Estacao { Nome = "Santo Amaro", Linha = "9-Esmeralda", Km = 219.000m, Ordem = 1 }, new Estacao { Nome = "Socorro", Linha = "9-Esmeralda", Km = 220.500m, Ordem = 2 });
        db.AddRange(inicio, fim);
        await db.SaveChangesAsync();
        var trecho = new Trecho { IdEstacaoInicio = inicio.IdEstacao, IdEstacaoFim = fim.IdEstacao, ClasseExigida = 1 };
        db.Add(trecho);
        await db.SaveChangesAsync();
        foreach (var n in new[] { 2, 3 })
            await db.Set<Dormente>().Where(d => d.IdDormente == db.Set<Rfid>().Where(r => r.TagRfid == Tag(n)).Select(r => r.IdDormente).First())
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IdPilha, (int?)null).SetProperty(d => d.IdTrecho, trecho.IdTrecho).SetProperty(d => d.KmProjeto, 219.400m + n / 1000m));
        db.ChangeTracker.Clear();
    }

    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private static string Tag(int n) => $"BR{n:D10}";
    private static LeituraDto Lida(int n) => new() { Tag = Tag(n) };
    private async Task<int> Peca(int n) => (await db.Set<Rfid>().AsNoTracking().SingleAsync(r => r.TagRfid == Tag(n))).IdDormente;
    private TrocaService Trocas() { db.ChangeTracker.Clear(); return BancoDeTeste.Trocas(db); }

    private async Task<InspecaoDto> Reprovar(int n) => await BancoDeTeste.Inspecoes(db).Registrar(new NovaInspecaoDto
    {
        IdDormente = await Peca(n), Leitura = Lida(n), IniciadoEm = DateTime.Now.AddMinutes(-10), FinalizadoEm = DateTime.Now,
        EstadoConservacao = EstadoConservacao.Ruim, Rachadura = RachaduraInspecao.Grave, AtaqueBiologico = AtaqueBiologicoInspecao.Inicio,
        VidaUtilMeses = 3, FotoArquivo = "fotos/insp.jpg"
    }, tecnico);

    private async Task<TrocaDto> Executar(TrocaDto troca, int saiu, int entrou) => (await Trocas().Executar(troca.Id, new ExecutarTrocaDto
    {
        LeituraSaiu = Lida(saiu), LeituraEntrou = Lida(entrou), Equipe = "Turma de Via 2", FotoArquivo = "fotos/troca.jpg"
    }, via))!;

    [Fact]
    public async Task Troca_em_quatro_passos_com_recusa_no_meio()
    {
        var inspecao = await Reprovar(2);
        var pedida = await Trocas().Pedir(new NovaTrocaDto { IdDormente = await Peca(2), IdInspecao = inspecao.Id }, tecnico);
        Assert.Equal(("Pedida", "Inspecao"), (pedida.Status, pedida.Origem));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Trocas().Pedir(new NovaTrocaDto { IdDormente = pedida.IdDormenteSaiu, Motivo = "de novo" }, tecnico));

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Trocas().Decidir(pedida.Id, new DecisaoTrocaDto { Aprovar = true }, Cargo.Tecnico, tecnico));
        var aprovada = await Trocas().Decidir(pedida.Id, new DecisaoTrocaDto { Aprovar = true }, Cargo.Supervisor, 1);
        Assert.Equal("Aprovada", aprovada!.Status);

        var compativeis = (await Trocas().ListarCompativeis(pedida.Id))!;
        Assert.Equal(24, compativeis.Count); // 26 aprovadas no exemplo, menos as duas que estão na via
        Assert.True(compativeis[0].Sugerida);
        Assert.Equal(0.102m, compativeis[0].DistanciaKm); // Santo Amaro (km 219,300) até a troca (km 219,402)
        var entra = 5;
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Trocas().Separar(pedida.Id, new SepararTrocaDto { IdDormente = 0, Leitura = Lida(entra) }, patio));
        var separada = await Trocas().Separar(pedida.Id, new SepararTrocaDto { IdDormente = await Peca(entra), Leitura = Lida(entra) }, patio);
        Assert.Equal(("Separada", Tag(entra)), (separada!.Status, separada.TagEntrou));
        Assert.DoesNotContain((await Trocas().ListarCompativeis(pedida.Id))!, c => c.Peca.Tag == Tag(entra));

        var recusada = await Trocas().Recusar(pedida.Id, new RecusarTrocaDto { Motivo = "Trilho sem fixação", FotoArquivo = "fotos/recusa.jpg" }, via);
        Assert.Equal("Recusada", recusada!.Status);
        Assert.Null(recusada.IdDormenteEntrou);
        Assert.Equal("A01", (await BancoDeTeste.Pecas(db).BuscarPorId(await Peca(entra)))!.Pilha); // voltou ao armazém
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Trocas().DecidirRecusa(pedida.Id, new DecidirRecusaDto { Decisao = DecisaoRecusa.Manter }, tecnico));
        var mantida = await Trocas().DecidirRecusa(pedida.Id, new DecidirRecusaDto { Decisao = DecisaoRecusa.Manter, Resposta = "Fixação refeita" }, tecnico);
        Assert.Equal("Aprovada", mantida!.Status);
        Assert.Equal("Manter", mantida.UltimaRecusa!.Decisao);

        await Trocas().Separar(pedida.Id, new SepararTrocaDto { IdDormente = await Peca(entra), Leitura = Lida(entra) }, patio);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Trocas().Executar(pedida.Id, new ExecutarTrocaDto { LeituraSaiu = Lida(3), LeituraEntrou = Lida(entra), Equipe = "T2", FotoArquivo = "f.jpg" }, via));
        var feita = await Executar(new TrocaDto(pedida.Id, "", "", 0, null, null, null, null, null, null, null, null, null, null, 0, default, null, null, null, null, null, null, null, null, null, null, 0, null), 2, entra);

        Assert.Equal("Feita", feita.Status);
        var (nova, velha) = (await BancoDeTeste.Pecas(db).BuscarPorId(await Peca(entra)), await BancoDeTeste.Pecas(db).BuscarPorId(await Peca(2)));
        Assert.Equal(("NaVia", velha!.IdTrecho), (nova!.Situacao, nova.IdTrecho));
        Assert.Equal("Retirada", velha.Situacao);
        Assert.Equal(["Separada", "Devolvida", "Separada", "Saida"],
            await db.Set<Movimentacao>().AsNoTracking().Where(m => m.IdTroca == pedida.Id).OrderBy(m => m.IdMovimentacao).Select(m => m.Tipo.ToString()).ToListAsync());
    }

    [Fact]
    public async Task Correcao_que_aprova_cancela_a_troca_e_devolve_a_peca()
    {
        var inspecao = await Reprovar(3);
        var troca = await Trocas().Pedir(new NovaTrocaDto { IdDormente = await Peca(3), IdInspecao = inspecao.Id }, tecnico);
        await Trocas().Decidir(troca.Id, new DecisaoTrocaDto { Aprovar = true }, Cargo.Supervisor, 1);
        await Trocas().Separar(troca.Id, new SepararTrocaDto { IdDormente = await Peca(7), Leitura = Lida(7) }, patio);
        var correcao = new CorrigirInspecaoDto
        {
            EstadoConservacao = EstadoConservacao.Regular, Rachadura = RachaduraInspecao.Leve, AtaqueBiologico = AtaqueBiologicoInspecao.Nenhum,
            VidaUtilMeses = 30, Motivo = "Marquei rachadura grave na peça errada"
        };

        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => BancoDeTeste.Inspecoes(db).Corrigir(inspecao.Id, correcao, outroTecnico));
        db.ChangeTracker.Clear();
        var corrigida = await BancoDeTeste.Inspecoes(db).Corrigir(inspecao.Id, correcao, tecnico);

        Assert.Equal("Aprovada", corrigida!.Resultado);
        Assert.Equal("Cancelada", (await Trocas().BuscarPorId(troca.Id))!.Status);
        Assert.Equal("A01", (await BancoDeTeste.Pecas(db).BuscarPorId(await Peca(7)))!.Pilha);
        var registro = await db.Set<InspecaoCorrecao>().AsNoTracking().SingleAsync();
        Assert.Equal(troca.Id, registro.IdTrocaCancelada);
        Assert.Contains("Grave", registro.RetratoAntes);
        Assert.Contains("Leve", registro.RetratoDepois);
    }

    [Fact]
    public async Task Laudos_da_inspecao_da_liberacao_e_da_troca()
    {
        var pdf = new PdfCapturado();
        var inspecao = await Reprovar(2);
        var troca = await Trocas().Pedir(new NovaTrocaDto { IdDormente = await Peca(2), IdInspecao = inspecao.Id }, tecnico);
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => BancoDeTeste.Laudos(db, pdf).Liberacao(troca.Id)); // ainda não aprovada

        var li = (await BancoDeTeste.Laudos(db, pdf).LaudoInspecao(inspecao.Id))!;
        Assert.Equal("LI-0001.pdf", li.Nome);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(li.Conteudo, 0, 4));
        Assert.Contains("Resultado: Reprovada", pdf.Texto);
        Assert.Contains("rachadura grave", pdf.Texto);

        await Trocas().Decidir(troca.Id, new DecisaoTrocaDto { Aprovar = true }, Cargo.Supervisor, 1);
        await Trocas().Separar(troca.Id, new SepararTrocaDto { IdDormente = await Peca(5), Leitura = Lida(5) }, patio);
        db.ChangeTracker.Clear();
        var lb = (await BancoDeTeste.Laudos(db, pdf).Liberacao(troca.Id))!;
        Assert.Contains($"Tag: {Tag(5)}", pdf.Texto);
        Assert.True(lb.Conteudo.Length > 1000);

        await Executar(new TrocaDto(troca.Id, "", "", 0, null, null, null, null, null, null, null, null, null, null, 0, default, null, null, null, null, null, null, null, null, null, null, 0, null), 2, 5);
        db.ChangeTracker.Clear();
        var ld = (await BancoDeTeste.Laudos(db, pdf).LaudoTroca(troca.Id))!;
        Assert.Equal($"LD-{troca.Id:0000}-v4.pdf", ld.Nome); // pedida, aprovada, separada, feita
        Assert.Contains("Turma de Via 2", pdf.Texto);
    }

    [Fact]
    public async Task Boletim_de_recebimento_da_carga_do_exemplo()
    {
        var pdf = new PdfCapturado();
        var br = (await BancoDeTeste.Laudos(db, pdf).BoletimRecebimento(1))!;

        Assert.Equal("BR-0001.pdf", br.Nome);
        Assert.Contains("Reprovadas (tag desativada, devolução): 2", pdf.Texto);
        Assert.Contains("Resultado: Confere", pdf.Texto);
        Assert.Null(await BancoDeTeste.Laudos(db, pdf).BoletimRecebimento(999));
    }
}