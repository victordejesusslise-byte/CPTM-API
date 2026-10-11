using PdfSharp.Fonts;

namespace ViaPermanente.Dados.Repositories;

/// <summary>Entrega ao PDFsharp a Arial da pasta de fontes do Windows (normal, negrito, itálico e os dois).</summary>
internal sealed class FontesDoWindows : IFontResolver
{
    private static readonly Dictionary<string, string> Arquivos = new()
    {
        ["Arial"] = "arial.ttf", ["Arial#b"] = "arialbd.ttf", ["Arial#i"] = "ariali.ttf", ["Arial#bi"] = "arialbi.ttf"
    };

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        => new($"Arial{(isBold || isItalic ? "#" : "")}{(isBold ? "b" : "")}{(isItalic ? "i" : "")}");

    public byte[]? GetFont(string faceName)
        => Arquivos.TryGetValue(faceName, out var arquivo)
            ? File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), arquivo))
            : null;
}