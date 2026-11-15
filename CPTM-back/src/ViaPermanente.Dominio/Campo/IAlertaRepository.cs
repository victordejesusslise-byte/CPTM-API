using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>O acesso ao banco dos alertas (ALERTA) e das vistorias (VISTORIA_ALERTA). Usado pelo AlertaService.</summary>
public interface IAlertaRepository
{
    /// <summary>Os alertas, filtrados pela origem, pelo status ou por um id.</summary>
    Task<IReadOnlyList<Alerta>> Listar(OrigemAlerta? origem, StatusAlerta? status, int? idAlerta = null);
    /// <summary>As detecções da câmera (→ DETECCAO_IMAGEM), pelo id.</summary>
    Task<IReadOnlyDictionary<int, DeteccaoImagem>> Deteccoes(IReadOnlyCollection<int> idsDeteccao);
    /// <summary>O km das leituras do carro que ainda existem (a leitura vive 90 dias).</summary>
    Task<IReadOnlyDictionary<long, decimal?>> KmDasLeituras(IReadOnlyCollection<long> idsLeituraVia);
    /// <summary>Quantas vistorias tem cada alerta.</summary>
    Task<IReadOnlyDictionary<int, int>> ContarVistorias(IReadOnlyCollection<int> idsAlerta);
    /// <summary>Grava a vistoria e a leitura; com idDormente, dá a peça ao alerta que não tinha.</summary>
    Task Vistoriar(VistoriaAlerta vistoria, LeituraDormente? leitura, int? idDormenteDoAlerta);
    /// <summary>A vistoria que o celular já mandou com este ID (o reenvio depois de a rede cair).</summary>
    Task<bool> VistoriaExiste(Guid idCliente);
    /// <summary>Marca o alerta como descartado, com quem, quando e por quê.</summary>
    Task Descartar(int idAlerta, int idUsuario, DateTime data, string motivo);
}