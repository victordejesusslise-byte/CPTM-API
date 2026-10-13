using PdfSharp.Fonts;

namespace ViaPermanente.Dados.Laudos;

/// <summary>
/// Entrega ao PDFsharp a Arial da pasta de fontes do Windows (normal, negrito, itálico e os dois).
/// No container (Linux), usa a Liberation Sans do pacote fonts-liberation, que tem as mesmas medidas da Arial.
/// </summary>
internal sealed class FontesDoWindows : IFontResolver
{
    private static readonly Dictionary<string, (string Windows, string Linux)> Arquivos = new()
    {
        ["Arial"] = ("arial.ttf", "LiberationSans-Regular.ttf"), ["Arial#b"] = ("arialbd.ttf", "LiberationSans-Bold.ttf"),
        ["Arial#i"] = ("ariali.ttf", "LiberationSans-Italic.ttf"), ["Arial#bi"] = ("arialbi.ttf", "LiberationSans-BoldItalic.ttf")
    };

    private const string PastaLinux = "/usr/share/fonts/truetype/liberation";

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        => new($"Arial{(isBold || isItalic ? "#" : "")}{(isBold ? "b" : "")}{(isItalic ? "i" : "")}");

    public byte[]? GetFont(string faceName)
    {
        if (!Arquivos.TryGetValue(faceName, out var arquivo)) return null;
        var windows = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), arquivo.Windows);
        return File.ReadAllBytes(File.Exists(windows) ? windows : Path.Combine(PastaLinux, arquivo.Linux));
    }
}
