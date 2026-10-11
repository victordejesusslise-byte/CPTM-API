import { PedidoDto } from '../../models/pedido';

/** A situação do pedido como no protótipo: rótulo e pílula (vermelho só na falta e no cancelado). */
const SITUACOES: Record<string, { rotulo: string; pilula: string }> = {
  Rascunho: { rotulo: 'Rascunho', pilula: 'pilula--quieta' },
  Aberto: { rotulo: 'Aberto', pilula: '' },
  EmTransito: { rotulo: 'Em trânsito', pilula: '' },
  RecebidoParcial: { rotulo: 'Recebido parcial', pilula: 'pilula--alerta' },
  Recebido: { rotulo: 'Recebido', pilula: 'pilula--solida' },
  Cancelado: { rotulo: 'Cancelado', pilula: 'pilula--alerta-solida' },
};
export const OPCOES_SITUACAO = Object.entries(SITUACOES).map(([valor, s]) => ({ valor, rotulo: s.rotulo }));
export const situacao = (p: PedidoDto) => SITUACOES[p.situacao] ?? { rotulo: p.situacao, pilula: '' };
export const cancelado = (p: PedidoDto) => p.situacao === 'Cancelado';
export const faltaChegar = (p: PedidoDto) => (cancelado(p) ? 0 : Math.max(0, p.quantidade - p.pecasRecebidas));
/** Chegou carga, mas não tudo. */
export const entregaIncompleta = (p: PedidoDto) => !cancelado(p) && p.pecasRecebidas > 0 && p.pecasRecebidas < p.quantidade;
/** Corrigir e cancelar só até a primeira carga chegar. */
export const editavel = (p: PedidoDto) => !cancelado(p) && p.pecasRecebidas === 0;

export const hojeIso = (): string => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

export type Periodo = 'semana' | 'mes' | '90' | 'all';
export const PERIODOS: { valor: Periodo; rotulo: string }[] = [
  { valor: 'semana', rotulo: 'Semana' }, { valor: 'mes', rotulo: 'Mês' }, { valor: '90', rotulo: '90 dias' }, { valor: 'all', rotulo: 'Tudo' },
];
export const dentroDoPeriodo = (dataIso: string, periodo: Periodo, hoje = new Date()): boolean => {
  const d = new Date(dataIso.length === 10 ? `${dataIso}T12:00:00` : dataIso);
  if (periodo === 'all') return true;
  if (periodo === 'mes') return d.getMonth() === hoje.getMonth() && d.getFullYear() === hoje.getFullYear();
  const dias = periodo === 'semana' ? 7 : 90;
  return d.getTime() >= hoje.getTime() - dias * 86_400_000;
};