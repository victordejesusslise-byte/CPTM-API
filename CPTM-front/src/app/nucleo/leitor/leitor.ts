import { Signal } from '@angular/core';
import { LeituraDto } from '../../compartilhado/entidades/enums';

/*
 * O leitor RFID do celular: as regras comuns ao módulo de verdade (leitor-usb.ts) e ao simulado (leitor-simulado.ts).
 * Ler aceita várias etiquetas por perto, mas só vale a que for claramente a mais forte; gravar só vale se a releitura bater.
 * (decisões: Leitura tolerante, gravação rigorosa; Nada muda de estado sem alvo único)
 */

/** A chave física do módulo: A Armazém, N Normal, V Via. */
export type ChaveModulo = 'A' | 'N' | 'V';

/** Um modo de leitura do módulo (→ MODO_LEITURA). */
export interface ModoDoModulo {
  /** A posição da chave do módulo. */
  chave: ChaveModulo;
  /** O id do modo (→ MODO_LEITURA): 1 Armazém, 2 Normal, 3 Via. */
  idModo: number;
  /** O nome do modo: "Armazém", "Normal" ou "Via". */
  nome: string;
  /** A potência do módulo, em dBm (mais potência, mais alcance). */
  potenciaDbm: number;
  /** A folga, em dB: quanto a etiqueta mais forte precisa passar das outras para valer sozinha. */
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
  /** O número da etiqueta: BR + 10 dígitos (ex.: BR0000000032). */
  tag: string;
  /** O sinal, em dBm (ex.: -45 perto, -70 longe). */
  rssiDbm: number;
}

/** Uma leitura boa do módulo. */
export interface LeituraDoModulo {
  /** O número da etiqueta lida (BR + 10 dígitos). */
  tag: string;
  /** O modo em que foi lida (→ MODO_LEITURA). */
  idModo: number;
  /** A potência usada, em dBm. Nulos no protótipo NFC (YBYRA-RFID-P0): ele lê de perto, sem potência nem sinal. */
  potenciaDbm: number | null;
  /** O sinal da etiqueta, em dBm (nulo no protótipo NFC). */
  rssiDbm: number | null;
  /** Quantas etiquetas o módulo viu: sempre 1, porque só vale com uma. */
  tagsVistas: 1;
}

/** O resultado de ler: a leitura, ou o motivo de não ter lido. */
export type ResultadoLeitura = { ok: true; leitura: LeituraDoModulo } | { ok: false; motivo: string };
/** O resultado de gravar: o número relido, ou o motivo de não ter gravado. */
export type ResultadoGravacao = { ok: true; relida: string } | { ok: false; motivo: string };

/** A mesma mensagem do servidor (AlvoUnico.MensagemAmbigua). */
export const MENSAGEM_AMBIGUA = 'Mais de um dormente próximo. Aproxime ou direcione melhor.';

/**
 * O leitor RFID que as telas usam: liga pelo USB-C, lê a etiqueta à frente, grava e relê, e apita.
 * No app é o LeitorUsb (o módulo de verdade); no site, o LeitorSimulado. As telas são as mesmas para os dois.
 * Digitar a tag continua valendo como plano B.
 */
export interface Leitor {
  /** O módulo está ligado e respondendo? */
  readonly conectado: Signal<boolean>;
  /** O modo do módulo agora (no protótipo USB, sempre o Normal). */
  readonly modo: Signal<ModoDoModulo>;
  /** Liga o módulo. */
  conectar(): void;
  /** Desliga o módulo. */
  desconectar(): void;
  /** A chave física mudou de posição (MODE_CHANGED). */
  trocarModo(chave: ChaveModulo): void;
  /** Varre e devolve o alvo único, com o modo, a potência e o sinal. */
  ler(): Promise<ResultadoLeitura>;
  /** Grava o número na etiqueta à frente (sozinha no alcance) e relê. */
  gravarEReler(tag: string): Promise<ResultadoGravacao>;
}

/** O que a varredura achou: um alvo claro, dúvida ou nenhuma etiqueta. */
export type ResultadoAlvo = { tipo: 'alvo'; tag: string; rssiDbm: number } | { tipo: 'duvida'; tagsVistas: number } | { tipo: 'nenhuma' };

/**
 * O alvo único: a etiqueta mais forte só vale se as outras estiverem pelo menos a folga do modo abaixo dela (Armazém 12 dB,
 * Normal 9, Via 6). As que ficam dentro da folga contam como vistas, e o servidor só aceita a leitura do módulo com uma.
 */
export function escolherAlvo(vistas: readonly TagVista[], folgaDb: number): ResultadoAlvo {
  if (!vistas.length) return { tipo: 'nenhuma' };
  // Põe a mais forte primeiro.
  const ordem = [...vistas].sort((a, b) => b.rssiDbm - a.rssiDbm);
  const forte = ordem[0];
  // Conta quantas estão perto demais do sinal da mais forte (ela mesma conta).
  const naFolga = ordem.filter((t) => forte.rssiDbm - t.rssiDbm < folgaDb).length;
  // Só ela: é o alvo. Mais de uma: dúvida.
  return naFolga === 1 ? { tipo: 'alvo', tag: forte.tag, rssiDbm: forte.rssiDbm } : { tipo: 'duvida', tagsVistas: naFolga };
}

/** A última leitura boa do módulo. */
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
    // Cada tom: [frequência em Hz, quando começa em segundos].
    const tons: [number, number][] = ok ? [[1800, 0]] : [[420, 0], [420, 0.2]];
    for (const [frequencia, inicio] of tons) {
      // Toca o tom baixinho, por uma fração de segundo.
      const oscilador = som.createOscillator();
      const volume = som.createGain();
      oscilador.frequency.value = frequencia;
      volume.gain.value = 0.15;
      oscilador.connect(volume).connect(som.destination);
      oscilador.start(som.currentTime + inicio);
      oscilador.stop(som.currentTime + inicio + (ok ? 0.09 : 0.12));
    }
    // Fecha o som depois de tocar.
    setTimeout(() => som.close(), 800);
  } catch {
    /* sem som */
  }
}
