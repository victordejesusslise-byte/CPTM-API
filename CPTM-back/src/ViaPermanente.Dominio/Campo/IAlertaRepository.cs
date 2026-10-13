using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Campo;

public interface IAlertaRepository
{
    Task<IReadOnlyList<Alerta>> Listar(OrigemAlerta? origem, StatusAlerta? status, int? idAlerta = null);
    Task<IReadOnlyDictionary<int, DeteccaoImagem>> Deteccoes(IReadOnlyCollection<int> idsDeteccao);
    /// <summary>O km das leituras do carro que ainda existem (a leitura vive 90 dias).</summary>
    Task<IReadOnlyDictionary<long, decimal?>> KmDasLeituras(IReadOnlyCollection<long> idsLeituraVia);
    Task<IReadOnlyDictionary<int, int>> ContarVistorias(IReadOnlyCollection<int> idsAlerta);
    /// <summary>Grava a vistoria e a leitura; com idDormente, dá a peça ao alerta que não tinha.</summary>
    Task Vistoriar(VistoriaAlerta vistoria, LeituraDormente? leitura, int? idDormenteDoAlerta);
    Task Descartar(int idAlerta, int idUsuario, DateTime data, string motivo);
}