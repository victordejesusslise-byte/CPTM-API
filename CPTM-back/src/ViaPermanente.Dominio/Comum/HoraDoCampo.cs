namespace ViaPermanente.Dominio.Comum;

/// <summary>
/// A hora em que o registro foi feito no campo, mandada pelo celular (que pode ter ficado sem rede e mandado horas depois).
/// O celular manda em UTC (com Z); o banco guarda a hora local do servidor (America/Sao_Paulo), como o resto.
/// Nula ou no futuro (relógio do celular adiantado): vale a hora do servidor.
/// </summary>
public static class HoraDoCampo
{
    public static DateTime De(DateTime? registradoEm, DateTime agora)
    {
        if (registradoEm is not { } r)
            return agora;
        var local = r.Kind == DateTimeKind.Utc ? r.ToLocalTime() : r;
        return local <= agora ? local : agora;
    }
}
