export interface TamanhoDto { id: number; comprimentoM: number; larguraM: number; alturaM: number; volumeM3: number; medidaProvisoria: boolean; }
export interface EspecieDto { id: number; nomeCientifico: string; familia: string; nomePopular: string | null; }
export interface PilhaDto { id: number; codigo: string; setor: string; comprimentoM: number; classe: number; pecas: number; }
export interface ArmazemDto { id: number; nome: string; linha: string; km: number | null; pecas: number; pilhas: PilhaDto[]; }
/** Os tipos de leitor, fixos no código (o mesmo TipoLeitor do back): o número é o que vai para a API. */
export const TIPOS_LEITOR = [
  { id: 1, nome: 'Módulo RFID (celular)' },
  { id: 2, nome: 'Pistola NIR' },
  { id: 3, nome: 'Carro de inspeção' },
] as const;

export interface LeitorDto { id: number; codigo: string; idTipoLeitor: number; tipo: string; numeroSerie: string | null; ativo: boolean; }
export interface ModoLeituraDto { id: number; nome: string; potenciaDbm: number; folgaRssiDb: number; antena: string; exigeAlvoUnico: boolean; }