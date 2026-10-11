using ViaPermanente.Dominio.Models.Catalogos;

namespace ViaPermanente.Dominio.Repositories;

public interface IParametroRepository
{
    Task<IReadOnlyList<Parametro>> Listar();
    Task<Parametro?> Buscar(string chave);
    Task Salvar(Parametro parametro);
}