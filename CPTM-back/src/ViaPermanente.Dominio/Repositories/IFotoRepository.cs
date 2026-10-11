namespace ViaPermanente.Dominio.Repositories;

/// <summary>Guarda a foto numa pasta do servidor e devolve o caminho que vai para o banco (Onde ficam as fotos ainda é ponto em aberto).</summary>
public interface IFotoRepository
{
    string Salvar(byte[] conteudo, string assunto, string extensao);
}