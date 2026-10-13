using ViaPermanente.Dominio.Indicadores;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Testes.Helpers;

public class IndicadoresPorCargoTestes
{
    [Theory]
    [InlineData(Cargo.Supervisor, "disponibilidade-registros", true)]
    [InlineData(Cargo.Supervisor, "estoque-m3", true)]
    [InlineData(Cargo.OperadorPatio, "estoque-m3", true)]
    [InlineData(Cargo.OperadorPatio, "acerto-camera", false)]
    [InlineData(Cargo.OperadorVia, "falhas-tag-7-noites", true)]
    [InlineData(Cargo.OperadorVia, "segunda-classe-pedido", false)]
    [InlineData(Cargo.Tecnico, "tempo-medio-inspecao", true)]
    [InlineData(Cargo.Especialista, "identificado-corretamente", true)]
    [InlineData(Cargo.Especialista, "inspecoes-por-dia", false)]
    public void Cada_cargo_ve_os_seus_e_o_supervisor_ve_todos(Cargo cargo, string chave, bool ve)
        => Assert.Equal(ve, IndicadoresPorCargo.Ve(cargo, chave));
}
