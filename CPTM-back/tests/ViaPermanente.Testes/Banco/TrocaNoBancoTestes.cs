using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Campo;
using ViaPermanente.Dados.Laudos;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via.Models;

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
        // saída do mais antigo: no mesmo armazém, a que entrou primeiro vem antes
        Assert.Equal(compativeis.OrderBy(c => c.Peca.EntradaArmazemEm).ThenBy(c => c.Peca.Id).Select(c => c.Peca.Id), compativeis.Select(c => c.Peca.Id));
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
        var feita = await Executar(new TrocaDto(pedida.Id, "", "", 0, null, null, null, null, null, null, null, null, null, null, 0, default, null, null, null, null, null, null, null, null, null, null, 0, null, null, false), 2, entra);

        Assert.Equal("Feita", feita.Status);
        var (nova, velha) = (await BancoDeTeste.Pecas(db).BuscarPorId(await Peca(entra)), await BancoDeTeste.Pecas(db).BuscarPorId(await Peca(2)));
        Assert.Equal(("NaVia", velha!.IdTrecho), (nova!.Situacao, nova.IdTrecho));
        Assert.Equal("Retirada", velha.Situacao);
        Assert.Equal(["Separada", "Devolvida", "Separada", "Saida"],
            await db.Set<Movimentacao>().AsNoTracking().Where(m => m.IdTroca == pedida.Id).OrderBy(m => m.IdMovimentacao).Select(m => m.Tipo.ToString()).ToListAsync());
    }

    [Fact]
    public async Task Troca_feita_sem_rede_entra_uma_vez_com_a_hora_do_campo_e_a_foto()
    {
        var pedida = await Trocas().Pedir(new NovaTrocaDto { IdDormente = await Peca(3), Motivo = "Dormente podre" }, tecnico);
        await Trocas().Decidir(pedida.Id, new DecisaoTrocaDto { Aprovar = true }, Cargo.Supervisor, 1);
        var pasta = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ybyra-fotos-" + Guid.NewGuid())).FullName;
        var fotos = new FotoCampoService(new FotoRepository(pasta), new TrocaRepository(db), new AlertaRepository(db), new PecaRepository(db));
        try
        {
            // A foto é tirada com a peça separada; a troca que não existe devolve nulo (404 na API).
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => fotos.DaTroca(pedida.Id, [1, 2, 3], ".jpg"));
            Assert.Null(await fotos.DaTroca(999999, [1, 2, 3], ".jpg"));
            Assert.StartsWith($"pecas/{await Peca(3)}/inspecao/", await fotos.DaInspecao(await Peca(3), [1, 2, 3], ".jpg"));
            Assert.Null(await fotos.DaVistoria(999999, [1, 2, 3], ".jpg"));

            // Primeira volta: a via recusa sem rede e o celular manda a recusa duas vezes. Entra uma.
            await Trocas().Separar(pedida.Id, new SepararTrocaDto { IdDormente = await Peca(5), Leitura = Lida(5) }, patio);
            var recusa = new RecusarTrocaDto { Motivo = "Trilho sem fixação", FotoArquivo = "fotos/r.jpg", IdCliente = Guid.NewGuid(), RegistradoEm = DateTime.Now.AddHours(-2) };
            await Trocas().Recusar(pedida.Id, recusa, via);
            Assert.Equal("Recusada", (await Trocas().Recusar(pedida.Id, recusa, via))!.Status);
            Assert.Single(await db.Set<RecusaTroca>().AsNoTracking().Where(r => r.IdTroca == pedida.Id).ToListAsync());
            await Trocas().DecidirRecusa(pedida.Id, new DecidirRecusaDto { Decisao = DecisaoRecusa.Manter, Resposta = "Fixação refeita" }, tecnico);

            // Segunda volta: a troca feita de madrugada chega horas depois, também duas vezes. Entra uma, com a hora do campo.
            await Trocas().Separar(pedida.Id, new SepararTrocaDto { IdDormente = await Peca(5), Leitura = Lida(5) }, patio);
            var foto = await fotos.DaTroca(pedida.Id, [1, 2, 3], ".JPG");
            Assert.StartsWith($"trocas/{pedida.Id}/", foto);
            var noCampo = DateTime.Now.AddHours(-4);
            var feita = new ExecutarTrocaDto
            {
                LeituraSaiu = Lida(3), LeituraEntrou = Lida(5), Equipe = "Turma de Via 2", FotoArquivo = foto!, IdCliente = Guid.NewGuid(), RegistradoEm = noCampo
            };
            Assert.Equal("Feita", (await Trocas().Executar(pedida.Id, feita, via))!.Status);
            Assert.Equal("Feita", (await Trocas().Executar(pedida.Id, feita, via))!.Status);
            Assert.Single(await db.Set<Movimentacao>().AsNoTracking().Where(m => m.IdTroca == pedida.Id && m.Tipo == TipoMovimentacao.Saida).ToListAsync());
            var gravada = await db.Set<Troca>().AsNoTracking().SingleAsync(t => t.IdTroca == pedida.Id);
            Assert.Equal(noCampo, gravada.Data!.Value, TimeSpan.FromSeconds(1));
            Assert.True(gravada.RecebidoEm > gravada.Data);
            Assert.Equal(feita.IdCliente, gravada.IdCliente);
            // Outro envio, com outro ID, para a troca já feita: não é o mesmo registro, então é recusado.
            await Assert.ThrowsAsync<RegraDeNegocioException>(() => Trocas().Executar(pedida.Id, new ExecutarTrocaDto
            {
                LeituraSaiu = Lida(3), LeituraEntrou = Lida(5), Equipe = "Turma de Via 2", FotoArquivo = foto!, IdCliente = Guid.NewGuid()
            }, via));
        }
        finally
        {
            Directory.Delete(pasta, true);
        }
    }

    [Fact]
    public async Task O_plano_da_noite_leva_as_trocas_desta_noite_e_nao_as_de_amanha()
    {
        var hoje = await Trocas().Pedir(new NovaTrocaDto { IdDormente = await Peca(2), Motivo = "Dormente podre" }, tecnico);
        var amanha = await Trocas().Pedir(new NovaTrocaDto { IdDormente = await Peca(3), Motivo = "Dormente rachado" }, tecnico);
        foreach (var troca in new[] { hoje, amanha })
            await Trocas().Decidir(troca.Id, new DecisaoTrocaDto { Aprovar = true }, Cargo.Supervisor, 1);
        var meioDia = DateTime.Today.AddHours(12);
        var noite = DateOnly.FromDateTime(meioDia);
        await db.Set<Troca>().Where(t => t.IdTroca == hoje.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.NoiteDoPlano, noite));
        await db.Set<Troca>().Where(t => t.IdTroca == amanha.Id).ExecuteUpdateAsync(s => s.SetProperty(t => t.NoiteDoPlano, noite.AddDays(1)));

        var plano = await new PlanoCampoService(Trocas(), BancoDeTeste.Alertas(db), BancoDeTeste.Pecas(db)).Montar(meioDia);
        Assert.Equal(noite, plano.Noite);
        Assert.Equal([hoje.Id], plano.Trocas.Select(t => t.Id));
        Assert.Contains(plano.Pecas, p => p.Id == hoje.IdDormenteSaiu && p.Tag == Tag(2)); // a peça da troca, para a inspeção sem rede
        // No dia seguinte, a de amanhã entra (e a de hoje, que não foi feita, continua).
        var depois = await new PlanoCampoService(Trocas(), BancoDeTeste.Alertas(db), BancoDeTeste.Pecas(db)).Montar(meioDia.AddDays(1));
        Assert.Equal(new[] { hoje.Id, amanha.Id }.Order(), depois.Trocas.Select(t => t.Id).Order());
    }

    [Fact]
    public async Task A_peca_ganha_a_data_de_alteracao_quando_ela_ou_um_registro_dela_muda()
    {
        var antiga = new DateTime(2026, 1, 1);
        await db.Set<Dormente>().ExecuteUpdateAsync(x => x.SetProperty(d => d.DataAlteracao, (DateTime?)antiga));
        var (peca2, peca3, peca4) = (await Peca(2), await Peca(3), await Peca(4));
        DateTime? Data(int id) => db.Set<Dormente>().AsNoTracking().Single(d => d.IdDormente == id).DataAlteracao;

        await Reprovar(2); // a inspeção é um registro da peça
        await Trocas().Pedir(new NovaTrocaDto { IdDormente = peca3, Motivo = "Dormente podre" }, tecnico); // a troca também
        db.ChangeTracker.Clear();

        Assert.True(Data(peca2) > antiga);
        Assert.True(Data(peca3) > antiga);
        Assert.Equal(antiga, Data(peca4)); // a que não mexeu, não muda
    }

    [Fact]
    public async Task A_copia_das_linhas_vem_em_paginas_e_depois_so_o_que_mudou()
    {
        LinhaCampoService Linha() { db.ChangeTracker.Clear(); return new LinhaCampoService(new PecaRepository(db), new InspecaoRepository(db), BancoDeTeste.Pecas(db)); }
        var (peca2, peca3) = (await Peca(2), await Peca(3));
        var agora = DateTime.Now;

        // A primeira vez: as duas peças da via, uma por página.
        var primeira = await Linha().Pagina(null, 0, 1, agora);
        Assert.Equal([peca2], primeira.Pecas.Select(p => p.Id));
        Assert.Equal(peca2, primeira.Proximo);
        var segunda = await Linha().Pagina(null, primeira.Proximo!.Value, 1, agora);
        Assert.Equal([peca3], segunda.Pecas.Select(p => p.Id));
        Assert.Equal(Tag(3), segunda.Pecas[0].Tag);
        Assert.Null((await Linha().Pagina(null, segunda.Proximo!.Value, 1, agora)).Proximo);

        // Depois: só o que mudou desde a última cópia, e a peça que saiu da via vai como Retirada.
        var antiga = new DateTime(2026, 1, 1);
        await db.Set<Dormente>().ExecuteUpdateAsync(x => x.SetProperty(d => d.DataAlteracao, (DateTime?)antiga));
        var desde = DateTime.Now;
        await Reprovar(2);
        await db.Set<Dormente>().Where(d => d.IdDormente == peca3).ExecuteUpdateAsync(x => x.SetProperty(d => d.RetiradaEm, (DateTime?)DateTime.Now).SetProperty(d => d.DataAlteracao, (DateTime?)DateTime.Now.AddSeconds(1)));
        var mudou = await Linha().Pagina(desde, 0, 100, DateTime.Now);
        Assert.Equal([peca2, peca3], mudou.Pecas.Select(p => p.Id));
        Assert.Equal("Reprovada", mudou.Pecas[0].UltimaInspecao);
        Assert.Equal("Retirada", mudou.Pecas[1].Situacao);
        Assert.DoesNotContain((await Linha().Pagina(null, 0, 100, DateTime.Now)).Pecas, p => p.Id == peca3);
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

        await Executar(new TrocaDto(troca.Id, "", "", 0, null, null, null, null, null, null, null, null, null, null, 0, default, null, null, null, null, null, null, null, null, null, null, 0, null, null, false), 2, 5);
        db.ChangeTracker.Clear();
        var ld = (await BancoDeTeste.Laudos(db, pdf).LaudoTroca(troca.Id))!;
        Assert.Equal($"LD-{troca.Id:0000}-v4.pdf", ld.Nome); // pedida, aprovada, separada, feita
        Assert.Contains("Turma de Via 2", pdf.Texto);

        // O mesmo laudo como documento, para a tela mostrar antes de baixar.
        var documento = (await BancoDeTeste.Laudos(db, pdf).Documento("troca", troca.Id))!;
        Assert.Equal($"LD-{troca.Id:0000}-v4", documento.Codigo);
        Assert.Contains(documento.Secoes, s => s.Titulo == "Passos" && s.Tabela!.Linhas.Count == 4);
        Assert.Null(await BancoDeTeste.Laudos(db, pdf).Documento("troca", 999_999));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => BancoDeTeste.Laudos(db, pdf).Documento("outro", troca.Id));
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