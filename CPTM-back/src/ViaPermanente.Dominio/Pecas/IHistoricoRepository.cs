using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

public interface IHistoricoRepository
{
    Task<HistoricoDaPeca> Buscar(int idDormente);
}