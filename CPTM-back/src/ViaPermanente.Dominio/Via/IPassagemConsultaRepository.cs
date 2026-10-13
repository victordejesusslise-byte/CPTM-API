using ViaPermanente.Dominio.Via.Models;

namespace ViaPermanente.Dominio.Via;

/// <summary>Leitura das passagens já gravadas (a gravação é do IVarreduraRepository).</summary>
public interface IPassagemConsultaRepository
{
    Task<IReadOnlyList<Passagem>> Ultimas(int quantas);
    Task<Passagem?> Buscar(int idPassagem);
    Task<IReadOnlyList<LeituraVia>> Leituras(int idPassagem);
    Task<int> MudancasDeFaixa(int idPassagem);
    /// <summary>Alertas da passagem: (de sensor, de imagem), e as detecções da câmera que não viraram alerta.</summary>
    Task<(int Sensor, int Imagem, int DeteccoesSemAlerta)> Alertas(int idPassagem);
}
