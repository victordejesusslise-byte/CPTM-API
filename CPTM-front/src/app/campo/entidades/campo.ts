import { LeituraDto, OrigemAlerta, StatusAlerta, StatusTroca } from '../../compartilhado/entidades/enums';
import { PecaDto } from '../../pecas/entidades/peca';

export interface DeteccaoDto { id: number; quadroArquivo: string; camera: string | null; caixaX: number; caixaY: number; caixaLargura: number; caixaAltura: number; defeito: string; grau: number; confianca: number; versaoModelo: string; }
export interface AlertaDto {
  id: number; origem: OrigemAlerta; tipo: string; status: StatusAlerta; data: string; desvioMm: number | null; km: number | null;
  idDormente: number | null; codigoPeca: string | null; tag: string | null; deteccao: DeteccaoDto | null; vistorias: number;
  idUsuarioDecisao: number | null; dataDecisao: string | null; motivoDescarte: string | null;
  notaPeca?: string | null;
}
export interface NovaVistoriaDto { viu: boolean; leitura?: LeituraDto | null; tagNaoResponde: boolean; fotoArquivo: string; observacao?: string | null; idCliente?: string; }
export interface NovoAvisoTagDto { tag: string; km?: number | null; observacao?: string | null; }
export interface AvisoTagDto { idLeitura: number; tag: string; km: number | null; observacao: string | null; data: string; idUsuario: number; }

export interface RecusaDto { id: number; data: string; idUsuario: number; motivo: string; descricao: string | null; fotoArquivo: string; decisao: string | null; dataDecisao: string | null; resposta: string | null; }
export interface TrocaDto {
  id: number; status: StatusTroca; origem: string;
  idDormenteSaiu: number; codigoSaiu: string | null; tagSaiu: string | null; idDormenteEntrou: number | null; codigoEntrou: string | null; tagEntrou: string | null;
  idInspecao: number | null; idTrecho: number | null; km: number | null; motivo: string | null; observacao: string | null;
  idUsuarioPediu: number; dataPedido: string; idUsuarioAprovou: number | null; dataAprovacao: string | null; motivoReprovacao: string | null;
  idUsuarioSeparou: number | null; dataSeparacao: string | null; idUsuarioFez: number | null; data: string | null; equipe: string | null; fotoArquivo: string | null;
  cancelamento: string | null; diasDesdeOPedido: number; ultimaRecusa: RecusaDto | null;
  /** A troca feita num lote de troca. */
  idLoteTroca?: number | null;
}

/** Lote de troca (Victor, 2026-11-11): várias trocas de uma vez; as peças só aparecem na noite, pela tag. */
export type StatusLoteTroca = 'Pedido' | 'Aprovado' | 'AguardandoPeca' | 'Separado' | 'Feito' | 'Reprovado' | 'Recusado' | 'Cancelado';
export interface PecaDoLoteDto { id: number; codigo: string; tag: string | null; kmProjeto: number | null; idTamanho: number; pilha: string | null; }
/**
 * Uma troca avulsa já aprovada de uma peça na faixa e no comprimento do lote (Victor, 2026-11-12): a via troca no lote ou diz que não deu.
 * Soma à quantidade aprovada. separada: a peça que o pátio já separou para o avulso, que entra só no lugar dela.
 */
export interface ObrigatoriaDto { idTroca: number; situacao: 'Aberta' | 'Trocada' | 'NaoDeu'; peca: PecaDoLoteDto; separada: PecaDoLoteDto | null; motivo: string | null; }
export interface LoteTrocaDto {
  id: number; codigo: string; status: StatusLoteTroca; idTrecho: number; trecho: string; classeExigida: number;
  kmInicio: number; kmFim: number; idTamanho: number; comprimentoM: number; quantidadePedida: number; quantidadeAprovada: number | null;
  motivo: string; observacao: string | null; idUsuarioPediu: number; dataPedido: string; idUsuarioAprovou: number | null; dataAprovacao: string | null;
  motivoReprovacao: string | null; motivoMenos: string | null; noiteDoPlano: string | null; urgente: boolean; idUsuarioFez: number | null; data: string | null;
  equipe: string | null; cancelamento: string | null;
  separadas: PecaDoLoteDto[]; devolvidas: PecaDoLoteDto[]; trocas: TrocaDto[]; pecasDaFaixa: PecaDoLoteDto[]; avulsasAbertas: PecaDoLoteDto[]; ultimaRecusa: RecusaDto | null;
  obrigatorias: ObrigatoriaDto[];
}
export interface NovoLoteTrocaDto { idTrecho: number; kmInicio: number; kmFim: number; quantidade: number; idTamanho: number; motivo: string; observacao?: string | null; }
/** Um dormente trocado no lote; a foto vai pela fila, no par da mesma posição. */
export interface ParDoLoteDto { leituraSaiu: LeituraDto | null; tagSaiuNaoResponde: boolean; idDormenteSaiu: number | null; leituraEntrou: LeituraDto; km?: number | null; registradoEm?: string; }
/** A obrigatória do lote que a via não conseguiu trocar: motivo e foto (a foto vai pela fila, depois das dos pares). */
export interface NaoDeuDto { idTroca: number; motivo: string; descricao?: string | null; registradoEm?: string; }
export interface CompativelDto { peca: PecaDto; distanciaKm: number | null; idadeDias: number; sugerida: boolean; }
export interface ExecutarTrocaDto { leituraSaiu?: LeituraDto | null; tagSaiuNaoResponde: boolean; leituraEntrou: LeituraDto; equipe: string; fotoArquivo: string; km?: number | null; idCliente?: string; registradoEm?: string; }
export interface RecusarTrocaDto { motivo: string; descricao?: string | null; fotoArquivo: string; idCliente?: string; registradoEm?: string; }