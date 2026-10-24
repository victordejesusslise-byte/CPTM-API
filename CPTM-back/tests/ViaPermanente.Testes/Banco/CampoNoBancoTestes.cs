using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Apresentacao;
using ViaPermanente.Dados.Campo;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Pecas;
using ViaPermanente.Dados.Pessoas;
using ViaPermanente.Dados.Via;
using ViaPermanente.Dominio.Apresentacao;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Dtos;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Dtos;
using ViaPermanente.Dominio.Pessoas;
using ViaPermanente.Dominio.Pessoas.Dtos;
using ViaPermanente.Dominio.Pessoas.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// Varredura noturna, alertas, vistoria e inspeção no SQL Server de verdade. As peças vêm do exemplo da apresentação
/// (tags BR0000000001 a BR0000000028; a 6ª e a 20ª foram reprovadas na qualidade).
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class CampoNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private int idTecnico;

    public async Task InitializeAsync()
    {
        await BancoDeTeste.Recriar(db);
        var usuarios = new UsuarioService(new UsuarioRepository(db));
        var supervisor = await usuarios.Criar(new NovoUsuarioDto { Nome = "Supervisor", Email = "s@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Supervisor });
        idTecnico = (await usuarios.Criar(new NovoUsuarioDto { Nome = "Técnico", Email = "t@teste.local", Senha = "senha-boa-1", Cargo = Cargo.Tecnico })).Id;
        await new ApresentacaoService(new ApresentacaoRepository(db)).CriarExemplo(supervisor.Id);
        db.ChangeTracker.Clear();
    }

    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private static string Tag(int n) => $"BR{n:D10}";
    private async Task<int> Peca(int n) => (await db.Set<Dominio.Pecas.Models.Rfid>().AsNoTracking().SingleAsync(r => r.TagRfid == Tag(n))).IdDormente;
    private VarreduraService Varredura() => new(new VarreduraRepository(db), new ParametroRepository(db));
    private PecaService Pecas() => new(new PecaRepository(db), new CatalogoRepository(db));
    private AlertaService Alertas() => new(new AlertaRepository(db), new PecaRepository(db), new LeituraService(new PecaRepository(db)), Pecas());
    private InspecaoService Inspecoes() => new(new InspecaoRepository(db), new AlertaRepository(db), new PecaRepository(db), new LeituraService(new PecaRepository(db)),
        new TrocaRepository(db), BancoDeTeste.Trocas(db));
    private static LeituraDto Lida(int n, int tagsVistas = 1) => new() { Tag = Tag(n), TagsVistas = tagsVistas };

    private async Task<PassagemDto> Passagem(string codigo, params NovaLeituraViaDto[] leituras)
    {
        db.ChangeTracker.Clear();
        return await Varredura().Registrar(new NovaPassagemDto
        {
            Codigo = codigo, Veiculo = "VLI-Carro Controle 04", DataInicio = new DateTime(2026, 10, 10, 23, 0, 0), Leituras = [.. leituras]
        });
    }

    private NovaInspecaoDto Checklist(int idDormente, int n, int? idAlerta, RachaduraInspecao rachadura, Guid? idCliente = null) => new()
    {
        IdCliente = idCliente,
        IdDormente = idDormente, IdAlerta = idAlerta, Leitura = Lida(n), IniciadoEm = new DateTime(2026, 10, 11, 9, 0, 0),
        FinalizadoEm = new DateTime(2026, 10, 11, 9, 12, 0), EstadoConservacao = EstadoConservacao.Regular, Rachadura = rachadura,
        AtaqueBiologico = AtaqueBiologicoInspecao.Nenhum, VidaUtilMeses = 24, FotoArquivo = "fotos/inspecao.jpg"
    };

    [Fact]
    public async Task Passagem_abre_alertas_e_acompanha_a_posicao()
    {
        var noite1 = await Varredura().Registrar(new NovaPassagemDto
        {
            Codigo = "PAS-1", DataInicio = new DateTime(2026, 10, 9, 23, 0, 0),
            Leituras =
            [
                new() { Tag = Tag(1), DesvioMm = 50, KmMedido = 213.120m },       // fora: alerta de posição
                new() { Tag = Tag(2), DesvioMm = 3, RachaduraSuspeita = true },     // alerta de rachadura
                new() { IdDormente = await Peca(3), Leitura = false },              // tag muda
                new() { Tag = Tag(4), DesvioMm = -25 },                             // atenção: sem alerta, com evento
                new() { Tag = Tag(5), DesvioMm = 5 },                               // ok
                new() { Tag = "BR9999999999", DesvioMm = 1 }                        // tag sem cadastro
            ],
            Deteccoes =
            [
                new() { Tag = Tag(7), QuadroArquivo = "quadros/1.jpg", Defeito = DefeitoImagem.Fenda, Grau = 2, Confianca = 0.91m, VersaoModelo = "v1" },
                new() { QuadroArquivo = "quadros/2.jpg", Defeito = DefeitoImagem.Quebra, Grau = 3, Confianca = 0.95m, VersaoModelo = "v1" },
                new() { Tag = Tag(8), QuadroArquivo = "quadros/3.jpg", Defeito = DefeitoImagem.Rachadura, Grau = 1, Confianca = 0.50m, VersaoModelo = "v1" }
            ]
        });

        Assert.Equal((6, 1, 1, 1), (noite1.Leituras, noite1.SemResposta, noite1.ForaDePosicao, noite1.TagsDesconhecidas));
        Assert.Equal((3, 2, 1, 2), (noite1.AlertasSensor, noite1.AlertasImagem, noite1.DeteccoesAbaixoDoMinimo, noite1.MudancasDeFaixa));
        var peca1 = await Peca(1);
        Assert.Equal(FaixaPosicao.Fora, (await db.Set<PosicaoAtual>().AsNoTracking().SingleAsync(p => p.IdDormente == peca1)).Faixa);

        var noite2 = await Passagem("PAS-2", new() { Tag = Tag(1), DesvioMm = 55 }, new() { Tag = Tag(4), DesvioMm = 8 });
        Assert.Equal(0, noite2.AlertasSensor);   // a peça 1 já tem alerta de posição aberto
        Assert.Equal(1, noite2.MudancasDeFaixa); // a peça 4 voltou de atenção para ok
        Assert.Equal(3, await db.Set<EventoPosicao>().CountAsync());
    }

    [Fact]
    public async Task Vistoria_descarte_e_inspecao_decidem_os_alertas()
    {
        await Passagem("PAS-1", new() { Tag = Tag(1), DesvioMm = 50 }, new() { Tag = Tag(2), DesvioMm = 3, RachaduraSuspeita = true });
        db.ChangeTracker.Clear();
        await Varredura().Registrar(new NovaPassagemDto
        {
            Codigo = "PAS-IMG", DataInicio = new DateTime(2026, 10, 10, 23, 0, 0),
            Deteccoes = [new() { QuadroArquivo = "quadros/9.jpg", Defeito = DefeitoImagem.Fenda, Grau = 2, Confianca = 0.9m, VersaoModelo = "v1" }]
        });
        var fila = await Alertas().Listar(null, StatusAlerta.Aberto);
        var posicao = fila.Single(a => a.Tipo == "Posicao");
        var rachadura = fila.Single(a => a.Tipo == "Rachadura" && a.Origem == "Sensor");
        var semPeca = fila.Single(a => a.Origem == "Imagem");
        var (peca1, peca9) = (await Peca(1), await Peca(9));
        Assert.Null(semPeca.IdDormente);

        // sem foto não registra; a tag da vizinha é recusada; sem peça não abre o checklist
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Alertas().Vistoriar(posicao.Id, new NovaVistoriaDto { Viu = true, Leitura = Lida(1) }, idTecnico));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Alertas().Vistoriar(posicao.Id, new NovaVistoriaDto { Viu = true, Leitura = Lida(2), FotoArquivo = "f.jpg" }, idTecnico));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Inspecoes().Registrar(Checklist(peca9, 9, semPeca.Id, RachaduraInspecao.Nenhuma), idTecnico));

        // a vistoria do alerta sem peça lê a tag e dá a peça ao alerta
        // mandada duas vezes pelo celular (a rede caiu antes da resposta): entra uma
        var vistoria = new NovaVistoriaDto { Viu = true, Leitura = Lida(9), FotoArquivo = "f.jpg", IdCliente = Guid.NewGuid() };
        await Alertas().Vistoriar(semPeca.Id, vistoria, idTecnico);
        var identificado = await Alertas().Vistoriar(semPeca.Id, vistoria, idTecnico);
        Assert.Equal(await Peca(9), identificado!.IdDormente);
        Assert.Equal(1, identificado.Vistorias);
        Assert.Equal("Aberto", identificado.Status);

        var descartado = await Alertas().Descartar(rachadura.Id, "Marca de ferramenta, não é rachadura", idTecnico);
        Assert.Equal("Descartado", descartado!.Status);

        var reprovou = await Inspecoes().Registrar(Checklist(await Peca(1), 1, posicao.Id, RachaduraInspecao.Grave), idTecnico);
        var checklist = Checklist(await Peca(9), 9, semPeca.Id, RachaduraInspecao.Leve, Guid.NewGuid());
        var aprovou = await Inspecoes().Registrar(checklist, idTecnico);
        Assert.Equal(aprovou.Id, (await Inspecoes().Registrar(checklist, idTecnico)).Id);

        Assert.Equal("Reprovada", reprovou.Resultado);
        Assert.Contains("rachadura grave", reprovou.MotivosDeReprovacao);
        Assert.Equal(720, reprovou.DuracaoS);
        Assert.Equal("Confirmado", (await Alertas().BuscarPorId(posicao.Id))!.Status);
        Assert.Equal("Aprovada", aprovou.Resultado);
        Assert.Equal("Descartado", (await Alertas().BuscarPorId(semPeca.Id))!.Status);
        Assert.Empty(await Alertas().Listar(null, StatusAlerta.Aberto));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => Inspecoes().Registrar(Checklist(peca1, 1, posicao.Id, RachaduraInspecao.Grave), idTecnico));
    }
}