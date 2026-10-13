using ViaPermanente.Dominio.Campo.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Campo;

public interface IInspecaoRepository
{
    Task<IReadOnlyList<Inspecao>> Listar(int? idDormente = null, int? idInspecao = null);
    /// <summary>
    /// Grava a leitura e a inspeção e fecha os alertas abertos da peça, das duas origens, com o status dado. Tudo junto.
    /// Devolve quantos alertas fechou.
    /// </summary>
    Task<int> Registrar(Inspecao inspecao, LeituraDormente leitura, StatusAlerta fecharAlertasComo, string? motivoDescarte);
}