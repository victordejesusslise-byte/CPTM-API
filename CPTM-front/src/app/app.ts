import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Tema } from './nucleo/layout/tema/tema';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {
  /** aplica o tema guardado já na abertura, inclusive no login */
  protected readonly tema = inject(Tema);
}