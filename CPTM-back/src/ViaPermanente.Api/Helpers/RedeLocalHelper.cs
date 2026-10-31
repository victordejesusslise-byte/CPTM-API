using System.Net;
using System.Net.Sockets;

namespace ViaPermanente.Api.Helpers;

/// <summary>
/// O front aberto por outro aparelho da mesma rede (http://192.168.x.x:4200 ou :4300). Só em desenvolvimento (Cors:RedeLocal):
/// vale endereço de rede privada (10.x, 172.16-31.x, 192.168.x) ou localhost, na porta do ng serve (4200) ou na do site de
/// produção (4300, a que a VPN serve; Victor, 2026-10-31).
/// </summary>
public static class RedeLocalHelper
{
    public static readonly int[] PortasDoFront = [4200, 4300];

    public static bool EhFrontDaRede(string? origem)
    {
        if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !PortasDoFront.Contains(uri.Port))
            return false;
        if (uri.IsLoopback) return true;
        if (!IPAddress.TryParse(uri.Host, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168);
    }
}
