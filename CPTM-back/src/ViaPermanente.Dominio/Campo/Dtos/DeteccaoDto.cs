namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>O quadro da câmera com a peça marcada.</summary>
public record DeteccaoDto(int Id, string QuadroArquivo, string? Camera, int CaixaX, int CaixaY, int CaixaLargura, int CaixaAltura,
    string Defeito, byte Grau, decimal Confianca, string VersaoModelo);