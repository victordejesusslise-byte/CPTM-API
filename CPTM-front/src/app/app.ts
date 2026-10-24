import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Tema } from './nucleo/layout/tema/tema';
import { AberturaTela } from './nucleo/layout/abertura/abertura';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AberturaTela],
  template: '<router-outlet /><app-abertura />',
})
export class App {
  /** aplica o tema guardado já na abertura, inclusive no login */
  protected readonly tema = inject(Tema);
}