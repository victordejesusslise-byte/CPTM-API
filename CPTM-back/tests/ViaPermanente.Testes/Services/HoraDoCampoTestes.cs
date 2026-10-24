using ViaPermanente.Dominio.Comum;

namespace ViaPermanente.Testes.Services;

/// <summary>A hora do campo que o celular manda depois (app Android, Etapa 3).</summary>
public class HoraDoCampoTestes
{
    private static readonly DateTime Agora = new(2026, 10, 24, 6, 0, 0, DateTimeKind.Local);

    [Fact]
    public void Sem_hora_do_celular_vale_a_do_servidor() => Assert.Equal(Agora, HoraDoCampo.De(null, Agora));

    [Fact]
    public void A_hora_local_do_celular_fica_como_veio()
    {
        var madrugada = Agora.AddHours(-4);
        Assert.Equal(madrugada, HoraDoCampo.De(madrugada, Agora));
    }

    [Fact]
    public void A_hora_em_UTC_vira_a_hora_local()
    {
        var madrugada = Agora.AddHours(-4);
        Assert.Equal(madrugada, HoraDoCampo.De(madrugada.ToUniversalTime(), Agora));
    }

    [Fact]
    public void Relogio_do_celular_adiantado_vale_a_hora_do_servidor() => Assert.Equal(Agora, HoraDoCampo.De(Agora.AddMinutes(30), Agora));
}
