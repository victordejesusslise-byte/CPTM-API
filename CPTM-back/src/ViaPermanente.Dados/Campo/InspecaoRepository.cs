using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Campo;
using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dados.Campo;

public class InspecaoRepository(ViaPermanenteContext db) : IInspecaoRepository
{
    public async Task<int?> IdPorCliente(Guid idCliente)
        => await db.Set<Inspecao>().Where(i => i.IdCliente == idCliente).Select(i => (int?)i.IdInspecao).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<Inspecao>> Listar(int? idDormente = null, int? idInspecao = null)
        => await db.Set<Inspecao>().AsNoTracking()
            .Where(i => (idDormente == null || i.IdDormente == idDormente) && (idInspecao == null || i.IdInspecao == idInspecao))
            .OrderByDescending(i => i.FinalizadoEm).ThenByDescending(i => i.IdInspecao)
            .ToListAsync();

    public async Task<int> Registrar(Inspecao inspecao, LeituraDormente leitura, StatusAlerta fecharAlertasComo, string? motivoDescarte)
    {
        await using var transacao = await db.Database.BeginTransactionAsync();
        db.Add(leitura);
        db.Add(inspecao);
        await db.SaveChangesAsync();
        var fechados = await db.Set<Alerta>()
            .Where(a => a.IdDormente == inspecao.IdDormente && a.Status == StatusAlerta.Aberto)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, fecharAlertasComo).SetProperty(a => a.IdUsuarioDecisao, inspecao.IdUsuario)
                .SetProperty(a => a.DataDecisao, inspecao.FinalizadoEm).SetProperty(a => a.MotivoDescarte, motivoDescarte));
        await transacao.CommitAsync();
        return fechados;
    }
}