using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Catalogos;
using ViaPermanente.Dados.Via;
using ViaPermanente.Dominio.Comum;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Dtos;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>
/// A passagem com o GPS do veículo no SQL Server de verdade (Victor, 2026-11-17): a passagem aponta a estação A e a B, o sistema
/// calcula o km de cada leitura e de cada detecção pelo caminho que o GPS andou, e grava a latitude e a longitude.
/// </summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class PassagemComGpsNoBancoTestes : IAsyncLifetime
{
    private static readonly DateTime Inicio = new(2026, 11, 16, 22, 0, 0);
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    private int idA;
    private int idB;

    public async Task InitializeAsync()
    {
        await BancoDeTeste.Recriar(db);
        // As duas estações do trecho da passagem: a A no km 251 e a B no km 260.
        var a = new Estacao { Nome = "Estação A", Linha = "9-Esmeralda", Km = 251m, Ordem = 1 };
        var b = new Estacao { Nome = "Estação B", Linha = "9-Esmeralda", Km = 260m, Ordem = 2 };
        db.AddRange(a, b);
        await db.SaveChangesAsync();
        (idA, idB) = (a.IdEstacao, b.IdEstacao);
        db.ChangeTracker.Clear();
    }

    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    private VarreduraService Varredura() => new(new VarreduraRepository(db), new ParametroRepository(db));

    /// <summary>Um ponto do GPS do veículo: a tantos minutos do começo e a tantos milésimos de grau de latitude (111 m cada).</summary>
    private static NovoPontoGpsDto Ponto(int minutos, double milesimos) => new() { Data = Inicio.AddMinutes(minutos), Latitude = -23.5m - (decimal)milesimos / 1000, Longitude = -46.7m };

    private static NovaLeituraViaDto Leitura(int minutos, double milesimos) => new()
    {
        Data = Inicio.AddMinutes(minutos), Latitude = -23.5m - (decimal)milesimos / 1000, Longitude = -46.7m,
        KmMedido = 999m // o que o carro mandava antes: com o GPS e as estações, o sistema calcula e ignora
    };

    private NovaPassagemDto Passagem(string codigo, int? inicio, int? fim, List<NovoPontoGpsDto> pontos, List<NovaLeituraViaDto> leituras) => new()
    {
        Codigo = codigo, Veiculo = "Carro de inspeção", DataInicio = Inicio, IdEstacaoInicio = inicio, IdEstacaoFim = fim, Pontos = pontos, Leituras = leituras
    };

    [Fact]
    public async Task O_sistema_calcula_o_km_de_cada_leitura_pelo_caminho_que_o_GPS_andou()
    {
        var dto = await Varredura().Registrar(Passagem("GPS-1", idA, idB,
            [Ponto(0, 0), Ponto(18, 3)], [Leitura(5, 1), Leitura(12, 2)]));

        db.ChangeTracker.Clear();
        var leituras = await db.Set<LeituraVia>().AsNoTracking().Where(l => l.IdPassagem == dto.Id).OrderBy(l => l.Data).ToListAsync();
        Assert.Equal([254m, 257m], leituras.Select(l => l.KmMedido));
    }

    [Fact]
    public async Task A_passagem_guarda_o_km_das_estacoes_como_inicio_e_fim()
    {
        var dto = await Varredura().Registrar(Passagem("GPS-2", idA, idB, [Ponto(0, 0), Ponto(18, 3)], [Leitura(5, 1)]));

        db.ChangeTracker.Clear();
        var passagem = await db.Set<Passagem>().AsNoTracking().SingleAsync(p => p.IdPassagem == dto.Id);
        Assert.Equal(251m, passagem.KmInicio);
        Assert.Equal(260m, passagem.KmFim);
        Assert.Equal(idA, passagem.IdEstacaoInicio);
        Assert.Equal(idB, passagem.IdEstacaoFim);
    }

    [Fact]
    public async Task A_latitude_e_a_longitude_ficam_gravadas_com_seis_casas()
    {
        var dto = await Varredura().Registrar(Passagem("GPS-3", idA, idB, [Ponto(0, 0), Ponto(18, 3)], [Leitura(5, 1.234567)]));

        db.ChangeTracker.Clear();
        var leitura = await db.Set<LeituraVia>().AsNoTracking().SingleAsync(l => l.IdPassagem == dto.Id);
        Assert.Equal(-23.501235m, leitura.Latitude);
        Assert.Equal(-46.7m, leitura.Longitude);
    }

    [Fact]
    public async Task Os_pontos_do_caminho_ficam_gravados_com_o_km_calculado()
    {
        var dto = await Varredura().Registrar(Passagem("GPS-4", idA, idB, [Ponto(0, 0), Ponto(5, 1), Ponto(18, 3)], [Leitura(5, 1)]));

        db.ChangeTracker.Clear();
        var pontos = await db.Set<PontoPassagem>().AsNoTracking().Where(p => p.IdPassagem == dto.Id).OrderBy(p => p.Data).ToListAsync();
        Assert.Equal([251m, 254m, 260m], pontos.Select(p => p.KmCalculado));
    }

    [Fact]
    public async Task Com_so_uma_estacao_a_passagem_e_recusada()
    {
        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            Varredura().Registrar(Passagem("GPS-5", idA, null, [Ponto(0, 0), Ponto(18, 3)], [Leitura(5, 1)])));

        Assert.Contains("as duas estações", erro.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Estacao_que_nao_existe_e_recusada()
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            Varredura().Registrar(Passagem("GPS-6", idA, 987654, [Ponto(0, 0), Ponto(18, 3)], [Leitura(5, 1)])));
    }

    [Fact]
    public async Task Coordenada_fora_do_mundo_e_recusada()
    {
        var fora = new NovoPontoGpsDto { Data = Inicio, Latitude = -123.5m, Longitude = -46.7m };

        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            Varredura().Registrar(Passagem("GPS-8", idA, idB, [fora, Ponto(18, 3)], [Leitura(5, 1)])));
    }

    [Fact]
    public async Task Sem_as_estacoes_o_km_que_o_carro_mandou_continua_valendo()
    {
        var dto = await Varredura().Registrar(Passagem("GPS-7", null, null, [], [Leitura(5, 1)]));

        db.ChangeTracker.Clear();
        var leitura = await db.Set<LeituraVia>().AsNoTracking().SingleAsync(l => l.IdPassagem == dto.Id);
        Assert.Equal(999m, leitura.KmMedido);
    }
}
