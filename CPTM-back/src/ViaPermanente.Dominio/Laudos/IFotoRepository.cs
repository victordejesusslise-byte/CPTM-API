
namespace ViaPermanente.Dominio.Laudos;

/// <summary>Guarda a foto numa pasta do servidor e devolve o caminho que vai para o banco (Onde ficam as fotos ainda é ponto em aberto).</summary>
public interface IFotoRepository
{
    string Salvar(byte[] conteudo, string assunto, string extensao);
    /// <summary>O arquivo pelo caminho que está no banco; nulo se não existe ou se o caminho sai da pasta das fotos.</summary>
    byte[]? Ler(string caminho);
}