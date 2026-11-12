import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, map, of } from 'rxjs';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { dataHora, km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { LoteTrocaDto, StatusLoteTroca, TrocaDto } from '../../entidades/campo';
import { StatusTroca } from '../../../compartilhado/entidades/enums';
import { TipoLaudo } from '../../../laudos/servicos/laudos';
import { PreviaLaudo } from '../../../laudos/componentes/previa-laudo';
import { Trocas } from '../../servicos/trocas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { FORMULARIO } from '../../../compartilhado/ui';
import { ROTULO_TROCA, ResumoTroca } from './resumo-troca';
import { PedidosDeTroca } from '../../../nucleo/ao-vivo/pedidos-de-troca';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';
import { LotesTroca } from '../../servicos/lotes-troca';
import { PedirLote } from './pedir-lote';
import { LoteAberto, ROTULO_LOTE, pecasDoLote } from './lote-aberto';

type Aba = 'aprovar' | 'decididas' | 'andamento' | 'recusas' | 'feitas';
/** Cada aba mostra as trocas avulsas e os lotes de troca do mesmo passo (Victor, 2026-11-11). */
const ABAS: Record<Aba, { sobre: string; titulo: string; status: StatusTroca[]; lotes: StatusLoteTroca[] }> = {
  aprovar: { sobre: 'Troca · 11', titulo: 'Para aprovar', status: ['Pedida'], lotes: ['Pedido'] },
  decididas: { sobre: 'Troca · 11', titulo: 'Decididas', status: ['Aprovada', 'AguardandoPeca', 'Reprovada'], lotes: ['Aprovado', 'AguardandoPeca', 'Reprovado'] },
  andamento: { sobre: 'Campo', titulo: 'Trocas em andamento', status: ['Pedida', 'Aprovada', 'AguardandoPeca', 'Separada'], lotes: ['Pedido', 'Aprovado', 'AguardandoPeca', 'Separado'] },
  recusas: { sobre: 'Campo', titulo: 'Recusas da via', status: ['Recusada'], lotes: ['Recusado'] },
  feitas: { sobre: 'Campo', titulo: 'Trocas feitas', status: ['Feita'], lotes: ['Feito'] },
};

/**
 * Aprovar trocas (supervisor e técnico, etapa 11; Victor, 2026-10-31: o técnico também aprova) e Trocas e laudos (técnico e supervisor): decidir, acompanhar, responder a recusa e os laudos.
 * Toda troca aberta mostra o laudo: primeiro a folha, sem baixar; "Abrir o laudo" abre a janela com Baixar PDF (Victor, 2026-10-18).
 */
@Component({
  selector: 'app-trocas-gestao',
  imports: [Esqueleto, FormsModule, FORMULARIO, Estado, ResumoTroca, PreviaLaudo, PedirLote, LoteAberto],
  templateUrl: './trocas-gestao.html',
  styles: `
    .pilula--lote { background: color-mix(in srgb, var(--areia) 22%, transparent); color: var(--tinta); border-color: var(--areia); }
    .laudo-previa { margin-top: 8px; padding-top: 10px; border-top: 1px solid var(--linha); }
    .laudo-previa header { display: flex; flex-wrap: wrap; align-items: center; gap: 4px 10px; margin-bottom: 8px; }
    .laudo-previa h3 { margin: 0; font-size: 15px; }
    .laudo-previa .ativo { font-weight: 700; text-decoration: underline; text-underline-offset: 4px; }
  `,
})
export class TrocasGestao {
  protected readonly valorDe = valorDe;
  readonly aba = input<Aba>('andamento');
  readonly aberta = input<string>();
  /** ?lote= o lote de troca aberto; ?novo=lote o técnico pedindo um lote. */
  readonly lote = input<string>();
  readonly novo = input<string>();
  private readonly api = inject(Trocas);
  private readonly lotesApi = inject(LotesTroca);
  private readonly router = inject(Router);
  private readonly sessao = inject(Sessao);

  protected readonly texto = computed(() => ABAS[this.aba()]);
  protected readonly lista = rxResource({
    params: () => ABAS[this.aba()].status,
    stream: ({ params }) => forkJoin(params.map((s) => this.api.listar(s))).pipe(map((l) => l.flat().sort((a, b) => b.dataPedido.localeCompare(a.dataPedido)))),
  });
  protected readonly lotes = rxResource({
    params: () => ABAS[this.aba()].lotes,
    stream: ({ params }) => forkJoin(params.map((s) => this.lotesApi.listar(s))).pipe(map((l) => l.flat().sort((a, b) => b.dataPedido.localeCompare(a.dataPedido)))),
  });
  protected readonly quantos = computed(() => (valorDe(this.lista)?.length ?? 0) + (valorDe(this.lotes)?.length ?? 0));
  protected readonly idLote = computed(() => (this.lote() ? Number(this.lote()) : null));
  protected readonly loteAberto = rxResource({ params: () => this.idLote() ?? undefined, stream: ({ params }) => this.lotesApi.buscar(params) });
  protected readonly pedindoLote = computed(() => this.novo() === 'lote' && this.sessao.temCargo('Tecnico'));
  protected readonly rotuloLote = ROTULO_LOTE;
  protected readonly metros = metros;
  protected readonly pecasDoLote = pecasDoLote;
  protected readonly erroLista = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly idAberta = computed(() => (this.aberta() ? Number(this.aberta()) : null));
  protected readonly troca = rxResource({ params: () => this.idAberta() ?? undefined, stream: ({ params }) => (params ? this.api.buscar(params) : of(null)) });
  /** Quem aprova: supervisor e técnico (a API confere de novo pelo parâmetro troca_quem_aprova). */
  protected readonly podeAprovar = computed(() => this.sessao.temCargo('Supervisor', 'Tecnico'));
  private readonly pedidos = inject(PedidosDeTroca);
  protected readonly ehTecnico = computed(() => this.sessao.temCargo('Tecnico'));
  protected readonly rotulo = ROTULO_TROCA;
  protected readonly dataHora = dataHora;
  protected readonly km = km;

  /** O laudo da troca aberta: começa no LD; a liberação (LB) só depois de aprovada. É a primeira visão, sem baixar. */
  protected readonly tipoLaudo = linkedSignal<number | null, TipoLaudo>({ source: this.idAberta, computation: () => 'troca' });
  protected liberacaoSai(t: TrocaDto): boolean {
    return t.status !== 'Pedida' && t.status !== 'Reprovada';
  }

  protected readonly motivo = signal('');
  protected readonly urgente = signal(false);
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

  protected abrirLote(l: LoteTrocaDto): void {
    this.mensagem.set(null);
    this.erro.set(null);
    this.router.navigate([], { queryParams: { lote: l.id } });
  }

  protected pedirLote(): void {
    this.mensagem.set(null);
    this.router.navigate([], { queryParams: { novo: 'lote' } });
  }

  /** O lote pedido já abre, com o caminho: ele vai para Aprovar trocas. */
  protected lotePedido(l: LoteTrocaDto): void {
    this.mensagem.set(`Lote ${l.codigo} pedido. Ele está em Campo › Aprovar trocas, esperando quem aprova.`);
    this.lotes.reload();
    void this.pedidos.contar();
    this.router.navigate([], { queryParams: { lote: l.id } });
  }

  protected loteMudou(mensagem: string): void {
    this.mensagem.set(mensagem);
    this.loteAberto.reload();
    this.lotes.reload();
    void this.pedidos.contar();
  }

  protected decidir(t: TrocaDto, aprovar: boolean): void {
    if (!aprovar && !this.motivo().trim()) {
      this.erro.set('Para reprovar, escreva o motivo.');
      return;
    }
    this.enviar(this.api.decidir(t.id, aprovar, this.motivo().trim() || null, aprovar && this.urgente()), aprovar ? 'Troca aprovada. O pátio já pode separar a peça.' : 'Troca reprovada.');
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
        void this.pedidos.contar();
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}