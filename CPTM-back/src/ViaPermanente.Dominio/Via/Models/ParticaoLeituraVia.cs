namespace ViaPermanente.Dominio.Via.Models;

/// <summary>Uma partição de LEITURA_VIA: a semana que ela guarda (Inicio nulo: tudo antes de Fim) e quantas linhas tem.</summary>
public record ParticaoLeituraVia(int Numero, DateTime? Inicio, DateTime? Fim, long Linhas);