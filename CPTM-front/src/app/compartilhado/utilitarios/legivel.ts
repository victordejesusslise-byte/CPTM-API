import { Pipe, PipeTransform } from '@angular/core';

/** Os nomes técnicos que a API manda e que pedem um texto próprio (o resto sai separando as palavras e pondo os acentos). */
const NOMES: Record<string, string> = {
  AguardandoVerificacao: 'Aguardando verificação',
  AguardandoPeca: 'Aguardando peça',
  NaVia: 'Na via',
  NoArmazem: 'No armazém',
  RecebidoParcial: 'Recebido parcial',
  EmTransito: 'Em trânsito',
  TrocaDePilha: 'Troca de pilha',
  TagMuda: 'Tag não respondeu',
  ModuloUhf: 'Módulo do celular',
  ModuloRfid: 'Módulo RFID',
  PistolaNir: 'Pistola NIR',
  OperadorPatio: 'Operador do pátio',
  OperadorVia: 'Operador da via',
  PlacaFeixe: 'Placa do feixe',
  PlacaFornecedor: 'Placa do fornecedor',
  EspecieClasse: 'Espécie e classe',
  PlanilhaNota: 'Planilha da nota',
  CodigoMaterial: 'Código do material',
  MetroCubico: 'Metro cúbico',
};

/** As palavras que o nome técnico escreve sem acento. */
const ACENTOS: Record<string, string> = {
  verificacao: 'verificação', armazem: 'armazém', transito: 'trânsito', peca: 'peça', saida: 'saída', posicao: 'posição',
  critico: 'crítico', conferencia: 'conferência', inspecao: 'inspeção', classificacao: 'classificação', separacao: 'separação',
  integracao: 'integração', especie: 'espécie', cubico: 'cúbico', patio: 'pátio', codigo: 'código', devolucao: 'devolução',
  modulo: 'módulo', tecnico: 'técnico', camera: 'câmera', medio: 'médio', media: 'média', nao: 'não', unica: 'única',
};

/**
 * O nome técnico que vem da API ("AguardandoVerificacao", "Saida") em português de gente ("Aguardando verificação", "Saída")
 * (Victor, 2026-10-24). Texto que não é nome técnico (código, tag, nome de pessoa, frase) fica como está.
 */
export const legivel = <T>(valor: T): T | string => {
  if (typeof valor !== 'string' || !/^[A-Z][a-z]+(?:[A-Z][a-z]*)*$/.test(valor)) return valor;
  if (NOMES[valor]) return NOMES[valor];
  return valor
    .split(/(?=[A-Z])/)
    .map((p, i) => {
      const palavra = ACENTOS[p.toLowerCase()] ?? p.toLowerCase();
      return i === 0 ? palavra[0].toUpperCase() + palavra.slice(1) : palavra;
    })
    .join(' ');
};

/** Uso: {{ peca.situacao | legivel }} */
@Pipe({ name: 'legivel' })
export class LegivelPipe implements PipeTransform {
  transform<T>(valor: T): T | string {
    return legivel(valor);
  }
}
