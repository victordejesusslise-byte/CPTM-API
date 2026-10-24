namespace ViaPermanente.Dominio.Pedidos.Models;

/// <summary>
/// MEDIDA_FORNECEDOR: um padrão de largura × altura do fornecedor (Victor, 2026-10-23). O fornecedor pode ter vários; o padrão vale para
/// uma espécie ou, sem espécie, para todas. Cada item do pedido escolhe a espécie e o padrão, e o m³ é comprimento × largura × altura.
/// Não se edita: um padrão errado é desativado e outro é cadastrado, para o m³ dos pedidos antigos não mudar.
/// </summary>
public class MedidaFornecedor
{
    public int IdMedida { get; set; }
    public int IdEmpresa { get; set; }
    public int? IdEspecie { get; set; }
    public decimal LarguraM { get; set; }
    public decimal AlturaM { get; set; }
    public bool Ativo { get; set; } = true;
}
