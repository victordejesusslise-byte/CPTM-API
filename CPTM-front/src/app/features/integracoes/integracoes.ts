import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { abrirArquivo } from '../../helpers/arquivo';
import { dataHora, mensagemDeErro } from '../../helpers/formato';
import { ImportacaoNotasDto, Integracoes as IntegracoesApi } from '../../services/integracoes';
import { FORMULARIO } from '../../shared/ui';

type Aba = 'notas' | 'erp' | 'historico';
const TITULOS: Record<Aba, string> = { notas: 'Notas pela planilha', erp: 'Estoque para o ERP', historico: 'Histórico das integrações' };

/** Integrações (supervisor e operador de pátio): importar as notas pela planilha do fornecedor, a planilha do ERP e o histórico. */
@Component({
  selector: 'app-integracoes',
  imports: [FORMULARIO],
  templateUrl: './integracoes.html',
})
export class Integracoes {
  readonly aba = input<Aba>('notas');
  private readonly api = inject(IntegracoesApi);
  protected readonly titulo = computed(() => TITULOS[this.aba()]);
  protected readonly historico = rxResource({ params: () => (this.aba() === 'historico' ? true : undefined), stream: () => this.api.log() });
  protected readonly dataHora = dataHora;
  protected readonly arquivo = signal<File | null>(null);
  protected readonly resultado = signal<ImportacaoNotasDto | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected escolher(e: Event): void {
    this.arquivo.set((e.target as HTMLInputElement).files?.[0] ?? null);
    this.resultado.set(null);
  }

  protected importar(): void {
    const f = this.arquivo();
    if (!f) return this.erro.set('Escolha a planilha (.xlsx).');
    this.enviando.set(true);
    this.erro.set(null);
    this.api.importarNotas(f).subscribe({
      next: (r) => {
        this.enviando.set(false);
        this.resultado.set(r);
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }

  protected baixarModelo(): void {
    this.api.modeloNotas().subscribe({ next: (b) => abrirArquivo(b, 'modelo-notas.xlsx'), error: (e) => this.erro.set(mensagemDeErro(e)) });
  }

  protected baixarErp(): void {
    this.api.estoqueErp().subscribe({ next: (b) => abrirArquivo(b, `estoque-erp-${new Date().toISOString().slice(0, 10)}.xlsx`), error: (e) => this.erro.set(mensagemDeErro(e)) });
  }
}