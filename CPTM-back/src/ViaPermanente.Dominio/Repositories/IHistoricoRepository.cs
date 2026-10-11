using ViaPermanente.Dominio.Models.Patio;

namespace ViaPermanente.Dominio.Repositories;

public interface IHistoricoRepository
{
    Task<HistoricoDaPeca> Buscar(int idDormente);
}