import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { ModoLeituraDto } from '../../catalogos/entidades/catalogo';
import {
  ChaveModulo, Leitor, MENSAGEM_AMBIGUA, MODOS_PADRAO, ModoDoModulo, ResultadoGravacao, ResultadoLeitura, TagVista, bipe, escolherAlvo, guardarUltimaLeitura,
} from './leitor';

export type Cenario = 'uma' | 'vizinhas' | 'duvida' | 'nenhuma';

/**
 * O módulo RFID simulado (até o de verdade existir): o "mundo" são as etiquetas por perto, com o sinal, que o painel do módulo monta.
 * Segue o fluxo do hardware: sem o módulo conectado não lê; a chave A/N/V muda a folga do alvo único; gravar exige a etiqueta
 * sozinha à frente e confere na releitura.
 */
@Injectable({ providedIn: 'root' })
export class LeitorSimulado implements Leitor {
  readonly conectado = signal(true);
  readonly chave = signal<ChaveModulo>('N');
  readonly modos = signal(MODOS_PADRAO);
  readonly modo = computed<ModoDoModulo>(() => this.modos()[this.chave()]);
  /** As etiquetas por perto (o painel monta). */
  readonly porPerto = signal<TagVista[]>([]);

  conectar(): void {
    this.conectado.set(true);
  }

  desconectar(): void {
    this.conectado.set(false);
  }

  trocarModo(chave: ChaveModulo): void {
    this.chave.set(chave);
  }

  /** Os modos que o servidor tem (o supervisor pode ter mudado a potência ou a folga). */
  usarModosDoServidor(lista: ModoLeituraDto[]): void {
    const atual = { ...this.modos() };
    for (const chave of ['A', 'N', 'V'] as ChaveModulo[]) {
      const m = lista.find((x) => x.id === MODOS_PADRAO[chave].idModo);
      if (m) atual[chave] = { chave, idModo: m.id, nome: m.nome, potenciaDbm: m.potenciaDbm, folgaRssiDb: m.folgaRssiDb };
    }
    this.modos.set(atual);
  }

  /** Monta as etiquetas por perto: só a peça; a peça e vizinhas longe; duas peças com sinal parecido; ou nenhuma. */
  montar(cenario: Cenario, tag: string): void {
    const alvo = tag.trim() || 'BR0000000001';
    const lista: Record<Cenario, TagVista[]> = {
      uma: [{ tag: alvo, rssiDbm: -45 }],
      vizinhas: [{ tag: alvo, rssiDbm: -45 }, { tag: 'BR9999999901', rssiDbm: -63 }, { tag: 'BR9999999902', rssiDbm: -71 }],
      duvida: [{ tag: alvo, rssiDbm: -48 }, { tag: 'BR9999999901', rssiDbm: -51 }],
      nenhuma: [],
    };
    this.porPerto.set(lista[cenario]);
  }

  async ler(): Promise<ResultadoLeitura> {
    const r = this.alvo();
    if (r.ok) guardarUltimaLeitura(r.leitura);
    bipe(r.ok);
    return r;
  }

  async gravarEReler(tag: string): Promise<ResultadoGravacao> {
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

  private alvo(): ResultadoLeitura {
    if (!this.conectado()) return { ok: false, motivo: 'Conecte o módulo no celular.' };
    const modo = this.modo();
    const r = escolherAlvo(this.porPerto(), modo.folgaRssiDb);
    if (r.tipo === 'nenhuma') return { ok: false, motivo: 'Nenhuma etiqueta no alcance. Aproxime o celular da peça.' };
    if (r.tipo === 'duvida') return { ok: false, motivo: MENSAGEM_AMBIGUA };
    return { ok: true, leitura: { tag: r.tag, idModo: modo.idModo, potenciaDbm: modo.potenciaDbm, rssiDbm: r.rssiDbm, tagsVistas: 1 } };
  }
}

/** O leitor que as telas usam. Hoje, o simulado; o módulo de verdade (Entrega 3) entra aqui. */
export const LEITOR = new InjectionToken<Leitor>('Leitor RFID', { providedIn: 'root', factory: () => inject(LeitorSimulado) });
