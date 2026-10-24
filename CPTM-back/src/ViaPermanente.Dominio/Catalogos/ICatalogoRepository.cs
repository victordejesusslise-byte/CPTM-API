using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;

namespace ViaPermanente.Dominio.Catalogos;

public interface ICatalogoRepository
{
    Task<IReadOnlyList<TamanhoDormente>> ListarTamanhos();
    Task<IReadOnlyList<Especie>> ListarEspecies();
    Task<IReadOnlyList<Armazem>> ListarArmazens();
    Task<IReadOnlyList<Pilha>> ListarPilhas();
    /// <summary>Quantas peças estão em cada pilha agora (DORMENTE.id_pilha).</summary>
    Task<IReadOnlyDictionary<int, int>> ContarPecasPorPilha();
    Task<IReadOnlyList<Leitor>> ListarLeitores();
    Task<IReadOnlyList<ModoLeitura>> ListarModos();
    Task AdicionarLeitor(Leitor leitor);
    /// <summary>Grava o código do ERP de um tamanho numa classe (1 ou 2).</summary>
    Task DefinirCodigoErp(int idTamanho, byte classe, string codigoErp);
}