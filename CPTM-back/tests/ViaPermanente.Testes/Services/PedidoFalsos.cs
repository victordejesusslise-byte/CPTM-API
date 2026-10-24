using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pecas.Models;
using ViaPermanente.Dominio.Pedidos;
using ViaPermanente.Dominio.Pedidos.Models;

namespace ViaPermanente.Testes.Services;

/// <summary>Repositórios em memória para testar o PedidoService sem banco.</summary>
internal class PedidoRepositoryFalso : IPedidoRepository
{
    public List<Empresa> Empresas { get; } =
    [
        new() { IdEmpresa = 1, Nome = "TG–Três Guri", Papel = PapelEmpresa.Fornecedor, Ativo = true },
        new() { IdEmpresa = 2, Nome = "Granoski", Papel = PapelEmpresa.Tratadora, Ativo = true }
    ];
    public List<Contrato> Contratos { get; } = [new() { IdContrato = 1, IdEmpresa = 1, NumeroAta = "AR00224", NumeroContrato = "AR00224-03" }];
    public List<Pedido> Pedidos { get; } = [];
    public List<PedidoItem> ItensGravados { get; } = [];
    public List<PedidoHistorico> HistoricoGravado { get; } = [];
    public Dictionary<int, int> Recebidas { get; } = [];

    public Task<Empresa?> BuscarEmpresa(int id) => Task.FromResult(Empresas.FirstOrDefault(e => e.IdEmpresa == id));
    public Task<Contrato?> BuscarContrato(int id) => Task.FromResult(Contratos.FirstOrDefault(c => c.IdContrato == id));
    public Task<bool> CodigoEmUso(string codigo) => Task.FromResult(Pedidos.Any(p => p.Codigo == codigo));
    public Task<Pedido?> BuscarPorCodigo(string codigo) => Task.FromResult(Pedidos.FirstOrDefault(p => p.Codigo == codigo));

