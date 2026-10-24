export interface LimiteDto { valido: boolean; percentual: number; unidade: string; limite: number; mensagem: string; }
export interface PedidoItemDto { id: number; idTamanho: number; comprimentoM: number; classe: number; idEspecie: number | null; especie: string | null; quantidade: number; volumeM3: number; idMedida: number; medida: string; }
export interface PedidoDto {
  id: number; codigo: string; idEmpresa: number; empresa: string; idContrato: number | null; contrato: string | null;
  dataCompra: string; criadoEm: string; quantidade: number; volumeM3: number;
  status: string; situacao: string; pecasRecebidas: number; segundaClasse: LimiteDto; itens: PedidoItemDto[];
}
export interface PedidoHistoricoDto { id: number; data: string; idUsuario: number; motivo: string; retratoAntes: string; retratoDepois: string; }
/** O item do pedido: comprimento, classe, espécie opcional e o padrão de medida do fornecedor (largura × altura). */
export interface NovoPedidoItemDto { idTamanho: number; classe: number; idEspecie?: number | null; quantidade: number; idMedida: number; }
export interface NovoPedidoDto { codigo: string; idEmpresa: number; idContrato?: number | null; dataCompra?: string | null; itens: NovoPedidoItemDto[]; }
export interface AvisoOfertaDto { idTamanho: number; classe: number; idEspecie: number | null; volumeM3Pedido: number; volumeM3Ofertado: number | null; mensagem: string; }
export interface VerificacaoPedidoDto { segundaClasse: LimiteDto; oferta: AvisoOfertaDto[]; }

export interface NotaItemDto { id: number; idEspecie: number; especie: string; idTamanho: number; comprimentoM: number; classe: number; quantidade: number; volumeM3Declarado: number; volumeM3Catalogo: number; }
export interface NotaDto {
  id: number; idPedido: number; numero: string; dataEmissao: string; valor: number | null; arquivo: string | null;
  origem: string; statusConferencia: string; idUsuarioConferiu: number | null; dataConferencia: string | null;
  itens: NotaItemDto[]; divergencias: string[]; avisos: string[];
}
export interface NovaNotaItemDto { idEspecie: number; idTamanho: number; classe: number; quantidade: number; volumeM3: number; }
export interface NovaNotaDto { numero: string; dataEmissao?: string | null; valor?: number | null; arquivo?: string | null; itens: NovaNotaItemDto[]; }

export interface FeixeDto {
  id: number; numero: string; quantidade: number; cadastradas: number; idEspecie: number; especie: string; classe: number;
  idTamanho: number; comprimentoM: number; tratamento: string | null; idEmpresaTratamento: number | null; idEmpresaInspecao: number | null; anoInspecao: number | null;
}
export interface ChegadaDto {
  id: number; idPedido: number; idNota: number | null; numeroNota: string | null; idArmazem: number; armazem: string;
  documentoOrigemTipo: string | null; documentoOrigemNumero: string | null; dataRecebimento: string; quantidadeRecebida: number; observacao: string | null;
  idLote: number; lote: string; feixes: FeixeDto[]; confere: boolean; divergencias: string[]; avisos: string[];
}
/** classe: sem ela, vale a do item do pedido; obrigatória quando o pedido tem 1ª e 2ª para o mesmo feixe. */
export interface NovoFeixeDto { numeroFeixe: string; quantidade: number; idEspecie: number; idTamanho: number; classe?: number | null; tratamento?: string | null; idEmpresaTratamento?: number | null; idEmpresaInspecao?: number | null; anoInspecao?: number | null; idLeituraOcr?: number | null; }
export interface NovaChegadaDto {
  idNota?: number | null; idArmazem: number; documentoOrigemTipo?: string | null; documentoOrigemNumero?: string | null;
  dataRecebimento?: string | null; quantidadeRecebida: number; observacao?: string | null; feixes: NovoFeixeDto[];
}export interface OfertaDto { id: number; idEmpresa: number; empresa: string; idEspecie: number; especie: string; idTamanho: number; comprimentoM: number; volumeM3Disponivel: number; data: string; }
