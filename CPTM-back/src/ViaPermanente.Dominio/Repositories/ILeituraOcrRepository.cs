using ViaPermanente.Dominio.Models.Chegada;

namespace ViaPermanente.Dominio.Repositories;

public interface ILeituraOcrRepository
{
    Task<IReadOnlyList<ModeloOcr>> Modelos();
    Task Gravar(LeituraOcr leitura, IReadOnlyList<OcrCampo> campos);
    Task<LeituraOcr?> Buscar(int idLeitura);
    Task<IReadOnlyList<OcrCampo>> Campos(int idLeitura);
    /// <summary>Grava a confirmação de cada campo e liga a leitura à chegada e ao feixe, se vierem.</summary>
    Task Confirmar(int idLeitura, IReadOnlyList<(int IdCampo, string Valor, bool Corrigido)> confirmacoes, int? idRecebimento, int? idFeixe);
}