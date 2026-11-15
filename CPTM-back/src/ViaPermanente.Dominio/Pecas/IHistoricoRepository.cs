using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>Busca no banco tudo o que as tabelas de evento guardam de uma peça.</summary>
public interface IHistoricoRepository
{
    /// <summary>Os registros da peça em todas as tabelas de evento.</summary>
    Task<HistoricoDaPeca> Buscar(int idDormente);
}