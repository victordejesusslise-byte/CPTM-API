using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Models.Via;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dados.Repositories;

/// <summary>SQL próprio do SQL Server. As datas vão como parâmetro; os nomes da função e do esquema são fixos.</summary>
public class ParticaoRepository(ViaPermanenteContext db) : IParticaoRepository
{
    private const string Funcao = "PF_LEITURA_VIA_SEMANA";


    public async Task<IReadOnlyList<DateTime>> Fronteiras()
        => await db.Database.SqlQueryRaw<DateTime>(
            $"SELECT CAST(v.value AS datetime2(7)) AS Value FROM sys.partition_range_values v JOIN sys.partition_functions f ON f.function_id = v.function_id WHERE f.name = '{Funcao}'")
            .OrderBy(d => d).ToListAsync();

    public async Task<IReadOnlyList<ParticaoLeituraVia>> Particoes()
    {
        var fronteiras = await Fronteiras();
        var linhas = await db.Database.SqlQueryRaw<LinhasDaParticao>(
            "SELECT p.partition_number AS Numero, CAST(p.rows AS bigint) AS Linhas FROM sys.partitions p WHERE p.object_id = OBJECT_ID('LEITURA_VIA') AND p.index_id = 1")
            .ToListAsync();
        return linhas.OrderBy(l => l.Numero).Select(l => new ParticaoLeituraVia(l.Numero,
            l.Numero >= 2 ? fronteiras[l.Numero - 2] : null,
            l.Numero - 1 < fronteiras.Count ? fronteiras[l.Numero - 1] : null,
            l.Linhas)).ToList();
    }

    public Task Abrir(DateTime fronteira)
        => db.Database.ExecuteSqlAsync($"ALTER PARTITION SCHEME PS_LEITURA_VIA_SEMANA NEXT USED [PRIMARY]; ALTER PARTITION FUNCTION PF_LEITURA_VIA_SEMANA() SPLIT RANGE ({fronteira});");

    public async Task<long> DescartarPrimeira(DateTime primeiraFronteira)
    {
        var antes = (await Particoes()).FirstOrDefault(p => p.Numero == 1)?.Linhas ?? 0;
        await db.Database.ExecuteSqlAsync($"TRUNCATE TABLE LEITURA_VIA WITH (PARTITIONS (1)); ALTER PARTITION FUNCTION PF_LEITURA_VIA_SEMANA() MERGE RANGE ({primeiraFronteira});");
        return antes;
    }

    private sealed record LinhasDaParticao(int Numero, long Linhas);
}