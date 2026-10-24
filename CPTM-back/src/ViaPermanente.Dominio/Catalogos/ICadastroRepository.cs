using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Catalogos;

/// <summary>Os cadastros de apoio: empresas, contratos, estações e trechos.</summary>
public interface ICadastroRepository
{
    Task<IReadOnlyList<Empresa>> Empresas();
    Task<IReadOnlyList<Contrato>> Contratos();
    Task<IReadOnlyList<Estacao>> Estacoes();
    Task<IReadOnlyList<Trecho>> Trechos();
    Task Adicionar<T>(T entidade) where T : class;
    Task AtivarEmpresa(int idEmpresa, bool ativo);
    Task<IReadOnlyList<MedidaFornecedor>> Medidas();
    Task AtivarMedida(int idMedida, bool ativo);
}