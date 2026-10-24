using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Catalogos;

public interface ICatalogoRepository
{
    /// <summary>Os comprimentos (lista fixa do código, TamanhoDormente.Todos).</summary>
    Task<IReadOnlyList<TamanhoDormente>> ListarTamanhos();
    /// <summary>Todos os padrões de medida dos fornecedores, ativos e desativados.</summary>
    Task<IReadOnlyList<MedidaFornecedor>> ListarMedidas();
    Task<IReadOnlyList<Especie>> ListarEspecies();
    Task<IReadOnlyList<Armazem>> ListarArmazens();
    Task<IReadOnlyList<Pilha>> ListarPilhas();
    /// <summary>Quantas peças estão em cada pilha agora (DORMENTE.id_pilha).</summary>
    Task<IReadOnlyDictionary<int, int>> ContarPecasPorPilha();
    Task<IReadOnlyList<Leitor>> ListarLeitores();
    Task<IReadOnlyList<ModoLeitura>> ListarModos();
    Task AdicionarLeitor(Leitor leitor);
    /// <summary>Os códigos de material do ERP, por comprimento e classe (guardados em PARAMETRO).</summary>
    Task<IReadOnlyDictionary<(int IdTamanho, byte Classe), string>> CodigosErp();
    /// <summary>Grava o código do ERP de um comprimento numa classe (1 ou 2), em PARAMETRO.</summary>
    Task DefinirCodigoErp(int idTamanho, byte classe, string codigoErp);
}