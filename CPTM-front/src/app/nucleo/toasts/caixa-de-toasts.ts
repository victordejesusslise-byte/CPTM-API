import { Component, DestroyRef, afterNextRender, inject } from '@angular/core';
import { Toasts } from './toasts';

/** As mensagens que as telas mostram depois de uma ação: o erro (role alert) e a confirmação (role status). Os avisos fixos das listas ficam fora. */
const SELETOR = '.aviso--erro[role="alert"], .aviso--ok[role="status"]';

/**
 * Os toasts no canto da tela (Victor, 2026-10-31: "coloque os toasts de erro nas telas", "e de aprovação também"). Toda mensagem de erro
 * ou de confirmação que uma tela mostra vira também um toast, sem cada tela precisar chamar nada: a caixa percebe quando a mensagem
 * aparece ou muda. Fica na raiz do app, então vale também no login.
 */
@Component({
  selector: 'app-caixa-de-toasts',
  template: `
    <div class="toasts" aria-live="polite">
      @for (t of toasts.lista(); track t.id) {
        <div class="toast" [class.toast--erro]="t.tipo === 'erro'" [class.toast--ok]="t.tipo === 'ok'">
          <span class="toast__tipo">{{ t.tipo === 'erro' ? 'Não deu certo' : 'Feito' }}</span>
          <span class="toast__texto">{{ t.texto }}</span>
          <button type="button" (click)="toasts.fechar(t.id)" aria-label="Fechar o aviso">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    /* embaixo à direita (Victor, 2026-10-31), logo acima do indicador do leitor de tag, que fica no canto */
    .toasts { position: fixed; right: 16px; bottom: calc(64px + var(--fundo-seguro, 0px)); z-index: 2000; display: flex; flex-direction: column; gap: 10px; width: min(420px, calc(100vw - 32px)); pointer-events: none; }
    .toast { pointer-events: auto; display: grid; grid-template-columns: 1fr auto; gap: 2px 12px; padding: 12px 14px 12px 16px; border-radius: 8px;
      background: var(--bloco); color: var(--sobre-escuro); border-left: 4px solid var(--areia); box-shadow: 0 10px 28px rgba(0, 0, 0, .35); animation: entra .25s ease-out; }
    .toast--erro { background: var(--vinho); border-left-color: var(--sobre-escuro); }
    .toast__tipo { grid-column: 1; font-size: 12px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; opacity: .85; }
    .toast__texto { grid-column: 1; font-size: 15px; line-height: 1.4; }
    .toast button { grid-column: 2; grid-row: 1 / span 2; align-self: start; min-width: 36px; min-height: 36px; border: 0; border-radius: 6px; background: transparent;
      color: inherit; font-size: 22px; line-height: 1; cursor: pointer; }
    .toast button:hover { background: rgba(255, 255, 255, .12); }
    @keyframes entra { from { opacity: 0; transform: translateY(10px); } }
    @media (max-width: 600px) { .toasts { left: 16px; width: auto; } }
  `,
})
export class CaixaDeToasts {
  protected readonly toasts = inject(Toasts);

  constructor() {
    const destruir = inject(DestroyRef);
    afterNextRender(() => {
      const avisar = (el: Element | null | undefined) => {
        if (el) this.toasts.mostrar(el.textContent ?? '', el.classList.contains('aviso--erro') ? 'erro' : 'ok');
      };
      const observador = new MutationObserver((mudancas) => {
        for (const m of mudancas) {
          if (m.type === 'characterData') avisar(m.target.parentElement?.closest(SELETOR));
          for (const no of m.addedNodes) {
            if (no instanceof Element) {
              if (no.matches(SELETOR)) avisar(no);
              no.querySelectorAll(SELETOR).forEach(avisar);
            } else avisar(no.parentElement?.closest(SELETOR));
          }
        }
      });
      observador.observe(document.body, { childList: true, subtree: true, characterData: true });
      destruir.onDestroy(() => observador.disconnect());
    });
  }
}
