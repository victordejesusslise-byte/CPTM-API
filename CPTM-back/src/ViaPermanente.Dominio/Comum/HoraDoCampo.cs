namespace ViaPermanente.Dominio.Comum;

/// <summary>A hora em que o registro foi feito no campo, mandada pelo celular (que pode ter ficado sem rede e mandado horas depois).
/// O celular manda em UTC; o banco guarda a hora local do servidor (America/Sao_Paulo).
/// Nula ou no futuro (relógio do celular adiantado): vale a hora do servidor.</summary>
public static class HoraDoCampo
{
    /// <summary>A hora do celular, em hora local; ou a do servidor, se o celular não mandou ou mandou uma hora no futuro.</summary>
    public static DateTime De(DateTime? registradoEm, DateTime agora)
    {
        // Sem a hora do celular: vale a do servidor.
        if (registradoEm is not { } r)
            return agora;
        // Passa de UTC para a hora local, como o banco guarda.
        var local = r.Kind == DateTimeKind.Utc ? r.ToLocalTime() : r;
        // Relógio do celular adiantado: não deixa a hora ficar no futuro.
        return local <= agora ? local : agora;
    }
}
