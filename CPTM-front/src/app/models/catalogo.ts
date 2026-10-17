export interface TamanhoDto { id: number; comprimentoM: number; larguraM: number; alturaM: number; volumeM3: number; medidaProvisoria: boolean; }
export interface EspecieDto { id: number; nomeCientifico: string; familia: string; nomePopular: string | null; }
export interface PilhaDto { id: number; codigo: string; setor: string; comprimentoM: number; classe: number; pecas: number; }
export interface ArmazemDto { id: number; nome: string; linha: string; km: number | null; pecas: number; pilhas: PilhaDto[]; }
export interface LeitorDto { id: number; codigo: string; idTipoLeitor: number; tipo: string; numeroSerie: string | null; ativo: boolean; }
export interface ModoLeituraDto { id: number; nome: string; potenciaDbm: number; folgaRssiDb: number; antena: string; exigeAlvoUnico: boolean; }