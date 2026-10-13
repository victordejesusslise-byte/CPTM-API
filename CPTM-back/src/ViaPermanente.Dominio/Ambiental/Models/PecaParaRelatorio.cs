namespace ViaPermanente.Dominio.Ambiental.Models;

/// <summary>O que o relatório precisa de cada peça aprovada: espécie vigente, m³, entrada no armazém e saída para a troca.</summary>
public record PecaParaRelatorio(int IdEspecie, decimal M3, DateTime? EntradaEm, DateTime? SaidaEm);