    public Task Criar(Pedido pedido, IReadOnlyList<PedidoItem> itens)
    {
        pedido.IdPedido = Pedidos.Count + 1;
        Pedidos.Add(pedido);
        foreach (var i in itens) { i.IdPedido = pedido.IdPedido; i.IdPedidoItem = ItensGravados.Count + 1; ItensGravados.Add(i); }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Pedido>> Listar(int? id = null)
        => Task.FromResult<IReadOnlyList<Pedido>>(Pedidos.Where(p => id == null || p.IdPedido == id)
            .Select(p => new Pedido { IdPedido = p.IdPedido, IdEmpresa = p.IdEmpresa, IdUsuario = p.IdUsuario, IdContrato = p.IdContrato,
                Codigo = p.Codigo, DataCompra = p.DataCompra, Quantidade = p.Quantidade, CriadoEm = p.CriadoEm, Status = p.Status }).ToList());

    public Task<IReadOnlyList<PedidoItem>> Itens(IReadOnlyCollection<int> ids)
        => Task.FromResult<IReadOnlyList<PedidoItem>>(ItensGravados.Where(i => ids.Contains(i.IdPedido)).ToList());
    public Task<IReadOnlyDictionary<int, int>> PecasRecebidas(IReadOnlyCollection<int> ids)
        => Task.FromResult<IReadOnlyDictionary<int, int>>(Recebidas.Where(r => ids.Contains(r.Key)).ToDictionary());
    public Task<bool> TemRecebimento(int id) => Task.FromResult(Recebidas.ContainsKey(id));
    public Task<IReadOnlyDictionary<int, string>> NomesEmpresas(IReadOnlyCollection<int> ids)
        => Task.FromResult<IReadOnlyDictionary<int, string>>(Empresas.Where(e => ids.Contains(e.IdEmpresa)).ToDictionary(e => e.IdEmpresa, e => e.Nome));
    public Task<IReadOnlyDictionary<int, string>> NumerosContratos(IReadOnlyCollection<int> ids)
        => Task.FromResult<IReadOnlyDictionary<int, string>>(Contratos.Where(c => ids.Contains(c.IdContrato)).ToDictionary(c => c.IdContrato, c => c.NumeroContrato));

    public Task Corrigir(Pedido pedido, IReadOnlyList<PedidoItem> itens, PedidoHistorico historico)
    {
        var gravado = Pedidos.Single(p => p.IdPedido == pedido.IdPedido);
        (gravado.IdContrato, gravado.DataCompra, gravado.Quantidade) = (pedido.IdContrato, pedido.DataCompra, pedido.Quantidade);
        ItensGravados.RemoveAll(i => i.IdPedido == pedido.IdPedido);
        foreach (var i in itens) { i.IdPedido = pedido.IdPedido; i.IdPedidoItem = ItensGravados.Count + 100; ItensGravados.Add(i); }
        HistoricoGravado.Add(historico);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PedidoHistorico>> Historico(int id) => Task.FromResult<IReadOnlyList<PedidoHistorico>>(HistoricoGravado.Where(h => h.IdPedido == id).ToList());

    public Task Cancelar(int id, PedidoHistorico historico)
    {
        Pedidos.Single(p => p.IdPedido == id).Status = StatusPedido.Cancelado;
        HistoricoGravado.Add(historico);
        return Task.CompletedTask;
    }
}

internal class OfertaRepositoryFalso : IOfertaRepository
{
    public List<OfertaFornecedor> Linhas { get; } = [];
    public Task<IReadOnlyList<OfertaFornecedor>> Listar(int? idEmpresa)
        => Task.FromResult<IReadOnlyList<OfertaFornecedor>>(Linhas.Where(o => idEmpresa == null || o.IdEmpresa == idEmpresa).ToList());
    public Task Adicionar(OfertaFornecedor oferta) { oferta.IdOferta = Linhas.Count + 1; Linhas.Add(oferta); return Task.CompletedTask; }
}

internal class CatalogoRepositoryFalso : ICatalogoRepository
{
    public Task<IReadOnlyList<TamanhoDormente>> ListarTamanhos() => Task.FromResult<IReadOnlyList<TamanhoDormente>>(
    [
        new() { IdTamanho = 1, ComprimentoM = 2.80m },
        new() { IdTamanho = 4, ComprimentoM = 4.80m }
    ]);
    public Task<IReadOnlyList<MedidaFornecedor>> ListarMedidas() => Task.FromResult<IReadOnlyList<MedidaFornecedor>>(
    [
        new() { IdMedida = 1, IdEmpresa = 1, LarguraM = 0.24m, AlturaM = 0.17m, Ativo = true },
        new() { IdMedida = 2, IdEmpresa = 1, IdEspecie = 14, LarguraM = 0.22m, AlturaM = 0.16m, Ativo = true },
        new() { IdMedida = 3, IdEmpresa = 2, LarguraM = 0.24m, AlturaM = 0.17m, Ativo = true },
        new() { IdMedida = 4, IdEmpresa = 1, LarguraM = 0.30m, AlturaM = 0.20m, Ativo = false }
    ]);
    public Task<IReadOnlyDictionary<(int IdTamanho, byte Classe), string>> CodigosErp()
        => Task.FromResult<IReadOnlyDictionary<(int IdTamanho, byte Classe), string>>(new Dictionary<(int, byte), string>());
    public Task<IReadOnlyList<Especie>> ListarEspecies() => Task.FromResult<IReadOnlyList<Especie>>(
    [
        new() { IdEspecie = 1, NomeCientifico = "Ocotea neesiana", Familia = "Lauraceae", Ativo = true },
        new() { IdEspecie = 14, NomeCientifico = "Eucalyptus grandis", Familia = "Myrtaceae", Ativo = true }
    ]);
    public Task<IReadOnlyList<Armazem>> ListarArmazens() => throw new NotImplementedException();
    public Task<IReadOnlyList<Pilha>> ListarPilhas() => throw new NotImplementedException();
    public Task<IReadOnlyDictionary<int, int>> ContarPecasPorPilha() => throw new NotImplementedException();
    public Task<IReadOnlyList<Leitor>> ListarLeitores() => throw new NotImplementedException();
    public Task<IReadOnlyList<ModoLeitura>> ListarModos() => throw new NotImplementedException();
    public Task AdicionarLeitor(Leitor leitor) => throw new NotImplementedException();
    public Task DefinirCodigoErp(int idTamanho, byte classe, string codigoErp) => throw new NotImplementedException();
}

internal class ParametroRepositoryFalso(string unidade = "m3", string pct = "20") : IParametroRepository
{
    private readonly List<Parametro> valores =
    [
        new() { Chave = "limite_segunda_classe_pct", Valor = pct },
        new() { Chave = "limite_segunda_classe_unidade", Valor = unidade },
        new() { Chave = "nota_tolerancia_volume_pct", Valor = "2" }
    ];
    public Task<IReadOnlyList<Parametro>> Listar() => Task.FromResult<IReadOnlyList<Parametro>>(valores);
    public Task<Parametro?> Buscar(string chave) => Task.FromResult(valores.FirstOrDefault(p => p.Chave == chave));
    public Task Salvar(Parametro parametro) => Task.CompletedTask;
}