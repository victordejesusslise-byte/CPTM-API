using ViaPermanente.Dominio.Models.Ambiental;

namespace ViaPermanente.Dominio.Repositories;

public interface IIntegracaoLogRepository
{
    Task Registrar(IReadOnlyList<IntegracaoLog> linhas);
    Task<IReadOnlyList<IntegracaoLog>> Ultimos(int quantidade);
}