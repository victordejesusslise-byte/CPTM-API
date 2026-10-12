import { LeituraDto, OrigemAlerta, StatusAlerta, StatusTroca } from './enums';
import { PecaDto } from './peca';

export interface DeteccaoDto { id: number; quadroArquivo: string; camera: string | null; caixaX: number; caixaY: number; caixaLargura: number; caixaAltura: number; defeito: string; grau: number; confianca: number; versaoModelo: string; }
export interface AlertaDto {
  id: number; origem: OrigemAlerta; tipo: string; status: StatusAlerta; data: string; desvioMm: number | null; km: number | null;
  idDormente: number | null; codigoPeca: string | null; tag: string | null; deteccao: DeteccaoDto | null; vistorias: number;
  idUsuarioDecisao: number | null; dataDecisao: string | null; motivoDescarte: string | null;
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
}
export interface CompativelDto { peca: PecaDto; distanciaKm: number | null; idadeDias: number; sugerida: boolean; }
export interface ExecutarTrocaDto { leituraSaiu?: LeituraDto | null; tagSaiuNaoResponde: boolean; leituraEntrou: LeituraDto; equipe: string; fotoArquivo: string; km?: number | null; }
export interface RecusarTrocaDto { motivo: string; descricao?: string | null; fotoArquivo: string; }