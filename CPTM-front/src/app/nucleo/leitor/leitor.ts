import { Signal } from '@angular/core';
import { LeituraDto } from '../../compartilhado/entidades/enums';

/** A chave física do módulo: A Armazém, N Normal, V Via. */
export type ChaveModulo = 'A' | 'N' | 'V';

export interface ModoDoModulo {
  chave: ChaveModulo;
  idModo: number;
  nome: string;
  potenciaDbm: number;
  folgaRssiDb: number;
}

/** Os modos de começo (MODO_LEITURA, provisórios até os ensaios); o painel troca pelos do servidor quando tem rede. */
export const MODOS_PADRAO: Record<ChaveModulo, ModoDoModulo> = {
  A: { chave: 'A', idModo: 1, nome: 'Armazém', potenciaDbm: 10, folgaRssiDb: 12 },
  N: { chave: 'N', idModo: 2, nome: 'Normal', potenciaDbm: 18, folgaRssiDb: 9 },
  V: { chave: 'V', idModo: 3, nome: 'Via', potenciaDbm: 27, folgaRssiDb: 6 },
};

/** Uma etiqueta que o módulo viu na varredura, com o sinal (RSSI em dBm: quanto mais perto de zero, mais forte). */
export interface TagVista {
  tag: string;
  rssiDbm: number;
}

export interface LeituraDoModulo {
  tag: string;
  idModo: number;
  potenciaDbm: number;
  rssiDbm: number;
  tagsVistas: 1;
}

export type ResultadoLeitura = { ok: true; leitura: LeituraDoModulo } | { ok: false; motivo: string };
export type ResultadoGravacao = { ok: true; relida: string } | { ok: false; motivo: string };

/** A mesma mensagem do servidor (AlvoUnico.MensagemAmbigua). */
export const MENSAGEM_AMBIGUA = 'Mais de um dormente próximo. Aproxime ou direcione melhor.';

/**
 * O leitor RFID do celular, pelo fluxo do hardware (Módulo RFID UHF no celular): liga quando o módulo é conectado no USB-C, a chave
 * física escolhe o modo, ele varre as etiquetas por perto com o sinal, só aceita um alvo claro, grava e relê, e apita.
 * Hoje quem faz é o LeitorSimulado; o módulo de verdade (Entrega 3) entra no lugar sem mudar as telas. Digitar a tag continua
 * no campo da leitura (método "Digitada"), como plano B.
 */
export interface Leitor {
  readonly conectado: Signal<boolean>;
  readonly modo: Signal<ModoDoModulo>;
  conectar(): void;
  desconectar(): void;
  /** A chave física mudou de posição (MODE_CHANGED). */
  trocarModo(chave: ChaveModulo): void;
  /** Varre e devolve o alvo único, com o modo, a potência e o sinal. */
  ler(): Promise<ResultadoLeitura>;
  /** Grava o número na etiqueta à frente (sozinha no alcance) e relê. */
  gravarEReler(tag: string): Promise<ResultadoGravacao>;
}

export type ResultadoAlvo = { tipo: 'alvo'; tag: string; rssiDbm: number } | { tipo: 'duvida'; tagsVistas: number } | { tipo: 'nenhuma' };

/**
 * O alvo único: a etiqueta mais forte só vale se as outras estiverem pelo menos a folga do modo abaixo dela (Armazém 12 dB,
 * Normal 9, Via 6). As que ficam dentro da folga contam como vistas, e o servidor só aceita a leitura do módulo com uma.
 */
export function escolherAlvo(vistas: readonly TagVista[], folgaDb: number): ResultadoAlvo {
  if (!vistas.length) return { tipo: 'nenhuma' };
  const ordem = [...vistas].sort((a, b) => b.rssiDbm - a.rssiDbm);
  const forte = ordem[0];
  const naFolga = ordem.filter((t) => forte.rssiDbm - t.rssiDbm < folgaDb).length;
  return naFolga === 1 ? { tipo: 'alvo', tag: forte.tag, rssiDbm: forte.rssiDbm } : { tipo: 'duvida', tagsVistas: naFolga };
}

let ultima: LeituraDoModulo | null = null;

/** Guarda a última leitura do módulo, para a leitura que vai à API levar o modo, a potência e o sinal. */
export const guardarUltimaLeitura = (leitura: LeituraDoModulo): void => {
  ultima = leitura;
};

/** O modo, a potência e o sinal da última leitura do módulo, se ela foi desta tag; senão, nada. */
export const detalhesDoModulo = (tag: string): Partial<LeituraDto> =>
  ultima && ultima.tag === tag.trim() ? { idModo: ultima.idModo, potenciaDbm: ultima.potenciaDbm, rssiDbm: ultima.rssiDbm, tagsVistas: 1 } : {};

/** O bipe do módulo: um curto e agudo quando leu, dois graves quando não. Sem som no aparelho (ou nos testes), fica quieto. */
export function bipe(ok: boolean): void {
  if (typeof AudioContext === 'undefined') return;
  try {
    const som = new AudioContext();
    const tons: [number, number][] = ok ? [[1800, 0]] : [[420, 0], [420, 0.2]];
    for (const [frequencia, inicio] of tons) {
      const oscilador = som.createOscillator();
      const volume = som.createGain();
      oscilador.frequency.value = frequencia;
      volume.gain.value = 0.15;
      oscilador.connect(volume).connect(som.destination);
      oscilador.start(som.currentTime + inicio);
      oscilador.stop(som.currentTime + inicio + (ok ? 0.09 : 0.12));
    }
    setTimeout(() => som.close(), 800);
  } catch {
    /* sem som */
  }
}
