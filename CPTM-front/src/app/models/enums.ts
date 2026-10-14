/** Os enums do back (vão como texto no JSON). */
export type MetodoCaptura = 'Pistola' | 'ModuloUhf' | 'Camera' | 'Nfc' | 'Digitado';
export type PapelEmpresa = 'Fornecedor' | 'Tratadora' | 'Inspetora';
export type AtaqueBiologicoVerificacao = 'Nenhum' | 'Suspeito' | 'Presente';
export type RachaduraVerificacao = 'Nenhuma' | 'Leve' | 'Grave';
export type ResultadoVerificacao = 'Aprovada' | 'Reprovada';
export type EstadoConservacao = 'Bom' | 'Regular' | 'Ruim';
export type RachaduraInspecao = 'Nenhuma' | 'Leve' | 'Media' | 'Grave';
export type AtaqueBiologicoInspecao = 'Nenhum' | 'Inicio' | 'Avancado';
export type StatusAlerta = 'Aberto' | 'Confirmado' | 'Descartado';
export type OrigemAlerta = 'Sensor' | 'Imagem';
export type StatusTroca = 'Pedida' | 'Aprovada' | 'AguardandoPeca' | 'Separada' | 'Feita' | 'Reprovada' | 'Recusada' | 'Cancelada';
export type DecisaoRecusa = 'Manter' | 'Aceitar';

/** Como a tag foi lida na tela web: módulo do celular ou digitada. A pistola agora é a NIR (madeira); 'Pistola' fica só no histórico. */
export const METODOS_WEB: { valor: MetodoCaptura; rotulo: string }[] = [
  { valor: 'ModuloUhf', rotulo: 'Módulo do celular' },
  { valor: 'Digitado', rotulo: 'Digitada' },
];

export interface LeituraDto {
  tag: string;
  metodo: MetodoCaptura;
  tagsVistas?: number;
  idLeitor?: number | null;
  idModo?: number | null;
  potenciaDbm?: number | null;
  rssiDbm?: number | null;
}