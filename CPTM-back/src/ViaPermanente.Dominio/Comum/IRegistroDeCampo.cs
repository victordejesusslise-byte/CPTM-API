namespace ViaPermanente.Dominio.Comum;

/// <summary>Registro que o celular pode gravar sem internet e enviar depois (decisão: Sem internet, o celular guarda e envia depois).
/// O IdCliente é gerado no celular e é único: o mesmo registro enviado duas vezes entra uma vez.</summary>
public interface IRegistroDeCampo
{
    /// <summary>Id gerado no celular, único no banco. Nulo quando quem gravou não mandou.</summary>
    Guid? IdCliente { get; set; }
    /// <summary>A hora em que o registro foi feito: a do celular, quando ele manda, ou a do servidor.</summary>
    DateTime RegistradoEm { get; set; }
    /// <summary>A hora em que o registro chegou ao servidor; nulo enquanto espera rede.</summary>
    DateTime? RecebidoEm { get; set; }
}