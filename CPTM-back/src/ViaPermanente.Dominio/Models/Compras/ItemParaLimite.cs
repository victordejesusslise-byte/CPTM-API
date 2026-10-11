namespace ViaPermanente.Dominio.Models.Compras;

/// <summary>O que a regra da 2ª classe precisa de cada item do pedido.</summary>
public record ItemParaLimite(byte Classe, int Quantidade, decimal VolumeM3PorPeca);