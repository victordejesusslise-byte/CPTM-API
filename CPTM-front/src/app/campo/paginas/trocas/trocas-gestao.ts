import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, map, of } from 'rxjs';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { dataHora, km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { TrocaDto } from '../../entidades/campo';
import { StatusTroca } from '../../../compartilhado/entidades/enums';
import { Laudos, TipoLaudo } from '../../../laudos/servicos/laudos';
import { FolhaLaudo } from '../../../laudos/componentes/folha-laudo';
import { LaudoAberto } from '../../../laudos/componentes/laudo-aberto';
import { Trocas } from '../../servicos/trocas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { FORMULARIO } from '../../../compartilhado/ui';
import { ROTULO_TROCA, ResumoTroca } from './resumo-troca';

type Aba = 'aprovar' | 'decididas' | 'andamento' | 'recusas' | 'feitas';
const ABAS: Record<Aba, { sobre: string; titulo: string; status: StatusTroca[] }> = {
  aprovar: { sobre: 'Troca · 11', titulo: 'Para aprovar', status: ['Pedida'] },
  decididas: { sobre: 'Troca · 11', titulo: 'Decididas', status: ['Aprovada', 'AguardandoPeca', 'Reprovada'] },
  andamento: { sobre: 'Campo', titulo: 'Trocas em andamento', status: ['Pedida', 'Aprovada', 'AguardandoPeca', 'Separada'] },
  recusas: { sobre: 'Campo', titulo: 'Recusas da via', status: ['Recusada'] },
  feitas: { sobre: 'Campo', titulo: 'Trocas feitas', status: ['Feita'] },
};

/**
 * Aprovar trocas (supervisor, etapa 11) e Trocas e laudos (técnico e supervisor): decidir, acompanhar, responder a recusa e os laudos.
 * Toda troca aberta mostra o laudo: primeiro a folha, sem baixar; "Abrir o laudo" abre a janela com Baixar PDF (Victor, 2026-10-18).
 */
@Component({
  selector: 'app-trocas-gestao',
  imports: [FormsModule, FORMULARIO, Estado, ResumoTroca, FolhaLaudo, LaudoAberto],
  templateUrl: './trocas-gestao.html',
  styles: `
    .laudo-previa { margin-top: 8px; padding-top: 10px; border-top: 1px solid var(--linha); }
    .laudo-previa header { display: flex; flex-wrap: wrap; align-items: center; gap: 4px 10px; margin-bottom: 8px; }
    .laudo-previa h3 { margin: 0; font-size: 15px; }
    .laudo-previa .ativo { font-weight: 700; text-decoration: underline; text-underline-offset: 4px; }
    .laudo-previa__folha { position: relative; max-height: 340px; overflow: hidden; padding: 12px; border: 1px solid var(--linha); border-radius: 8px;
      background: var(--superficie-3); cursor: zoom-in; user-select: none; }
    .laudo-previa__folha::after { content: ''; position: absolute; inset: auto 0 0 0; height: 90px; background: linear-gradient(transparent, var(--superficie-3)); pointer-events: none; }
    .laudo-previa__folha:focus-visible { outline: 2px solid var(--marcador); outline-offset: 2px; }
  `,
})
export class TrocasGestao {
  readonly aba = input<Aba>('andamento');
  readonly aberta = input<string>();
  private readonly api = inject(Trocas);
  private readonly laudos = inject(Laudos);
  private readonly router = inject(Router);
  private readonly sessao = inject(Sessao);

  protected readonly texto = computed(() => ABAS[this.aba()]);
  protected readonly lista = rxResource({
    params: () => ABAS[this.aba()].status,
    stream: ({ params }) => forkJoin(params.map((s) => this.api.listar(s))).pipe(map((l) => l.flat().sort((a, b) => b.dataPedido.localeCompare(a.dataPedido)))),
  });
  protected readonly erroLista = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly idAberta = computed(() => (this.aberta() ? Number(this.aberta()) : null));
  protected readonly troca = rxResource({ params: () => this.idAberta() ?? undefined, stream: ({ params }) => (params ? this.api.buscar(params) : of(null)) });
  protected readonly ehSupervisor = computed(() => this.sessao.temCargo('Supervisor'));
  protected readonly ehTecnico = computed(() => this.sessao.temCargo('Tecnico'));
  protected readonly rotulo = ROTULO_TROCA;
  protected readonly dataHora = dataHora;
  protected readonly km = km;

  /** O laudo da troca aberta: começa no LD; a liberação (LB) só depois de aprovada. É a primeira visão, sem baixar. */
  protected readonly tipoLaudo = linkedSignal<number | null, TipoLaudo>({ source: this.idAberta, computation: () => 'troca' });
  protected readonly documentoLaudo = rxResource({
    params: () => (this.idAberta() ? { tipo: this.tipoLaudo(), id: this.idAberta()! } : undefined),
    stream: ({ params }) => this.laudos.documento(params.tipo, params.id),
  });
  protected readonly erroLaudo = computed(() => (this.documentoLaudo.error() ? mensagemDeErro(this.documentoLaudo.error()) : null));
  protected liberacaoSai(t: TrocaDto): boolean {
    return t.status !== 'Pedida' && t.status !== 'Reprovada';
  }

  protected readonly motivo = signal('');
  protected readonly resposta = signal('');
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected abrir(t: TrocaDto): void {
    this.mensagem.set(null);
    this.erro.set(null);
    this.motivo.set('');
    this.resposta.set('');
    this.router.navigate([], { queryParams: { aberta: t.id } });
  }

  protected fechar(): void {
    this.router.navigate([], { queryParams: {} });
  }

  protected decidir(t: TrocaDto, aprovar: boolean): void {
    if (!aprovar && !this.motivo().trim()) {
      this.erro.set('Para reprovar, escreva o motivo.');
      return;
    }
    this.enviar(this.api.decidir(t.id, aprovar, this.motivo().trim() || null), aprovar ? 'Troca aprovada. O pátio já pode separar a peça.' : 'Troca reprovada.');
  }

  protected decidirRecusa(t: TrocaDto, decisao: 'Manter' | 'Aceitar'): void {
    this.enviar(
      this.api.decidirRecusa(t.id, decisao, this.resposta().trim() || null),
      decisao === 'Manter' ? 'Troca mantida: volta para a via fazer.' : 'Recusa aceita: a troca foi cancelada.',
    );
  }

  private enviar(acao: import('rxjs').Observable<unknown>, ok: string): void {
    this.enviando.set(true);
    this.erro.set(null);
    acao.subscribe({
      next: () => {
        this.enviando.set(false);
        this.mensagem.set(ok);
        this.troca.reload();
        this.lista.reload();
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}