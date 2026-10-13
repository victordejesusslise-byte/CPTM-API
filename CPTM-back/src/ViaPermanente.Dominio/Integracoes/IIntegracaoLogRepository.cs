using ViaPermanente.Dominio.Ambiental.Models;

namespace ViaPermanente.Dominio.Integracoes;

public interface IIntegracaoLogRepository
{
    Task Registrar(IReadOnlyList<IntegracaoLog> linhas);
    Task<IReadOnlyList<IntegracaoLog>> Ultimos(int quantidade);
}