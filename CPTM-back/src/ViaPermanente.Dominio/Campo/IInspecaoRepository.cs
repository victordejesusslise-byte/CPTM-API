using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Campo;

/// <summary>O acesso ao banco das inspeções (INSPECAO). Usado pelo InspecaoService e pela cópia das linhas do celular.</summary>
public interface IInspecaoRepository
{
    /// <summary>As inspeções, de uma peça ou por id.</summary>
    Task<IReadOnlyList<Inspecao>> Listar(int? idDormente = null, int? idInspecao = null);
    /// <summary>A inspeção que o celular já mandou com este ID (o reenvio depois de a rede cair).</summary>
    Task<int?> IdPorCliente(Guid idCliente);
    /// <summary>
    /// Grava a leitura e a inspeção e fecha os alertas abertos da peça, das duas origens, com o status dado. Tudo junto.
    /// Devolve quantos alertas fechou.
    /// </summary>
    Task<int> Registrar(Inspecao inspecao, LeituraDormente leitura, StatusAlerta fecharAlertasComo, string? motivoDescarte);
}