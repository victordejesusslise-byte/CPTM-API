import { Injectable, signal } from '@angular/core';

const CHAVE = 'ybyra.tema';

/**
 * O tema da tela: claro (padrão, o creme da marca) ou escuro (o verde mata). A escolha fica no navegador;
 * se o navegador bloquear, vale só enquanto a página estiver aberta. As cores dos dois estão em styles.scss.
 */
@Injectable({ providedIn: 'root' })
export class Tema {
  readonly escuro = signal(Tema.ler());

  constructor() {
    this.aplicar();
  }

  alternar(): void {
    this.escuro.update((e) => !e);
    this.aplicar();
    try {
      localStorage.setItem(CHAVE, this.escuro() ? 'escuro' : 'claro');
    } catch {
      /* sem armazenamento: segue só nesta página */
    }
  }

  private aplicar(): void {
    if (this.escuro()) document.documentElement.dataset['theme'] = 'dark';
    else delete document.documentElement.dataset['theme'];
  }

  private static ler(): boolean {
    try {
      return localStorage.getItem(CHAVE) === 'escuro';
    } catch {
      return false;
    }
  }
}
