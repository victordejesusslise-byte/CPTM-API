using ViaPermanente.Dominio.Campo;

namespace ViaPermanente.Testes.Services;

/// <summary>O plano da noite (Victor, 2026-10-24): corte às 20:00, urgente e a trava até as 06:00 do dia seguinte.</summary>
public class PlanoDaNoiteTestes
{
    private static readonly TimeOnly Corte = new(20, 0);
    private static readonly DateOnly Dia = new(2026, 10, 24);

    [Theory]
    [InlineData(19, 59, false, 0)] // antes do corte: entra hoje
    [InlineData(20, 0, false, 0)]  // no corte: ainda entra
    [InlineData(21, 30, false, 1)] // depois do corte: noite seguinte
    [InlineData(21, 30, true, 0)]  // depois do corte, urgente: entra hoje
    public void A_noite_em_que_a_troca_entra(int hora, int minuto, bool urgente, int diasDepois)
        => Assert.Equal(Dia.AddDays(diasDepois), PlanoDaNoite.NoiteDe(Dia.ToDateTime(new TimeOnly(hora, minuto)), Corte, urgente));

    [Theory]
    [InlineData(0, 19, 0, false)]  // antes do corte da noite: pode cancelar
    [InlineData(0, 22, 0, true)]   // depois do corte: travada
    [InlineData(1, 3, 12, true)]   // de madrugada: travada
    [InlineData(1, 6, 0, false)]   // a noite acabou: pode cancelar
    public void A_troca_do_plano_trava_do_corte_ate_a_manha(int dias, int hora, int minuto, bool travada)
        => Assert.Equal(travada, PlanoDaNoite.Travada(Dia, Dia.AddDays(dias).ToDateTime(new TimeOnly(hora, minuto)), Corte));

    [Fact]
    public void Sem_noite_do_plano_nao_trava()
        => Assert.False(PlanoDaNoite.Travada(null, Dia.ToDateTime(new TimeOnly(23, 0)), Corte));

    [Theory]
    [InlineData("20:00", 20, 0)]
    [InlineData("8:30", 8, 30)]
    [InlineData("vinte", 20, 0)] // inválido: vale 20:00
    [InlineData(null, 20, 0)]
    public void O_corte_vem_do_parametro(string? valor, int hora, int minuto)
        => Assert.Equal(new TimeOnly(hora, minuto), PlanoDaNoite.Corte(valor));
}
