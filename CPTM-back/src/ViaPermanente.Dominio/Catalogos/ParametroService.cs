using ViaPermanente.Dominio.Catalogos.Dtos;
using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Dominio.Catalogos;

/// <summary>Os parâmetros do sistema. Só muda o que já existe; chave nova é decisão de projeto.</summary>
public class ParametroService(IParametroRepository parametros)
{
    /// <summary>Os códigos de material do ERP ficam em PARAMETRO com esta chave + comprimento + classe; são cadastrados na tela de Integração.</summary>
    public const string PrefixoCodigoErp = "erp_codigo_material_";

    public static string ChaveCodigoErp(int idTamanho, byte classe) => $"{PrefixoCodigoErp}{idTamanho}_{classe}";

    /// <summary>Os parâmetros da tela de Parâmetros (os códigos do ERP ficam na tela de Integração).</summary>
    public async Task<IReadOnlyList<ParametroDto>> Listar()
        => (await parametros.Listar()).Where(p => !p.Chave.StartsWith(PrefixoCodigoErp)).Select(p => new ParametroDto(p.Chave, p.Valor, p.AlteradoEm)).ToList();

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