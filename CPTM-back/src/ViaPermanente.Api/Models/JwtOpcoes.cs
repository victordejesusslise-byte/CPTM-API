namespace ViaPermanente.Api.Models;

/// <summary>Seção "Jwt" da configuração. A Chave vem do .env (Jwt__Chave), nunca do appsettings.</summary>
public class JwtOpcoes
{
    /// <summary>A chave secreta que assina o token (HMAC); vem do .env e precisa ter pelo menos 32 caracteres.</summary>
    public string Chave { get; set; } = "";
    /// <summary>Quem emite o token (o "iss"); o back confere que o token recebido veio dele mesmo.</summary>
    public string Emissor { get; set; } = "";
    /// <summary>Para quem o token vale (o "aud"); o back recusa token feito para outro público.</summary>
    public string Publico { get; set; } = "";
    /// <summary>Quanto tempo o token vale, em minutos (padrão 480 = 8 horas, um turno).</summary>
    public int MinutosValidade { get; set; } = 480;
}