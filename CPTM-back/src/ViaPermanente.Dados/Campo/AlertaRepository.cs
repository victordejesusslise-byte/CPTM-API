using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Campo;

public class AlertaRepository(ViaPermanenteContext db) : IAlertaRepository
{
    public Task<bool> VistoriaExiste(Guid idCliente) => db.Set<VistoriaAlerta>().AnyAsync(v => v.IdCliente == idCliente);

    public async Task<IReadOnlyList<Alerta>> Listar(OrigemAlerta? origem, StatusAlerta? status, int? idAlerta = null)
        => await db.Set<Alerta>().AsNoTracking()
            .Where(a => (origem == null || a.Origem == origem) && (status == null || a.Status == status) && (idAlerta == null || a.IdAlerta == idAlerta))
            .OrderByDescending(a => a.Data).ThenByDescending(a => a.IdAlerta)
            .ToListAsync();

    public async Task<IReadOnlyDictionary<int, DeteccaoImagem>> Deteccoes(IReadOnlyCollection<int> idsDeteccao)
        => await db.Set<DeteccaoImagem>().AsNoTracking().Where(d => idsDeteccao.Contains(d.IdDeteccao)).ToDictionaryAsync(d => d.IdDeteccao);

    public async Task<IReadOnlyDictionary<long, decimal?>> KmDasLeituras(IReadOnlyCollection<long> idsLeituraVia)
        => await db.Set<LeituraVia>().AsNoTracking().Where(l => idsLeituraVia.Contains(l.IdLeituraVia))
            .GroupBy(l => l.IdLeituraVia).ToDictionaryAsync(g => g.Key, g => g.Max(l => l.KmMedido));

    public async Task<IReadOnlyDictionary<int, int>> ContarVistorias(IReadOnlyCollection<int> idsAlerta)
        => await db.Set<VistoriaAlerta>().AsNoTracking().Where(v => idsAlerta.Contains(v.IdAlerta))
            .GroupBy(v => v.IdAlerta).ToDictionaryAsync(g => g.Key, g => g.Count());

    public async Task Vistoriar(VistoriaAlerta vistoria, LeituraDormente? leitura, int? idDormenteDoAlerta)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        if (leitura is not null)
            db.Add(leitura);
        db.Add(vistoria);
        await db.SaveChangesAsync();
        if (idDormenteDoAlerta is int idDormente)
            await db.Set<Alerta>().Where(a => a.IdAlerta == vistoria.IdAlerta && a.IdDormente == null)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IdDormente, idDormente));
        await transacao.CommitAsync();
    }

    public Task Descartar(int idAlerta, int idUsuario, DateTime data, string motivo)
        => db.Set<Alerta>().Where(a => a.IdAlerta == idAlerta && a.Status == StatusAlerta.Aberto)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, StatusAlerta.Descartado).SetProperty(a => a.IdUsuarioDecisao, idUsuario)
                .SetProperty(a => a.DataDecisao, data).SetProperty(a => a.MotivoDescarte, motivo));
}