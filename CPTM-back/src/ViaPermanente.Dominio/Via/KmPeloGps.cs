namespace ViaPermanente.Dominio.Via;

/// <summary>Um ponto da passagem: a hora e, se o GPS do veículo pegou, a latitude e a longitude.</summary>
public readonly record struct PontoGps(DateTime Hora, double? Latitude, double? Longitude)
{
    /// <summary>Tem GPS neste ponto?</summary>
    public bool TemGps => Latitude is not null && Longitude is not null;
}

/// <summary>
/// O km de cada foto da passagem, só com o GPS do veículo (Victor, 2026-11-17). O primeiro ponto vale o km da estação A e o último
/// o da B; os do meio ficam na proporção do caminho que o GPS andou. Pela distância e não pela hora, porque o veículo para e muda
/// de velocidade. Quem perdeu o sinal fica pela hora entre os vizinhos que têm GPS.
/// </summary>
public static class KmPeloGps
{
    /// <summary>O raio médio da Terra, em metros, para a distância entre dois pontos.</summary>
    private const double RaioDaTerraM = 6_371_000;

    /// <summary>
    /// O km de cada ponto, na mesma ordem em que vieram. Nulo onde não dá para calcular: menos de dois pontos com GPS, GPS que não
    /// andou, ou ponto sem GPS fora do trecho andado.
    /// </summary>
    public static decimal?[] Calcular(decimal kmInicio, decimal kmFim, IReadOnlyList<PontoGps> pontos)
    {
        var resultado = new decimal?[pontos.Count];

        // A hora decide a ordem; a posição na lista de entrada só desempata.
        var ordem = Enumerable.Range(0, pontos.Count).OrderBy(i => pontos[i].Hora).ThenBy(i => i).ToList();
        var comGps = ordem.Where(i => pontos[i].TemGps).ToList();
        if (comGps.Count < 2) return resultado;

        // O caminho andado até cada ponto com GPS, somando a distância de um ao seguinte.
        var andado = new double[comGps.Count];
        for (var k = 1; k < comGps.Count; k++)
            andado[k] = andado[k - 1] + Distancia(pontos[comGps[k - 1]], pontos[comGps[k]]);
        var total = andado[^1];
        if (total <= 0) return resultado;

        for (var k = 0; k < comGps.Count; k++)
            resultado[comGps[k]] = Km(kmInicio, kmFim, andado[k] / total);

        // Quem perdeu o sinal: a parte do tempo entre o vizinho de antes e o de depois, que têm km.
        foreach (var i in ordem.Where(i => !pontos[i].TemGps))
        {
            var antes = comGps.LastOrDefault(j => pontos[j].Hora <= pontos[i].Hora, -1);
            var depois = comGps.FirstOrDefault(j => pontos[j].Hora >= pontos[i].Hora, -1);
            if (antes < 0 || depois < 0) continue;
            var duracao = (pontos[depois].Hora - pontos[antes].Hora).TotalSeconds;
            var parte = duracao <= 0 ? 0 : (pontos[i].Hora - pontos[antes].Hora).TotalSeconds / duracao;
            var kmAntes = resultado[antes]!.Value;
            var kmDepois = resultado[depois]!.Value;
            resultado[i] = Math.Round(kmAntes + (kmDepois - kmAntes) * (decimal)parte, 3);
        }
        return resultado;
    }

    /// <summary>O km a tal parte (0 a 1) do caminho entre as duas estações, com 3 casas, como a coluna do banco.</summary>
    private static decimal Km(decimal kmInicio, decimal kmFim, double parte)
        => Math.Round(kmInicio + (kmFim - kmInicio) * (decimal)parte, 3);

    /// <summary>A distância em metros entre dois pontos do GPS (fórmula de haversine).</summary>
    private static double Distancia(PontoGps a, PontoGps b)
    {
        double Rad(double graus) => graus * Math.PI / 180;
        var dLat = Rad(b.Latitude!.Value - a.Latitude!.Value);
        var dLon = Rad(b.Longitude!.Value - a.Longitude!.Value);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(Rad(a.Latitude!.Value)) * Math.Cos(Rad(b.Latitude!.Value)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * RaioDaTerraM * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}
