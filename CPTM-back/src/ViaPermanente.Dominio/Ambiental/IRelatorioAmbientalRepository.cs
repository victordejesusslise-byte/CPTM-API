using ViaPermanente.Dominio.Ambiental.Models;

namespace ViaPermanente.Dominio.Ambiental;

public interface IRelatorioAmbientalRepository
{
    Task<IReadOnlyList<PecaParaRelatorio>> Pecas();
    Task<IReadOnlyList<RelatorioAmbiental>> Listar(int? idRelatorio = null, DateOnly? mes = null);
    Task<IReadOnlyList<RelatorioAmbientalItem>> Itens(IReadOnlyCollection<int> idsRelatorio);
    /// <summary>Grava o relatório e os itens; apaga antes o rascunho do mesmo mês, se houver.</summary>
    Task Gravar(RelatorioAmbiental relatorio, IReadOnlyList<RelatorioAmbientalItem> itens, int? idRascunhoAnterior);
    Task Enviar(int idRelatorio, DateTime enviadoEm);
}