namespace ViaPermanente.Dominio.Dtos.Pedidos;

/// <summary>A 2ª classe do pedido: percentual e limite em % (20 = 20%), a unidade e a mensagem para a tela.</summary>
public record LimiteDto(bool Valido, decimal Percentual, string Unidade, decimal Limite, string Mensagem);