import { Component, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { BuscaNasOpcoes } from '../busca-opcoes/busca-opcoes';
import { LeituraDto, METODOS_WEB, MetodoCaptura } from '../../entidades/enums';
import { detalhesDoModulo } from '../../../nucleo/leitor/leitor';
import { LEITOR } from '../../../nucleo/leitor/leitor-simulado';

/** Monta a leitura que a API pede (tag + como foi lida). Lida pelo módulo, leva também o modo, a potência e o sinal. */
export const leitura = (tag: string, metodo: MetodoCaptura): LeituraDto => ({
  tag: tag.trim(), metodo, tagsVistas: 1, ...(metodo === 'ModuloUhf' ? detalhesDoModulo(tag) : {}),
});

/**
 * O campo da tag: a pistola funciona como teclado e termina com Enter, que dispara (lida). Com o módulo, o botão "Ler com o módulo"
 * varre, escolhe o alvo único e preenche a tag (ou diz por que não leu).
 * Uso: <app-leitura-tag [(tag)]="tag" [(metodo)]="metodo" (lida)="buscar()" />
 */
@Component({
  selector: 'app-leitura-tag',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, BuscaNasOpcoes],
  template: `
    <div class="leitura" [class.leitura--compacta]="compacta()">
      <mat-form-field appearance="outline" class="leitura__tag">
        <mat-label>{{ rotulo() }}</mat-label>
        <input matInput class="mono" [ngModel]="tag()" (ngModelChange)="tag.set($event)" (keydown.enter)="$event.preventDefault(); lida.emit()"
          autocomplete="off" spellcheck="false" />
      </mat-form-field>
      @if (!compacta()) {
        <mat-form-field appearance="outline" class="leitura__metodo">
          <mat-label>Lida por</mat-label>
          <mat-select [ngModel]="metodo()" (ngModelChange)="metodo.set($event)"><app-busca-opcoes />
            @for (m of metodos; track m.valor) {
              <mat-option [value]="m.valor">{{ m.rotulo }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      }
      @if (metodo() === 'ModuloUhf') {
        <div class="leitura__modulo">
          <button mat-stroked-button type="button" (click)="lerComModulo()" [disabled]="lendo()">{{ compacta() ? 'Ler' : 'Ler com o módulo' }}</button>
          @if (aviso() && !compacta()) { <p class="aviso aviso--erro" role="alert">{{ aviso() }}</p> }
        </div>
      }
      @if (aviso() && compacta()) { <p class="aviso aviso--erro leitura__aviso" role="alert">{{ aviso() }}</p> }
    </div>
  `,
  styles: `
    .leitura { display: grid; grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); gap: 0 12px; }
    .leitura__modulo { grid-column: 1 / -1; display: flex; flex-wrap: wrap; align-items: center; gap: 8px 12px; margin: -8px 0 12px; }
    .leitura__modulo .aviso { margin: 0; }
    @media (max-width: 560px) { .leitura { grid-template-columns: 1fr; } }
    /* Compacta (o lote de troca, uma tag atrás da outra): o campo e o botão Ler na mesma linha, sem "Lida por". */
    .leitura.leitura--compacta { grid-template-columns: minmax(0, 1fr) auto; align-items: start; }
    .leitura--compacta .leitura__modulo { grid-column: auto; margin: 4px 0 0; }
    .leitura--compacta .leitura__modulo button { min-height: 52px; }
    .leitura__aviso { grid-column: 1 / -1; margin: -8px 0 12px; }
  `,
})
export class LeituraTag {
  readonly tag = model('');
  readonly metodo = model<MetodoCaptura>('ModuloUhf');
  readonly rotulo = input('Tag da peça');
  /** Só o campo e o botão Ler, sem escolher como foi lida (a leitura vai como do módulo, ou digitada se a pessoa escreveu). */
  readonly compacta = input(false);
  readonly lida = output<void>();
  protected readonly metodos = METODOS_WEB;
  private readonly leitor = inject(LEITOR);
  protected readonly lendo = signal(false);
  protected readonly aviso = signal<string | null>(null);

  protected async lerComModulo(): Promise<void> {
    this.lendo.set(true);
    const r = await this.leitor.ler();
    this.lendo.set(false);
    this.aviso.set(r.ok ? null : r.motivo);
    if (r.ok) {
      this.tag.set(r.leitura.tag);
      this.lida.emit();
    }
  }
}