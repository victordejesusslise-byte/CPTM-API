using ViaPermanente.Dominio.Models.Campo;
using ViaPermanente.Dominio.Models.Patio;

namespace ViaPermanente.Dominio.Repositories;

public interface IInspecaoRepository
{
    Task<IReadOnlyList<Inspecao>> Listar(int? idDormente = null, int? idInspecao = null);
    /// <summary>
    /// Grava a leitura e a inspeção e fecha os alertas abertos da peça, das duas origens, com o status dado. Tudo junto.
    /// Devolve quantos alertas fechou.
    /// </summary>
    Task<int> Registrar(Inspecao inspecao, LeituraDormente leitura, StatusAlerta fecharAlertasComo, string? motivoDescarte);
}