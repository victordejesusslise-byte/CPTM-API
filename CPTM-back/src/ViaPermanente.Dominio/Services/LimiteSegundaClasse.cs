using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Models.Compras;

namespace ViaPermanente.Dominio.Services;

/// <summary>
/// Regra do pedido: a 2ª classe fica em no máximo 20%, contada em m³ (padrão) ou em peças, conforme PARAMETRO.
/// O mesmo pedido pode passar numa unidade e falhar na outra (Limite de segunda classe em m³, com parâmetro).
/// </summary>
public static class LimiteSegundaClasse
{
    /// <summary>Conta em decimal e compara sem arredondar; exatamente no limite passa.</summary>
    public static ResultadoLimite Validar(IReadOnlyList<ItemParaLimite> itens, UnidadeLimite unidade, decimal limite = 0.20m)
    {
        decimal Peso(ItemParaLimite i) => unidade == UnidadeLimite.Pecas ? i.Quantidade : i.Quantidade * i.VolumeM3PorPeca;

        var total = itens.Sum(Peso);
        if (itens.Count == 0 || total <= 0)
            return new ResultadoLimite(false, 0, unidade, limite, "O pedido precisa de pelo menos um item com quantidade.");

        var percentual = itens.Where(i => i.Classe == 2).Sum(Peso) / total;
        var valido = percentual <= limite;
        var conta = unidade == UnidadeLimite.Pecas ? "das peças" : "do m³";
        var texto = $"2ª classe: {FormatoHelper.Porcento(percentual)} {conta} (limite de {FormatoHelper.Porcento(limite)}).";
        return new ResultadoLimite(valido, percentual, unidade, limite,
            valido ? texto : $"{texto} Reduza a 2ª classe para registrar o pedido.");
    }
}