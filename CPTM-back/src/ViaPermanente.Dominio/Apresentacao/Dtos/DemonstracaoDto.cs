namespace ViaPermanente.Dominio.Apresentacao.Dtos;

/// <summary>O que a demonstração criou (ou apagou), para a tela e o Swagger.</summary>
public record DemonstracaoDto(int Pedidos, int Pecas, int Alertas, int Trocas, string Mensagem);
