import { Component, Injectable, inject, signal } from '@angular/core';
import { Capacitor } from '@capacitor/core';

/** Quanto tempo a tela com o logo fica no app (Victor, 2026-10-24): ao abrir e depois de entrar. */
export const TEMPO_DA_ABERTURA_MS = 5000;

/**
 * A tela com o logo YBYRA no app: aparece ao abrir (continua a tela de carregamento do index.html até completar 5 segundos)
 * e depois do login ("Entrando…"). No site do navegador ela não segura nada: some assim que o Angular abre.
 */
@Injectable({ providedIn: 'root' })
export class Abertura {
  private readonly noApp = Capacitor.isNativePlatform();
  readonly texto = signal<string | null>(this.noApp ? 'Abrindo' : null);

  constructor() {
    if (this.noApp) setTimeout(() => this.texto.set(null), Math.max(0, TEMPO_DA_ABERTURA_MS - performance.now()));
  }

  /** Depois do login, no app: a tela com o logo fica o tempo da abertura e então segue. */
  entrando(): Promise<void> {
    if (!this.noApp) return Promise.resolve();
    this.texto.set('Entrando');
    return new Promise((pronto) => setTimeout(() => (this.texto.set(null), pronto()), TEMPO_DA_ABERTURA_MS));
  }
}

@Component({
  selector: 'app-abertura',
  template: `
    @if (abertura.texto(); as texto) {
      <div class="abrindo" role="status" [attr.aria-label]="texto + ' o YBYRA'">
        <svg width="88" height="88" viewBox="0 0 64 64" aria-hidden="true"><g fill="none" stroke="currentColor" stroke-linecap="round">
          <circle cx="32" cy="32" r="28" stroke-width="5" />
          @for (r of aneis; track r) { <circle cx="32" cy="32" [attr.r]="r" stroke-width="1.4" /> }
          <path d="M14 14 L50 50 M50 14 L14 50" stroke-width="1.6" /><path d="M32 4 L32 60" stroke-width="1.2" /></g></svg>
        <span class="abrindo__nome">YBYRA</span>
        <span class="abrindo__barra"><span></span></span>
        <span class="abrindo__texto">{{ texto }}…</span>
      </div>
    }
  `,
  styles: `
    .abrindo { position: fixed; inset: 0; z-index: 1000; display: grid; place-content: center; justify-items: center; gap: 18px; background: #1E2A20; color: #D5C4A1; }
    .abrindo__nome { font-weight: 700; letter-spacing: .32em; font-size: 22px; padding-left: .32em; }
    .abrindo__barra { width: 120px; height: 3px; background: rgba(213, 196, 161, .2); border-radius: 3px; overflow: hidden; }
    .abrindo__barra span { display: block; width: 40%; height: 100%; background: #D5C4A1; border-radius: 3px; animation: abrindo 1.1s ease-in-out infinite; }
    .abrindo__texto { font-size: 14px; color: rgba(213, 196, 161, .8); }
    @keyframes abrindo { from { transform: translateX(-100%); } to { transform: translateX(250%); } }
    @media (prefers-reduced-motion: reduce) { .abrindo__barra span { animation: none; width: 100%; } }
  `,
})
export class AberturaTela {
  protected readonly abertura = inject(Abertura);
  protected readonly aneis = [6, 10, 14, 18, 22];
}
