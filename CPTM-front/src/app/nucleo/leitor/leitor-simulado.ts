import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { ModoLeituraDto } from '../../catalogos/entidades/catalogo';
import {
  ChaveModulo, Leitor, MENSAGEM_AMBIGUA, MODOS_PADRAO, ModoDoModulo, ResultadoGravacao, ResultadoLeitura, TagVista, bipe, escolherAlvo, guardarUltimaLeitura,
} from './leitor';
import { NO_APP } from '../ao-vivo/trabalho-da-noite';
import { LeitorUsb } from './leitor-usb';

/** A cena que o painel monta: só a peça, a peça com vizinhas longe, duas peças parecidas (dúvida) ou nenhuma. */
export type Cenario = 'uma' | 'vizinhas' | 'duvida' | 'nenhuma';

/**
 * O módulo RFID simulado, usado no site e nos testes: as etiquetas "por perto" são montadas no painel do módulo.
 * Segue as regras do módulo de verdade: desconectado não lê, a chave A/N/V muda a folga, e gravar confere na releitura.
 */
@Injectable({ providedIn: 'root' })
export class LeitorSimulado implements Leitor {
  /** O módulo está "conectado"? (o painel liga e desliga) */
  readonly conectado = signal(true);
  /** A posição da chave do módulo (começa no Normal). */
  readonly chave = signal<ChaveModulo>('N');
  /** Os três modos, com a potência e a folga (os de começo ou os do servidor). */
  readonly modos = signal(MODOS_PADRAO);
  /** O modo da chave atual. */
  readonly modo = computed<ModoDoModulo>(() => this.modos()[this.chave()]);
  /** As etiquetas por perto (o painel monta). */
  readonly porPerto = signal<TagVista[]>([]);

  /** Liga o módulo simulado. */
  conectar(): void {
    this.conectado.set(true);
  }

  /** Desliga o módulo simulado. */
  desconectar(): void {
    this.conectado.set(false);
  }

  /** Muda a chave do módulo (A, N ou V). */
  trocarModo(chave: ChaveModulo): void {
    this.chave.set(chave);
  }

  /** Os modos que o servidor tem (o supervisor pode ter mudado a potência ou a folga). */
  usarModosDoServidor(lista: ModoLeituraDto[]): void {
    const atual = { ...this.modos() };
    // Troca cada modo pelo do servidor com o mesmo id; os que faltam ficam como estão.
    for (const chave of ['A', 'N', 'V'] as ChaveModulo[]) {
      const m = lista.find((x) => x.id === MODOS_PADRAO[chave].idModo);
      if (m) atual[chave] = { chave, idModo: m.id, nome: m.nome, potenciaDbm: m.potenciaDbm, folgaRssiDb: m.folgaRssiDb };
    }
    this.modos.set(atual);
  }

  /**
   * Monta as etiquetas por perto: só a peça; a peça e vizinhas longe; duas peças com sinal parecido; ou nenhuma.
   * Sem a tag da peça à frente, não monta (devolve falso): inventar uma tag fazia ler a peça errada.
   */
  montar(cenario: Cenario, tag: string): boolean {
    const alvo = tag.trim();
    if (!alvo && cenario !== 'nenhuma') return false;
    // As vizinhas usam tags de teste (BR99999999..), com sinal mais fraco que o da peça.
    const lista: Record<Cenario, TagVista[]> = {
      uma: [{ tag: alvo, rssiDbm: -45 }],
      vizinhas: [{ tag: alvo, rssiDbm: -45 }, { tag: 'BR9999999901', rssiDbm: -63 }, { tag: 'BR9999999902', rssiDbm: -71 }],
      duvida: [{ tag: alvo, rssiDbm: -48 }, { tag: 'BR9999999901', rssiDbm: -51 }],
      nenhuma: [],
    };
    this.porPerto.set(lista[cenario]);
    return true;
  }

  /** Lê a etiqueta à frente: guarda a leitura boa e apita. */
  async ler(): Promise<ResultadoLeitura> {
    const r = this.alvo();
    if (r.ok) guardarUltimaLeitura(r.leitura);
    bipe(r.ok);
    return r;
  }

  /** Grava o número na etiqueta à frente e relê. */
  async gravarEReler(tag: string): Promise<ResultadoGravacao> {
    // Sem uma etiqueta clara à frente, não grava.
    const antes = this.alvo();
    if (!antes.ok) {
      bipe(false);
      return antes;
    }
    // A etiqueta à frente passa a ter o número novo; a releitura confere o que ficou gravado.
    this.porPerto.update((l) => l.map((t) => (t.tag === antes.leitura.tag ? { ...t, tag: tag.trim() } : t)));
    const depois = this.alvo();
    bipe(depois.ok);
    return depois.ok ? { ok: true, relida: depois.leitura.tag } : depois;
  }

  /** Escolhe o alvo entre as etiquetas por perto, com a folga do modo atual. */
  private alvo(): ResultadoLeitura {
    if (!this.conectado()) return { ok: false, motivo: 'Conecte o módulo no celular.' };
    const modo = this.modo();
    const r = escolherAlvo(this.porPerto(), modo.folgaRssiDb);
    if (r.tipo === 'nenhuma') return { ok: false, motivo: 'Nenhuma etiqueta no alcance. Aproxime o celular da peça.' };
    if (r.tipo === 'duvida') return { ok: false, motivo: MENSAGEM_AMBIGUA };
    return { ok: true, leitura: { tag: r.tag, idModo: modo.idModo, potenciaDbm: modo.potenciaDbm, rssiDbm: r.rssiDbm, tagsVistas: 1 } };
  }
}

/**
 * O leitor que as telas usam: no app, o módulo de verdade no USB-C (YBYRA-RFID-P0; sem ele, as telas pedem para conectar e digitar a tag
 * continua valendo); no site, o simulado, para testes (Victor, 2026-11-11).
 */
export const LEITOR = new InjectionToken<Leitor>('Leitor RFID', { providedIn: 'root', factory: () => (inject(NO_APP) ? inject(LeitorUsb) : inject(LeitorSimulado)) });
