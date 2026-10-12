using System.Net;
using System.Net.Sockets;

namespace ViaPermanente.Api.Helpers;

/// <summary>
/// O front aberto pelo celular no mesmo Wi-Fi (http://192.168.x.x:4200). Só em desenvolvimento (Cors:RedeLocal):
/// vale endereço de rede privada (10.x, 172.16-31.x, 192.168.x) ou localhost, sempre na porta do ng serve.
/// </summary>
public static class RedeLocalHelper
{
    public const int PortaDoFront = 4200;

    public static bool EhFrontDaRede(string? origem)
    {
        if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.Port != PortaDoFront)
            return false;
        if (uri.IsLoopback) return true;
        if (!IPAddress.TryParse(uri.Host, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168);
    }
}
