namespace ViaPermanente.Dominio.Models.Chegada;

/// <summary>MODELO_OCR: as versões do Tesseract treinado; só uma ativa por vez.</summary>
public class ModeloOcr
{
    public int IdModelo { get; set; }
    public string Nome { get; set; } = "";
    public int Versao { get; set; }
    public string ModeloBase { get; set; } = "";
    public DateTime? TreinadoEm { get; set; }
    public int Amostras { get; set; }
    public decimal? AcertoTeste { get; set; }
    public bool Ativo { get; set; }
    public int? IdUsuarioAtivou { get; set; }
}