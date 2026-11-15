namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O quadro da câmera com a peça marcada.</summary>
/// <param name="Id">id da detecção (→ DETECCAO_IMAGEM).</param>
/// <param name="QuadroArquivo">o caminho do quadro da câmera na pasta das fotos.</param>
/// <param name="Camera">qual câmera do carro tirou o quadro.</param>
/// <param name="CaixaX">a marcação da peça no quadro: canto esquerdo, em pixels.</param>
/// <param name="CaixaY">a marcação da peça no quadro: canto de cima, em pixels.</param>
/// <param name="CaixaLargura">a largura da marcação, em pixels.</param>
/// <param name="CaixaAltura">a altura da marcação, em pixels.</param>
/// <param name="Defeito">o defeito que a câmera viu: Rachadura, Fenda ou Quebra.</param>
/// <param name="Grau">a gravidade do defeito, de 1 a 3.</param>
/// <param name="Confianca">a certeza do modelo, de 0 a 1.</param>
/// <param name="VersaoModelo">a versão do modelo de imagem.</param>
public record DeteccaoDto(int Id, string QuadroArquivo, string? Camera, int CaixaX, int CaixaY, int CaixaLargura, int CaixaAltura,
    string Defeito, byte Grau, decimal Confianca, string VersaoModelo);