import { EspecieDto, TamanhoDto } from '../../../catalogos/entidades/catalogo';
import { LeituraOcrDto } from '../../servicos/ocr';
import { NOMES_PLACA } from './ler-placa';

/** O que a chegada mostra de uma placa confirmada: os 9 campos na ordem da placa e os valores (campo → valor) que preenchem o feixe. */
export interface PlacaNoFeixe {
  idLeitura: number;
  campos: { nome: string; valor: string; naoBate: boolean }[];
  valores: Record<string, string>;
}

/** O que a placa preenche no feixe (nulo: a placa não disse ou não deu para reconhecer; o operador escolhe). */
export interface FeixeDaPlaca {
  idEspecie: number | null;
  idTamanho: number | null;
  classe: number | null;
  tratamento: string | null;
}

const ORDEM = ['Fornecedor', 'Ata', 'EspecieClasse', 'Comprimento', 'Tratamento', 'Tratadora', 'Inspetora', 'Ano', 'Contrato'];

/** Sem acento, sem espaço e sem pontuação, em minúsculas (como a Chave do back): "Ocotea neesiana" vira "ocoteaneesiana". */
const chave = (texto: string) => texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().replace(/[^a-z0-9]/g, '');

/** A placa confirmada: vale o valor que o operador confirmou; sem ele, o que o OCR leu. */
export function placaNoFeixe(leitura: LeituraOcrDto): PlacaNoFeixe {
  const valores: Record<string, string> = {};
  for (const c of leitura.campos) valores[c.campo] = c.valorConfirmado ?? c.valorLido ?? '';
  return {
    idLeitura: leitura.id,
    campos: ORDEM.map((campo) => ({
      nome: NOMES_PLACA[campo],
      valor: valores[campo] ?? '',
      naoBate: leitura.campos.some((c) => c.campo === campo && c.conferePedido === false),
    })),
    valores,
  };
}

/**
 * Da placa para o feixe: a espécie pelo nome científico ou popular ("OCOTEA NEESIANA, LAURACEAE / 1a"), o tamanho pelo comprimento
 * ("2,80 M"), a classe pelo fim de "espécie e classe" ("1a", "2ª") e o tratamento como está.
 */
export function feixeDaPlaca(valores: Record<string, string>, especies: EspecieDto[], tamanhos: TamanhoDto[]): FeixeDaPlaca {
  const especieClasse = valores['EspecieClasse'] ?? '';
  const nome = chave(especieClasse.split(/[,/]/)[0]);
  const especie = nome ? especies.find((e) => chave(e.nomeCientifico) === nome || (e.nomePopular !== null && chave(e.nomePopular) === nome)) : undefined;
  const numero = (valores['Comprimento'] ?? '').match(/\d+[.,]\d+/);
  const metros = numero ? parseFloat(numero[0].replace(',', '.')) : NaN;
  const tamanho = tamanhos.find((t) => Math.abs(t.comprimentoM - metros) < 0.001);
  const classe = especieClasse.match(/([12])\s*[ªa°º]?\s*(classe)?\s*$/i);
  return {
    idEspecie: especie?.id ?? null,
    idTamanho: tamanho?.id ?? null,
    classe: classe ? Number(classe[1]) : null,
    tratamento: (valores['Tratamento'] ?? '').trim() || null,
  };
}
