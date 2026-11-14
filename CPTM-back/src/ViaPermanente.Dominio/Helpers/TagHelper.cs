namespace ViaPermanente.Dominio.Helpers;

/// <summary>O número da tag UHF: "BR" e a sequência com 10 dígitos (BR0000000001).</summary>
public static class TagHelper
{
    public const string Prefixo = "BR";

    /// <summary>O número seguinte ao último usado. Sem último (ou fora do formato), começa em BR0000000001.</summary>
    public static string Proxima(string? ultima)
    {
        var atual = ultima is not null && ultima.StartsWith(Prefixo) && long.TryParse(ultima[Prefixo.Length..], out var n) ? n : 0;
        return Numero(atual + 1);
    }

    public static string Numero(long sequencia) => $"{Prefixo}{sequencia:D10}";

    /// <summary>O número que o módulo grava: BR e 10 dígitos.</summary>
    public static bool Valida(string? tag)
        => tag is { Length: 12 } && tag.StartsWith(Prefixo) && tag[Prefixo.Length..].All(char.IsAsciiDigit);

    /// <summary>
    /// A tag do fornecedor montada pelo pedido (Victor, 2026-11-14): SIGLA-NÚMERO DO PEDIDO-SEQUÊNCIA, como TG-20261013193233-28.
    /// O número do pedido junta todos os dígitos do código ("PD-20261017-195108" → 20261017195108); sem dígito, vale o id do pedido.
    /// A sequência é a peça seguinte do pedido (cadastradas + 1), com pelo menos 2 dígitos. Sem sigla, fica NÚMERO-SEQUÊNCIA.
    /// </summary>
    public static string DoFornecedor(string? sigla, string codigoPedido, int idPedido, int cadastradas)
    {
        var digitos = new string(codigoPedido.Where(char.IsAsciiDigit).ToArray());
        var numero = digitos.Length > 0 ? digitos : idPedido.ToString();
        var sequencia = $"{numero}-{cadastradas + 1:00}";
        return string.IsNullOrWhiteSpace(sigla) ? sequencia : $"{sigla.Trim()}-{sequencia}";
    }
}