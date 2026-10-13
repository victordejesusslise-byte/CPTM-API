using ViaPermanente.Dominio.Catalogos.Models;

namespace ViaPermanente.Dominio.Catalogos;

public interface IParametroRepository
{
    Task<IReadOnlyList<Parametro>> Listar();
    Task<Parametro?> Buscar(string chave);
    Task Salvar(Parametro parametro);
}