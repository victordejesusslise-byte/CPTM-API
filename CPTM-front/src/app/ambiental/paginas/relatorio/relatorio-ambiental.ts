import { Component, computed, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { abrirArquivo } from '../../../compartilhado/utilitarios/arquivo';
import { dataHora, m3, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { RelatorioDto, Relatorios } from '../../servicos/relatorios';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { FORMULARIO } from '../../../compartilhado/ui';
import { CartoesNoCelular } from '../../../compartilhado/componentes/tabela/cartoes';
import { LegivelPipe } from '../../../compartilhado/utilitarios/legivel';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

const mesAtual = () => new Date().toISOString().slice(0, 7);
const nomeDoMes = (iso: string) => new Date(`${iso.slice(0, 7)}-15T12:00:00`).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });

/**
 * Relatório ambiental (protótipo YBYRA; especialista e supervisor): o relatório do mês, gerado como rascunho,
 * a planilha do IBAMA e o envio, que congela; e a aba por espécie.
 */
@Component({
  selector: 'app-relatorio-ambiental',
  imports: [LegivelPipe, CartoesNoCelular, Esqueleto, FormsModule, FORMULARIO, Estado],
  templateUrl: './relatorio-ambiental.html',
})
export class RelatorioAmbiental {
  protected readonly valorDe = valorDe;
  readonly aba = input<'mes' | 'especies'>('mes');
  private readonly api = inject(Relatorios);

  protected readonly relatorios = rxResource({ stream: () => this.api.listar() });
  protected readonly erroLista = computed(() => (this.relatorios.error() ? mensagemDeErro(this.relatorios.error()) : null));
  protected readonly mes = signal(mesAtual());
  protected readonly escolhido = signal<number | null>(null);
  protected readonly relatorio = computed<RelatorioDto | null>(() => {
    const lista = valorDe(this.relatorios) ?? [];
    return lista.find((r) => r.id === this.escolhido()) ?? lista.find((r) => r.mesReferencia.slice(0, 7) === this.mes()) ?? null;
  });
  protected readonly maiorEstoque = computed(() => Math.max(1, ...(this.relatorio()?.itens ?? []).map((i) => i.volumeM3Estoque)));
  protected readonly confirmarEnvio = signal(false);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);
  protected readonly m3 = m3;
  protected readonly dataHora = dataHora;
  protected readonly nomeDoMes = nomeDoMes;

  protected gerar(): void {
    this.executar(this.api.gerar(`${this.mes()}-01`), (r) => {
      this.escolhido.set(r.id);
      return `Rascunho de ${nomeDoMes(r.mesReferencia)} gerado. Confira e baixe a planilha antes de enviar.`;
    });
  }

  protected enviar(r: RelatorioDto): void {
    this.confirmarEnvio.set(false);
    this.executar(this.api.enviar(r.id), () => `Relatório de ${nomeDoMes(r.mesReferencia)} enviado. Agora ele fica congelado.`);
  }

  protected baixar(r: RelatorioDto): void {
    this.api.planilha(r.id).subscribe({
      next: (arquivo) => abrirArquivo(arquivo, `ibama-${r.mesReferencia.slice(0, 7)}.xlsx`),
      error: (e) => this.erro.set(mensagemDeErro(e)),
    });
  }

  protected abrir(r: RelatorioDto): void {
    this.escolhido.set(r.id);
    this.mes.set(r.mesReferencia.slice(0, 7));
  }

  private executar(acao: import('rxjs').Observable<RelatorioDto>, ok: (r: RelatorioDto) => string): void {
    this.enviando.set(true);
    this.erro.set(null);
    this.mensagem.set(null);
    acao.subscribe({
      next: (r) => {
        this.enviando.set(false);
        this.mensagem.set(ok(r));
        this.relatorios.reload();
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}