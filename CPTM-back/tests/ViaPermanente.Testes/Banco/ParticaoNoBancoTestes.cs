using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dados;
using ViaPermanente.Dados.Via;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Testes.Banco;

/// <summary>A partição semanal de LEITURA_VIA no SQL Server de verdade: abre semanas à frente e descarta as de mais de 90 dias.</summary>
[Trait("Categoria", "Banco")]
[Collection("Banco")]
public class ParticaoNoBancoTestes : IAsyncLifetime
{
    private readonly ViaPermanenteContext db = BancoDeTeste.Contexto();
    public Task InitializeAsync() => BancoDeTeste.Recriar(db);
    public Task DisposeAsync() => BancoDeTeste.Apagar(db);

    [Fact]
    public async Task Leitura_velha_some_e_a_nova_fica()
    {
        var passagem = new Passagem { Codigo = "PAS-P", DataInicio = new DateTime(2026, 7, 1) };
        db.Add(passagem);
        await db.SaveChangesAsync();
        db.AddRange(
            new LeituraVia { IdPassagem = passagem.IdPassagem, Leitura = true, Data = new DateTime(2026, 7, 1, 23, 0, 0) },   // mais de 97 dias
            new LeituraVia { IdPassagem = passagem.IdPassagem, Leitura = true, Data = new DateTime(2026, 10, 10, 23, 0, 0) }); // ontem
        await db.SaveChangesAsync();
        var hoje = new DateTime(2026, 10, 11);

        var resultado = await new ManutencaoLeituraViaService(new ParticaoRepository(db)).Executar(hoje);

        Assert.Equal(1, resultado.LinhasApagadas);
        Assert.Equal(new DateTime(2026, 10, 10, 23, 0, 0), (await db.Set<LeituraVia>().AsNoTracking().SingleAsync()).Data);
        Assert.Contains(resultado.Particoes, p => p.Inicio == new DateTime(2026, 10, 5) && p.Linhas == 1);
        Assert.True(resultado.Particoes.Max(p => p.Inicio) >= new DateTime(2026, 11, 2));
        Assert.True(resultado.Particoes.Where(p => p.Inicio != null).Min(p => p.Inicio) > hoje.AddDays(-97));
        Assert.Empty((await new ManutencaoLeituraViaService(new ParticaoRepository(db)).Executar(hoje)).SemanasDescartadas); // de novo não muda nada
    }
}