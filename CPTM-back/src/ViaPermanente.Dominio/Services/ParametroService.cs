using ViaPermanente.Dominio.Dtos.Parametros;
using ViaPermanente.Dominio.Repositories;

namespace ViaPermanente.Dominio.Services;

/// <summary>Os parâmetros do sistema. Só muda o que já existe; chave nova é decisão de projeto.</summary>
public class ParametroService(IParametroRepository parametros)
{
    public async Task<IReadOnlyList<ParametroDto>> Listar()
        => (await parametros.Listar()).Select(p => new ParametroDto(p.Chave, p.Valor, p.AlteradoEm)).ToList();

    /// <summary>Devolve nulo se a chave não existe.</summary>
    public async Task<ParametroDto?> Alterar(string chave, string valor, int idUsuario)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new RegraDeNegocioException("Informe o valor.");

        var parametro = await parametros.Buscar(chave);
        if (parametro is null)
            return null;

        parametro.Valor = valor.Trim();
        parametro.AlteradoPor = idUsuario;
        parametro.AlteradoEm = DateTime.Now;
        await parametros.Salvar(parametro);
        return new ParametroDto(parametro.Chave, parametro.Valor, parametro.AlteradoEm);
    }
}