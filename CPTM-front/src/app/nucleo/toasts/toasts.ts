import { Injectable, signal } from '@angular/core';

export type TipoToast = 'erro' | 'ok';
export interface Toast {
  id: number;
  texto: string;
  tipo: TipoToast;
}

/** Quanto tempo o toast fica: o erro um pouco mais, para dar tempo de ler. */
const DURACAO_MS: Record<TipoToast, number> = { erro: 8000, ok: 5000 };
/** A mesma mensagem dentro deste tempo não vira outro toast. */
const REPETIDA_MS = 4000;

/** Os toasts da tela: no máximo 3 de cada vez, cada um some sozinho ou no X (Victor, 2026-10-31). */
@Injectable({ providedIn: 'root' })
export class Toasts {
  readonly lista = signal<Toast[]>([]);
  private proximo = 1;
  private readonly recentes = new Map<string, number>();

  mostrar(texto: string, tipo: TipoToast): void {
    const limpo = texto.replace(/\s+/g, ' ').trim();
    if (!limpo) return;
    const chave = tipo + ':' + limpo;
    const agora = Date.now();
    if ((this.recentes.get(chave) ?? 0) > agora - REPETIDA_MS) return;
    this.recentes.set(chave, agora);
    const toast: Toast = { id: this.proximo++, texto: limpo, tipo };
    this.lista.update((l) => [...l, toast].slice(-3));
    setTimeout(() => this.fechar(toast.id), DURACAO_MS[tipo]);
  }

  fechar(id: number): void {
    this.lista.update((l) => l.filter((t) => t.id !== id));
  }
}
