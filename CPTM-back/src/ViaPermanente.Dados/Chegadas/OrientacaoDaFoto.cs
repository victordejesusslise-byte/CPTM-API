using Tesseract;

namespace ViaPermanente.Dados.Chegadas;

/// <summary>
/// A câmera do celular grava a foto deitada e anota no EXIF do JPEG como mostrar (orientação 6: girar 90° no sentido do relógio).
/// A galeria e o navegador obedecem a anotação; o Leptonica não, e o Tesseract lia a placa de lado e não achava nada (Victor, 2026-10-27).
/// Aqui a foto é endireitada antes do OCR. As orientações espelhadas (2, 4, 5 e 7) o celular não grava: ficam como vieram.
/// </summary>
public static class OrientacaoDaFoto
{
    private const ushort TagOrientacao = 0x0112;

    /// <summary>A orientação do EXIF (1 a 8); 1 (em pé) quando não é JPEG, não tem EXIF ou o EXIF não diz.</summary>
    public static int Ler(byte[] foto)
    {
        if (foto.Length < 4 || foto[0] != 0xFF || foto[1] != 0xD8)
            return 1;
        var i = 2;
        while (i + 4 <= foto.Length && foto[i] == 0xFF)
        {
            var marcador = foto[i + 1];
            // Início da imagem comprimida: o EXIF, se houvesse, já teria aparecido.
            if (marcador == 0xDA)
                break;
            var tamanho = (foto[i + 2] << 8) | foto[i + 3];
            if (tamanho < 2)
                break;
            if (marcador == 0xE1 && tamanho >= 8 && i + 10 <= foto.Length
                && foto[i + 4] == 'E' && foto[i + 5] == 'x' && foto[i + 6] == 'i' && foto[i + 7] == 'f' && foto[i + 8] == 0 && foto[i + 9] == 0)
                return DoTiff(foto, i + 10, Math.Min(foto.Length, i + 2 + tamanho)) ?? 1;
            i += 2 + tamanho;
        }
        return 1;
    }

    /// <summary>A foto em pé. Devolve uma Pix nova (quem chama descarta as duas).</summary>
    public static Pix Endireitar(Pix foto, int orientacao) => orientacao switch
    {
        3 => Girar180(foto),
        6 => foto.Rotate90(1),
        8 => foto.Rotate90(-1),
        _ => foto.Clone(),
    };

    private static Pix Girar180(Pix foto)
    {
        using var meia = foto.Rotate90(1);
        return meia.Rotate90(1);
    }

    /// <summary>O cabeçalho TIFF do EXIF ("II" ou "MM", 42) e a primeira lista de campos (IFD0), onde fica a orientação.</summary>
    private static int? DoTiff(byte[] foto, int inicio, int fim)
    {
        if (inicio + 8 > fim)
            return null;
        bool? intel = (foto[inicio], foto[inicio + 1]) switch { ((byte)'I', (byte)'I') => true, ((byte)'M', (byte)'M') => false, _ => null };
        if (intel is not bool ii || Numero16(foto, inicio + 2, ii) != 42)
            return null;
        var ifd = inicio + (int)Numero32(foto, inicio + 4, ii);
        if (ifd < inicio || ifd + 2 > fim)
            return null;
        var campos = Numero16(foto, ifd, ii);
        for (var n = 0; n < campos; n++)
        {
            var campo = ifd + 2 + n * 12;
            if (campo + 12 > fim)
                return null;
            if (Numero16(foto, campo, ii) == TagOrientacao)
            {
                var valor = Numero16(foto, campo + 8, ii);
                return valor is >= 1 and <= 8 ? valor : null;
            }
        }
        return null;
    }

    private static ushort Numero16(byte[] b, int i, bool intel)
        => intel ? (ushort)(b[i] | (b[i + 1] << 8)) : (ushort)((b[i] << 8) | b[i + 1]);

    private static uint Numero32(byte[] b, int i, bool intel)
        => intel ? (uint)(b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24)) : (uint)((b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3]);
}
