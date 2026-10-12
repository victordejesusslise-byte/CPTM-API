namespace ViaPermanente.Dominio.Dtos.Apresentacao;

/// <summary>O que a demonstração criou (ou apagou), para a tela e o Swagger.</summary>
public record DemonstracaoDto(int Pedidos, int Pecas, int Alertas, int Trocas, string Mensagem);
