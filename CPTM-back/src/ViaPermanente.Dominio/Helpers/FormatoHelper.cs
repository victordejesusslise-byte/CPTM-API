using System.Globalization;

namespace ViaPermanente.Dominio.Helpers;

/// <summary>Números escritos como no Brasil, para as mensagens da API: "2,80 m", "3,199 m³", "22,2%".</summary>
public static class FormatoHelper
{
    private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

    public static string Metros(decimal metros) => metros.ToString("0.00", Brasil) + " m";

    public static string MetrosCubicos(decimal m3) => m3.ToString("0.###", Brasil) + " m³";

    /// <summary>Fração para porcentagem: 0,222 vira "22,2%".</summary>
    public static string Porcento(decimal fracao) => (fracao * 100).ToString("0.#", Brasil) + "%";
}