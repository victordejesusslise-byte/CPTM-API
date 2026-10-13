using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Via;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Via;

public class PassagemConsultaRepository(ViaPermanenteContext db) : IPassagemConsultaRepository
{
    public async Task<IReadOnlyList<Passagem>> Ultimas(int quantas)
        => await db.Set<Passagem>().AsNoTracking().OrderByDescending(p => p.DataInicio).Take(quantas).ToListAsync();

    public Task<Passagem?> Buscar(int idPassagem) => db.Set<Passagem>().AsNoTracking().FirstOrDefaultAsync(p => p.IdPassagem == idPassagem);

    public async Task<IReadOnlyList<LeituraVia>> Leituras(int idPassagem)
        => await db.Set<LeituraVia>().AsNoTracking().Where(l => l.IdPassagem == idPassagem).OrderBy(l => l.KmMedido).ToListAsync();

    public Task<int> MudancasDeFaixa(int idPassagem) => db.Set<EventoPosicao>().CountAsync(e => e.IdPassagem == idPassagem);

    public async Task<(int Sensor, int Imagem, int DeteccoesSemAlerta)> Alertas(int idPassagem)
    {
        var leituras = db.Set<LeituraVia>().Where(l => l.IdPassagem == idPassagem).Select(l => (long?)l.IdLeituraVia);
        var deteccoes = db.Set<DeteccaoImagem>().Where(d => d.IdPassagem == idPassagem).Select(d => (int?)d.IdDeteccao);
        var sensor = await db.Set<Alerta>().CountAsync(a => a.Origem == OrigemAlerta.Sensor && leituras.Contains(a.IdLeituraVia));
        var imagem = await db.Set<Alerta>().CountAsync(a => a.Origem == OrigemAlerta.Imagem && deteccoes.Contains(a.IdDeteccao));
        var semAlerta = await db.Set<DeteccaoImagem>().CountAsync(d => d.IdPassagem == idPassagem && !db.Set<Alerta>().Any(a => a.IdDeteccao == d.IdDeteccao));
        return (sensor, imagem, semAlerta);
    }
}
