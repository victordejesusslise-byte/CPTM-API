import { Cargo } from '../models/usuario';

const br = 'pt-BR';

/** 3.19872 → "3,199 m³" */
export const m3 = (valor: number | null | undefined): string =>
  valor == null ? '—' : `${valor.toLocaleString(br, { maximumFractionDigits: 3 })} m³`;

/** 2.8 → "2,80 m" */
export const metros = (valor: number | null | undefined): string =>
  valor == null ? '—' : `${valor.toLocaleString(br, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} m`;

/** 213.12 → "213,120" */
export const km = (valor: number | null | undefined): string =>
  valor == null ? '—' : valor.toLocaleString(br, { minimumFractionDigits: 3, maximumFractionDigits: 3 });

/** ISO → "11/10/2026 14:05" */
export const dataHora = (iso: string | null | undefined): string =>
  !iso ? '—' : new Date(iso).toLocaleString(br, { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

/** ISO → "11/10/2026" */
export const data = (iso: string | null | undefined): string =>
  !iso ? '—' : new Date(iso).toLocaleDateString(br);

const nomes: Record<Cargo, string> = {
  Supervisor: 'Supervisor', Tecnico: 'Técnico', Especialista: 'Especialista', OperadorPatio: 'Operador de pátio', OperadorVia: 'Operador da via',
};

/** "OperadorPatio" → "Operador de pátio" */
export const nomeDoCargo = (cargo: Cargo | null | undefined): string => (cargo ? nomes[cargo] : '—');

/** Lê a mensagem que a API manda (ProblemDetails.detail) ou uma genérica. */
export const mensagemDeErro = (erro: unknown): string => {
  const e = erro as { error?: { detail?: string; title?: string; mensagem?: string; errors?: Record<string, string[]> }; status?: number };
  if (e?.status === 0) return 'Sem conexão com o servidor.';
  if (e?.status === 403) return 'Seu cargo não pode fazer isso.';
  if (e?.error?.detail) return e.error.detail;
  if (e?.error?.mensagem) return e.error.mensagem;
  if (e?.error?.errors) return Object.values(e.error.errors).flat().join(' ');
  return e?.error?.title ?? 'Algo deu errado. Tente de novo.';
};