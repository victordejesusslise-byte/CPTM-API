using ViaPermanente.Dominio.Models.Patio;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// Nada muda de estado sem alvo único: ao apontar para o dormente 1842, a ação não pode cair no 1841.
/// O módulo e a pistola precisam de uma tag só no alcance; câmera, NFC e digitado leem uma de cada vez.
/// </summary>
public static class AlvoUnico
{
    public const string MensagemAmbigua = "Mais de um dormente próximo. Aproxime ou direcione melhor.";

    public static bool EhAmbigua(MetodoCaptura metodo, int tagsVistas)
        => metodo is MetodoCaptura.ModuloUhf or MetodoCaptura.Pistola && tagsVistas != 1;
}