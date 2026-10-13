using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Chegadas.Models;
using ViaPermanente.Dominio.Pecas;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dados.Pecas;

public class FotoPecaRepository(ViaPermanenteContext db) : IFotoPecaRepository
{
    public Task<bool> PecaExiste(int idDormente) => db.Set<Dormente>().AnyAsync(d => d.IdDormente == idDormente);

    public async Task<IReadOnlyList<(string Arquivo, DateTime Data, string Resultado)>> DaEntrada(int idDormente)
        => (await db.Set<VerificacaoQualidade>().AsNoTracking()
                .Where(v => v.IdDormente == idDormente && v.FotoArquivo != null && v.FotoArquivo != "")
                .OrderByDescending(v => v.Data).Select(v => new { v.FotoArquivo, v.Data, v.Resultado }).ToListAsync())
            .Select(v => (v.FotoArquivo!, v.Data, v.Resultado.ToString())).ToList();

    public async Task<IReadOnlyList<(string Arquivo, DateTime Data, string Defeito, byte Grau, decimal Confianca)>> DaCamera(int idDormente)
        => (await db.Set<DeteccaoImagem>().AsNoTracking()
                .Where(d => d.IdDormente == idDormente).OrderByDescending(d => d.Data)
                .Select(d => new { d.QuadroArquivo, d.Data, d.Defeito, d.Grau, d.Confianca }).ToListAsync())
            .Select(d => (d.QuadroArquivo, d.Data, d.Defeito.ToString(), d.Grau, d.Confianca)).ToList();
}
