using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Pecas;

/// <summary>O acesso ao banco para a classificação da espécie da peça.</summary>
public interface IClassificacaoRepository
{
    /// <summary>A espécie pelo id. Nula: não existe.</summary>
    Task<Especie?> Especie(int idEspecie);
    /// <summary>A classificação mais nova da peça (a que vale), ou nula.</summary>
    Task<ClassificacaoMadeira?> Ultima(int idDormente);
    /// <summary>A classificação já recebida com este id do celular. Nula: é um envio novo.</summary>
    Task<ClassificacaoMadeira?> PorCliente(Guid idCliente);
    /// <summary>Grava a classificação nova e a devolve com o id.</summary>
    Task<ClassificacaoMadeira> Adicionar(ClassificacaoMadeira classificacao);
    /// <summary>Com a espécie de cada uma. Por peça, ou as feitas desde a data (sem a da placa do fornecedor).</summary>
    Task<IReadOnlyList<(ClassificacaoMadeira Classificacao, Especie Especie)>> Listar(int? idDormente, DateTime? desde);
}
