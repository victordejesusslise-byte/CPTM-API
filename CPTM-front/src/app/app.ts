import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationCancellationCode, NavigationEnd, NavigationError, NavigationSkipped, NavigationStart, Router, RouterOutlet } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';
import { Tema } from './nucleo/layout/tema/tema';
import { Abertura, AberturaTela } from './nucleo/layout/abertura/abertura';
import { EsqueletoInicio, TEMPO_DO_ESQUELETO_DO_LOGIN_MS } from './nucleo/layout/inicio/esqueleto-inicio';
import { NO_APP } from './nucleo/ao-vivo/trabalho-da-noite';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AberturaTela, EsqueletoInicio],
  template: `
    @if (abrindo(); as tipo) { <app-esqueleto-inicio [tipo]="tipo" /> }
    <router-outlet /><app-abertura />
  `,
})
export class App {
  /** aplica o tema guardado já na abertura, inclusive no login */
  protected readonly tema = inject(Tema);
  /**
   * O skeleton enquanto a tela não chega: na primeira tela (ao abrir o sistema ou o app) e ao ir para o login (ao sair). Com o login,
   * a forma do login; senão, a de uma tela. Dentro do sistema, quem mostra o skeleton ao trocar de tela é a casca.
   */
  protected readonly abrindo = signal<'login' | 'pagina' | null>('pagina');
  private readonly noApp = inject(NO_APP);
  private readonly abertura = inject(Abertura);
  private readonly aberturaSaiu = toObservable(this.abertura.texto).pipe(filter((t) => t === null));
  private primeira = true;
  private loginPedidoEm = 0;

  constructor() {
    inject(Router)
      .events.pipe(takeUntilDestroyed())
      .subscribe((e) => {
        if (e instanceof NavigationStart && (this.primeira || e.url.startsWith('/login'))) {
          const login = e.url.startsWith('/login');
          this.abrindo.set(login ? 'login' : 'pagina');
          if (login) this.loginPedidoEm = Date.now();
        } else if (e instanceof NavigationEnd || e instanceof NavigationCancel || e instanceof NavigationError || e instanceof NavigationSkipped) {
          // O guarda que manda para o login cancela a primeira navegação e começa outra: o skeleton continua até ela chegar.
          if (e instanceof NavigationCancel && e.code === NavigationCancellationCode.Redirect && this.primeira) return;
          this.primeira = false;
          if (this.abrindo() === 'login' && this.noApp) void this.esconderOLoginDepois();
          else this.abrindo.set(null);
        }
      });
  }

  /**
   * No app (Victor, 2026-10-24: 3 segundos com o skeleton): o skeleton do login fica alguns segundos na tela, contados de quando ele
   * aparece de verdade. Ao abrir o app, a tela do logo fica por cima dele; o tempo só começa quando ela sai.
   */
  private async esconderOLoginDepois(): Promise<void> {
    let desde = this.loginPedidoEm;
    if (this.abertura.texto() !== null) {
      await firstValueFrom(this.aberturaSaiu);
      desde = Date.now();
    }
    await new Promise((pronto) => setTimeout(pronto, Math.max(0, desde + TEMPO_DO_ESQUELETO_DO_LOGIN_MS - Date.now())));
    if (this.abrindo() === 'login') this.abrindo.set(null);
  }
}
