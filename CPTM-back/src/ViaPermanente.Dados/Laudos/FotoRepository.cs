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
}