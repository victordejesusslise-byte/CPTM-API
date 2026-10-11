using ViaPermanente.Dominio.Models.Via;

namespace ViaPermanente.Dominio.Repositories;

/// <summary>A partição semanal de LEITURA_VIA no SQL Server.</summary>
public interface IParticaoRepository
{
    Task<IReadOnlyList<DateTime>> Fronteiras();
    Task<IReadOnlyList<ParticaoLeituraVia>> Particoes();
    /// <summary>Abre a semana que começa na fronteira (SPLIT RANGE).</summary>
    Task Abrir(DateTime fronteira);
    /// <summary>Esvazia a primeira partição (tudo antes da primeira fronteira) e junta a fronteira (TRUNCATE + MERGE RANGE). Devolve as linhas apagadas.</summary>
    Task<long> DescartarPrimeira(DateTime primeiraFronteira);
}