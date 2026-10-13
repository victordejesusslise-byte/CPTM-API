using ViaPermanente.Dominio.Laudos;

namespace ViaPermanente.Dados.Laudos;

/// <summary>As fotos numa pasta do servidor (Fotos__Pasta no .env), por assunto, ano e mês. O banco guarda o caminho relativo.</summary>
public class FotoRepository(string pasta) : IFotoRepository
{
    public string Salvar(byte[] conteudo, string assunto, string extensao)
    {
        var relativo = Path.Combine(assunto, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"), $"{Guid.NewGuid():N}{extensao.ToLowerInvariant()}");
        var completo = Path.Combine(pasta, relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(completo)!);
        File.WriteAllBytes(completo, conteudo);
        return relativo.Replace('\\', '/');
    }

    public byte[]? Ler(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return null;
        var base_ = Path.GetFullPath(pasta);
        var completo = Path.GetFullPath(Path.Combine(base_, caminho));
        return completo.StartsWith(base_ + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(completo) ? File.ReadAllBytes(completo) : null;
    }
}