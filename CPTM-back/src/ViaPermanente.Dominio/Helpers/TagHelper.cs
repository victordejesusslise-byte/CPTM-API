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
}