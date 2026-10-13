using ViaPermanente.Dominio.Helpers;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Dominio.Pedidos;

/// <summary>
/// Confere a nota fiscal com o pedido (decisões do Victor, 2026-10-11):
/// o pedido pode vir em mais de uma nota, então a nota parcial confere; diverge se trouxer tamanho ou classe
/// que não foi pedido, espécie que não foi pedida, classe diferente da espécie ou mais peças do que falta.
/// O volume declarado é comparado com o do catálogo: enquanto a medida do tamanho é provisória, qualquer diferença
/// só avisa; com a medida real, diverge acima da tolerância de PARAMETRO (nota_tolerancia_volume_pct).
/// </summary>
public static class ConferenciaNota
{
    public static ResultadoConferencia Conferir(
        IReadOnlyList<PedidoItem> pedido,
        IReadOnlyList<NotaFiscalItem> notasAnteriores,
        IReadOnlyList<NotaFiscalItem> nota,
        IReadOnlyDictionary<int, TamanhoDormente> tamanhos,
        IReadOnlyDictionary<int, Especie> especies,
        decimal toleranciaVolume)
    {
        var divergencias = new List<string>();
        var avisos = new List<string>();

        foreach (var item in nota)
        {
            var tamanho = tamanhos[item.IdTamanho];
            var especie = especies[item.IdEspecie];
            var rotulo = Rotulo(tamanho, item.Classe);

            if (especie.Classe != item.Classe)
                divergencias.Add($"{especie.NomeCientifico} é de {especie.Classe}ª classe, mas a nota diz {item.Classe}ª.");

            var pedidos = pedido.Where(p => p.IdTamanho == item.IdTamanho && p.Classe == item.Classe).ToList();
            if (pedidos.Count == 0)
                divergencias.Add($"{rotulo} não foi pedido.");
            else if (pedidos.All(p => p.IdEspecie is not null && p.IdEspecie != item.IdEspecie))
                divergencias.Add($"{especie.NomeCientifico} não foi pedida em {rotulo}.");

            var calculado = item.Quantidade * VolumeHelper.M3(tamanho.ComprimentoM, tamanho.LarguraM, tamanho.AlturaM);
            var diferenca = Math.Abs(item.VolumeM3 - calculado);
            if (calculado > 0 && Math.Round(diferenca, 3) > 0)
            {
                var texto = $"{rotulo}, {especie.NomeCientifico}: a nota declara {FormatoHelper.MetrosCubicos(item.VolumeM3)} " +
                            $"e o catálogo calcula {FormatoHelper.MetrosCubicos(calculado)} " +
                            $"({FormatoHelper.Porcento(diferenca / calculado)} de diferença).";
                if (tamanho.MedidaProvisoria)
                    avisos.Add($"{texto} A medida do catálogo ainda é provisória.");
                else if (diferenca / calculado > toleranciaVolume)
                    divergencias.Add(texto);
            }
        }

        foreach (var grupo in nota.GroupBy(i => (i.IdTamanho, i.Classe)))
        {
            var pedidas = pedido.Where(p => p.IdTamanho == grupo.Key.IdTamanho && p.Classe == grupo.Key.Classe).Sum(p => p.Quantidade);
            if (pedidas == 0)
                continue; // já marcado como "não foi pedido"
            var anteriores = notasAnteriores.Where(i => i.IdTamanho == grupo.Key.IdTamanho && i.Classe == grupo.Key.Classe).Sum(i => i.Quantidade);
            var falta = Math.Max(0, pedidas - anteriores);
            var naNota = grupo.Sum(i => i.Quantidade);
            if (naNota > falta)
                divergencias.Add($"{Rotulo(tamanhos[grupo.Key.IdTamanho], grupo.Key.Classe)}: a nota traz {naNota} peças e faltam {falta} " +
                                 $"do pedido ({pedidas} pedidas, {anteriores} em notas anteriores).");
        }

        var faltamNoPedido = pedido.Sum(p => p.Quantidade) - notasAnteriores.Sum(i => i.Quantidade) - nota.Sum(i => i.Quantidade);
        if (faltamNoPedido > 0)
            avisos.Add($"Nota parcial: faltam {faltamNoPedido} peças do pedido em outras notas.");

        return new ResultadoConferencia(divergencias.Count == 0, divergencias, avisos);
    }

    private static string Rotulo(TamanhoDormente tamanho, byte classe) => $"{FormatoHelper.Metros(tamanho.ComprimentoM)} de {classe}ª classe";
}