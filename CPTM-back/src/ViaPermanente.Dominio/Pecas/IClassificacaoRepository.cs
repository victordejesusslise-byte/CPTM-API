using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

public interface IClassificacaoRepository
{
    Task<Especie?> Especie(int idEspecie);
    /// <summary>A classificação mais nova da peça (a que vale), ou nula.</summary>
    Task<ClassificacaoMadeira?> Ultima(int idDormente);
    Task<ClassificacaoMadeira?> PorCliente(Guid idCliente);
    Task<ClassificacaoMadeira> Adicionar(ClassificacaoMadeira classificacao);
    /// <summary>Com a espécie de cada uma. Por peça, ou as feitas desde a data (sem a da placa do fornecedor).</summary>
    Task<IReadOnlyList<(ClassificacaoMadeira Classificacao, Especie Especie)>> Listar(int? idDormente, DateTime? desde);
}
