namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>O resultado da regra da 2ª classe. Percentual e Limite vão de 0 a 1 (0,20 = 20%).</summary>
public record ResultadoLimite(bool Valido, decimal Percentual, UnidadeLimite Unidade, decimal Limite, string Mensagem);