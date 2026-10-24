import { Component, computed, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { abrirArquivo } from '../../../compartilhado/utilitarios/arquivo';
import { dataHora, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { metros } from '../../../compartilhado/utilitarios/formato';
import { Catalogos } from '../../../catalogos/servicos/catalogos';
import { ImportacaoNotasDto, Integracoes as IntegracoesApi } from '../../servicos/integracoes';
import { FORMULARIO } from '../../../compartilhado/ui';
import { CartoesNoCelular } from '../../../compartilhado/componentes/tabela/cartoes';
import { LegivelPipe } from '../../../compartilhado/utilitarios/legivel';

type Aba = 'notas' | 'erp' | 'historico';
const TITULOS: Record<Aba, string> = { notas: 'Notas pela planilha', erp: 'Estoque para o ERP', historico: 'Histórico das integrações' };

/** Integrações (supervisor e operador de pátio): importar as notas pela planilha do fornecedor, a planilha do ERP e o histórico. */
@Component({
  selector: 'app-integracoes',
  imports: [LegivelPipe, CartoesNoCelular, Esqueleto, FormsModule, FORMULARIO],
  templateUrl: './integracoes.html',
})
export class Integracoes {
  readonly aba = input<Aba>('notas');
  private readonly api = inject(IntegracoesApi);
  protected readonly titulo = computed(() => TITULOS[this.aba()]);
  protected readonly historico = rxResource({ params: () => (this.aba() === 'historico' ? true : undefined), stream: () => this.api.log() });
  protected readonly dataHora = dataHora;
  protected readonly metros = metros;
  protected readonly codigos = rxResource({ params: () => (this.aba() === 'erp' ? true : undefined), stream: () => this.api.codigosMaterial() });
  protected readonly tamanhos = toSignal(inject(Catalogos).tamanhos(), { initialValue: [] });
  protected readonly ehSupervisor = inject(Sessao).temCargo('Supervisor');
  protected readonly novo = { idTamanho: 0, classe: 1, codigoErp: '' };
  protected readonly feito = signal<string | null>(null);
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

  protected salvarCodigo(): void {
    const n = this.novo;
    if (!n.idTamanho || !n.codigoErp.trim()) return this.erro.set('Escolha o tamanho e informe o código do ERP.');
    this.api.novoCodigoMaterial(n.idTamanho, n.classe, n.codigoErp.trim()).subscribe({
      next: () => {
        n.codigoErp = '';
        this.feito.set('Código de material cadastrado.');
        this.codigos.reload();
      },
      error: (e) => this.erro.set(mensagemDeErro(e)),
    });
  }

  protected baixarModelo(): void {
    this.api.modeloNotas().subscribe({ next: (b) => abrirArquivo(b, 'modelo-notas.xlsx'), error: (e) => this.erro.set(mensagemDeErro(e)) });
  }

  protected baixarErp(): void {
    this.api.estoqueErp().subscribe({ next: (b) => abrirArquivo(b, `estoque-erp-${new Date().toISOString().slice(0, 10)}.xlsx`), error: (e) => this.erro.set(mensagemDeErro(e)) });
  }
}