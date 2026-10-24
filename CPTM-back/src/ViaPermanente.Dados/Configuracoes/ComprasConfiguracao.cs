using Microsoft.EntityFrameworkCore;
using ViaPermanente.Dominio.Armazens.Models;
using ViaPermanente.Dominio.Catalogos.Models;
using ViaPermanente.Dominio.Pedidos.Models;
using ViaPermanente.Dominio.Pessoas.Models;

namespace ViaPermanente.Dados.Configuracoes;

/// <summary>CONTRATO, EMPRESA, MEDIDA_FORNECEDOR, NOTA_FISCAL, NOTA_FISCAL_ITEM, OFERTA_FORNECEDOR, PEDIDO, PEDIDO_HISTORICO, PEDIDO_ITEM e RECEBIMENTO.</summary>
internal static class ComprasConfiguracao
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<Empresa>(e => e.HasKey(x => x.IdEmpresa));

        // Os padrões de largura × altura de cada fornecedor (sem espécie: valem para todas).
        mb.Entity<MedidaFornecedor>(e =>
        {
            e.HasKey(x => x.IdMedida);
            e.ToTable(t => t.HasCheckConstraint("CK_MEDIDA_FORNECEDOR_medidas", "[largura_m] > 0 AND [altura_m] > 0"));
        });
        mb.Fk<MedidaFornecedor, Empresa>(x => x.IdEmpresa);
        mb.Fk<MedidaFornecedor, Especie>(x => x.IdEspecie);

        mb.Entity<Contrato>(e =>
        {
            e.HasKey(x => x.IdContrato);
            e.HasIndex(x => x.NumeroContrato).IsUnique();
        });
        mb.Fk<Contrato, Empresa>(x => x.IdEmpresa);

        mb.Entity<NotaFiscal>(e => e.HasKey(x => x.IdNota));
        mb.Fk<NotaFiscal, Pedido>(x => x.IdPedido);
        mb.Fk<NotaFiscal, Usuario>(x => x.IdUsuarioConferiu);

        mb.Entity<NotaFiscalItem>(e => e.HasKey(x => x.IdNotaItem));
        mb.Fk<NotaFiscalItem, NotaFiscal>(x => x.IdNota);
        mb.Fk<NotaFiscalItem, Especie>(x => x.IdEspecie);

        mb.Entity<OfertaFornecedor>(e => e.HasKey(x => x.IdOferta));
        mb.Fk<OfertaFornecedor, Empresa>(x => x.IdEmpresa);
        mb.Fk<OfertaFornecedor, Especie>(x => x.IdEspecie);

        mb.Entity<Pedido>(e =>
        {
            e.HasKey(x => x.IdPedido);
            e.HasIndex(x => x.Codigo).IsUnique();
        });
        mb.Fk<Pedido, Empresa>(x => x.IdEmpresa);
        mb.Fk<Pedido, Usuario>(x => x.IdUsuario);
        mb.Fk<Pedido, Contrato>(x => x.IdContrato);

        mb.Entity<PedidoHistorico>(e => e.HasKey(x => x.IdHistorico));
        mb.Fk<PedidoHistorico, Pedido>(x => x.IdPedido);
        mb.Fk<PedidoHistorico, Usuario>(x => x.IdUsuario);

        mb.Entity<PedidoItem>(e => e.HasKey(x => x.IdPedidoItem));
        mb.Fk<PedidoItem, Pedido>(x => x.IdPedido);
        mb.Fk<PedidoItem, MedidaFornecedor>(x => x.IdMedida);
        mb.Fk<PedidoItem, Especie>(x => x.IdEspecie);

        mb.Entity<Recebimento>(e => e.HasKey(x => x.IdRecebimento));
        mb.Fk<Recebimento, Pedido>(x => x.IdPedido);
        mb.Fk<Recebimento, Usuario>(x => x.IdUsuario);
        mb.Fk<Recebimento, NotaFiscal>(x => x.IdNota);
        mb.Fk<Recebimento, Armazem>(x => x.IdArmazem);
    }
}