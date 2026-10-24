import { Injectable, computed, signal } from '@angular/core';

const CHAVE = 'ybyra.tema';

export type PreferenciaTema = 'sistema' | 'claro' | 'escuro';
const ORDEM: PreferenciaTema[] = ['sistema', 'claro', 'escuro'];
const ROTULOS: Record<PreferenciaTema, string> = { sistema: 'Sistema', claro: 'Claro', escuro: 'Escuro' };

/**
 * O tema da tela (Victor, 2026-10-24): Sistema (padrão: segue o claro ou o escuro do celular ou do computador, e muda junto),
 * Claro (o creme da marca) ou Escuro (o verde mata). O botão do alto passa pelas três. A escolha fica no navegador;
 * se o navegador bloquear, vale só enquanto a página estiver aberta. As cores dos dois temas estão em styles.scss.
 */
@Injectable({ providedIn: 'root' })
export class Tema {
  private readonly midia = typeof matchMedia === 'function' ? matchMedia('(prefers-color-scheme: dark)') : null;
  private readonly sistemaEscuro = signal(this.midia?.matches ?? false);
  readonly preferencia = signal<PreferenciaTema>(Tema.ler());
  readonly escuro = computed(() => this.preferencia() === 'escuro' || (this.preferencia() === 'sistema' && this.sistemaEscuro()));
  readonly rotulo = computed(() => ROTULOS[this.preferencia()]);
  readonly proximo = computed(() => ROTULOS[ORDEM[(ORDEM.indexOf(this.preferencia()) + 1) % ORDEM.length]]);

  constructor() {
    this.midia?.addEventListener('change', (e) => {
      this.sistemaEscuro.set(e.matches);
      this.aplicar();
    });
    this.aplicar();
  }

  /** Sistema → Claro → Escuro → Sistema. */
  alternar(): void {
    this.preferencia.update((p) => ORDEM[(ORDEM.indexOf(p) + 1) % ORDEM.length]);
    this.aplicar();
    try {
      localStorage.setItem(CHAVE, this.preferencia());
    } catch {
      /* sem armazenamento: segue só nesta página */
    }
  }

  private aplicar(): void {
    if (this.escuro()) document.documentElement.dataset['theme'] = 'dark';
    else delete document.documentElement.dataset['theme'];
  }

  private static ler(): PreferenciaTema {
    try {
      const salvo = localStorage.getItem(CHAVE);
      return salvo === 'claro' || salvo === 'escuro' ? salvo : 'sistema';
    } catch {
      return 'sistema';
    }
  }
}
