namespace ViaPermanente.Dominio.Repositories;

/// <summary>Monta uma planilha .xlsx com uma folha: cabeçalho em negrito e uma linha por item.</summary>
public interface IPlanilhaExportacaoRepository
{
    byte[] Montar(string folha, IReadOnlyList<string> cabecalhos, IEnumerable<object?[]> linhas);
}