namespace ViaPermanente.Dominio.Pessoas.Models;

/// <summary>USUARIO: entra pelo e-mail da PESSOA; cada um tem a própria senha, guardada só em hash.</summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public int IdPessoa { get; set; }
    public Cargo TipoCargo { get; set; }
    public DateTime DataCadastro { get; set; }
    public string SenhaHash { get; set; } = "";
    public bool Ativo { get; set; } = true;
}