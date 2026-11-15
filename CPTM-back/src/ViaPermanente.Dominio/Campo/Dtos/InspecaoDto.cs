namespace ViaPermanente.Dominio.Campo.Dtos;

/// <summary>Uma inspeção como a tela e o laudo LI mostram: o checklist, o resultado e os motivos da reprovação.</summary>
/// <param name="Id">id da inspeção (→ INSPECAO); no laudo, LI-0001.</param>
/// <param name="IdDormente">id da peça inspecionada (→ DORMENTE).</param>
/// <param name="IdUsuario">o técnico que fez a inspeção (→ USUARIO); só ele pode corrigir.</param>
/// <param name="IdAlerta">o alerta que abriu a inspeção (→ ALERTA); nulo na inspeção de rotina.</param>
/// <param name="IdInspecaoOrigem">no retrabalho, a inspeção que esta refaz (→ INSPECAO); nulo na primeira.</param>
/// <param name="IniciadoEm">quando o cronômetro do checklist começou.</param>
/// <param name="FinalizadoEm">quando o checklist terminou.</param>
/// <param name="DuracaoS">a duração do checklist, em segundos (fim menos início).</param>
/// <param name="VersaoChecklist">a versão das perguntas do checklist (hoje 2).</param>
/// <param name="EstadoConservacao">Bom, Regular ou Ruim.</param>
/// <param name="Rachadura">Nenhuma, Leve, Media ou Grave.</param>
/// <param name="AtaqueBiologico">Nenhum, Inicio ou Avancado.</param>
/// <param name="UmidadePct">a umidade da madeira, em %; nula se não foi medida.</param>
/// <param name="VidaUtilMeses">a vida útil que ainda resta, em meses; nula se não foi estimada.</param>
/// <param name="TipoMadeiraConfere">se a madeira vista confere com a espécie cadastrada.</param>
/// <param name="Resultado">Aprovada ou Reprovada, calculado das respostas.</param>
/// <param name="MotivosDeReprovacao">o que reprovou a peça (ex.: "rachadura grave"); vazia se aprovou.</param>
/// <param name="Observacao">o que o técnico anotou.</param>
/// <param name="FotoArquivo">o caminho da foto da peça na inspeção (obrigatória).</param>
public record InspecaoDto(int Id, int IdDormente, int IdUsuario, int? IdAlerta, int? IdInspecaoOrigem,
    DateTime IniciadoEm, DateTime FinalizadoEm, int DuracaoS, int VersaoChecklist,
    string EstadoConservacao, string Rachadura, string AtaqueBiologico, decimal? UmidadePct, int? VidaUtilMeses, bool TipoMadeiraConfere,
    string Resultado, IReadOnlyList<string> MotivosDeReprovacao, string? Observacao, string FotoArquivo);