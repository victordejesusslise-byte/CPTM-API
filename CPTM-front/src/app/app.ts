import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationCancellationCode, NavigationEnd, NavigationError, NavigationSkipped, NavigationStart, Router, RouterOutlet } from '@angular/router';
import { Tema } from './nucleo/layout/tema/tema';
import { AberturaTela } from './nucleo/layout/abertura/abertura';
import { EsqueletoInicio } from './nucleo/layout/inicio/esqueleto-inicio';

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
  private primeira = true;

  constructor() {
    inject(Router)
      .events.pipe(takeUntilDestroyed())
      .subscribe((e) => {
        if (e instanceof NavigationStart && (this.primeira || e.url.startsWith('/login')))
          this.abrindo.set(e.url.startsWith('/login') ? 'login' : 'pagina');
        else if (e instanceof NavigationEnd || e instanceof NavigationCancel || e instanceof NavigationError || e instanceof NavigationSkipped) {
          // O guarda que manda para o login cancela a primeira navegação e começa outra: o skeleton continua até ela chegar.
          if (e instanceof NavigationCancel && e.code === NavigationCancellationCode.Redirect && this.primeira) return;
          this.primeira = false;
          this.abrindo.set(null);
        }
      });
  }
}
