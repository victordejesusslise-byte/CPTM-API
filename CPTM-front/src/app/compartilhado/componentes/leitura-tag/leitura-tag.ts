import { Component, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { LeituraDto, METODOS_WEB, MetodoCaptura } from '../../entidades/enums';

/** Monta a leitura que a API pede (tag + como foi lida). */
export const leitura = (tag: string, metodo: MetodoCaptura): LeituraDto => ({ tag: tag.trim(), metodo, tagsVistas: 1 });

/**
 * O campo da tag: a pistola funciona como teclado e termina com Enter, que dispara (lida).
 * Uso: <app-leitura-tag [(tag)]="tag" [(metodo)]="metodo" (lida)="buscar()" />
 */
@Component({
  selector: 'app-leitura-tag',
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <div class="leitura">
      <mat-form-field appearance="outline" class="leitura__tag">
        <mat-label>{{ rotulo() }}</mat-label>
        <input matInput class="mono" [ngModel]="tag()" (ngModelChange)="tag.set($event)" (keydown.enter)="$event.preventDefault(); lida.emit()"
          autocomplete="off" spellcheck="false" />
      </mat-form-field>
      <mat-form-field appearance="outline" class="leitura__metodo">
        <mat-label>Lida por</mat-label>
        <mat-select [ngModel]="metodo()" (ngModelChange)="metodo.set($event)">
          @for (m of metodos; track m.valor) {
            <mat-option [value]="m.valor">{{ m.rotulo }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>
  `,
  styles: `
    .leitura { display: grid; grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); gap: 0 12px; }
    @media (max-width: 560px) { .leitura { grid-template-columns: 1fr; } }
  `,
})
export class LeituraTag {
  readonly tag = model('');
  readonly metodo = model<MetodoCaptura>('ModuloUhf');
  readonly rotulo = input('Tag da peça');
  readonly lida = output<void>();
  protected readonly metodos = METODOS_WEB;
}