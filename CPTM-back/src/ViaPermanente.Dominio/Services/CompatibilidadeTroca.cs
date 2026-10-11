namespace ViaPermanente.Dominio.Services;

/// <summary>
/// A peça que entra tem o mesmo comprimento da que sai e classe igual ou melhor que a exigida pelo trecho
/// (2ª não entra em trecho de 1ª; 1ª serve em trecho de 2ª). A classe 1 é melhor que a 2.
/// </summary>
public static class CompatibilidadeTroca
{
    public static bool Serve(int idTamanhoDaCandidata, byte classeDaCandidata, int idTamanhoQueSai, byte classeExigida)
        => idTamanhoDaCandidata == idTamanhoQueSai && classeDaCandidata <= classeExigida;
}