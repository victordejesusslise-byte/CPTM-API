import { Component, computed, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { forkJoin, map, of } from 'rxjs';
import { km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { CompativelDto, LoteTrocaDto, StatusLoteTroca, TrocaDto } from '../../entidades/campo';
import { LotesTroca } from '../../servicos/lotes-troca';
import { ROTULO_LOTE, aSepararNoLote } from './lote-aberto';
import { SepararLote } from './separar-lote';
import { MetodoCaptura, StatusTroca } from '../../../compartilhado/entidades/enums';
import { Trocas } from '../../servicos/trocas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';
import { ROTULO_TROCA, ResumoTroca } from './resumo-troca';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

type Aba = 'fila' | 'separadas';
/** Os lotes de troca (Victor, 2026-11-11): para separar, os aprovados que ainda não têm todas as peças; separados, os que já têm alguma. */
const ABAS: Record<Aba, { titulo: string; status: StatusTroca[]; lotes: StatusLoteTroca[] }> = {
  fila: { titulo: 'Trocas para separar', status: ['Aprovada', 'AguardandoPeca'], lotes: ['Aprovado', 'AguardandoPeca', 'Separado'] },
  separadas: { titulo: 'Separadas, esperando a via', status: ['Separada'], lotes: ['Separado'] },
};

/** Separar peça (protótipo YBYRA, etapa 12): o pátio separa uma peça compatível; o sistema sugere a da frente da pilha. */
@Component({
  selector: 'app-separar-peca',
  imports: [Esqueleto, FORMULARIO, Estado, LeituraTag, ResumoTroca, SepararLote],
  templateUrl: './separar-peca.html',
})
export class SepararPeca {
  protected readonly valorDe = valorDe;
  readonly aba = input<Aba>('fila');
  readonly aberta = input<string>();
  /** ?lote= o lote de troca aberto. */
  readonly lote = input<string>();
  private readonly api = inject(Trocas);
  private readonly lotesApi = inject(LotesTroca);
  private readonly router = inject(Router);

  protected readonly lotes = rxResource({
    params: () => ABAS[this.aba()].lotes,
    stream: ({ params }) => forkJoin(params.map((s) => this.lotesApi.listar(s))).pipe(map((l) => l.flat())),
  });
  protected readonly lotesVisiveis = computed(() => (valorDe(this.lotes) ?? []).filter((l) =>
    this.aba() === 'fila' ? l.separadas.length < aSepararNoLote(l) : l.separadas.length > 0));
  protected readonly quantos = computed(() => (valorDe(this.lista)?.length ?? 0) + this.lotesVisiveis().length);
  protected readonly idLote = computed(() => (this.lote() ? Number(this.lote()) : null));
  protected readonly loteAberto = rxResource({ params: () => this.idLote() ?? undefined, stream: ({ params }) => this.lotesApi.buscar(params) });
  protected readonly rotuloLote = ROTULO_LOTE;

  protected abrirLote(l: LoteTrocaDto): void {
    this.mensagem.set(null);
    this.erro.set(null);
    this.router.navigate([], { queryParams: { lote: l.id } });
  }

  protected loteMudou(mensagem: string): void {
    this.mensagem.set(mensagem);
    this.loteAberto.reload();
    this.lotes.reload();
  }

  protected readonly titulo = computed(() => ABAS[this.aba()].titulo);
  protected readonly lista = rxResource({
    params: () => ABAS[this.aba()].status,
    stream: ({ params }) => forkJoin(params.map((s) => this.api.listar(s))).pipe(map((l) => l.flat())),
  });
  protected readonly erroLista = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly idAberta = computed(() => (this.aberta() ? Number(this.aberta()) : null));
  protected readonly troca = rxResource({ params: () => this.idAberta() ?? undefined, stream: ({ params }) => (params ? this.api.buscar(params) : of(null)) });
  protected readonly compativeis = rxResource({
    params: () => (this.aba() === 'fila' ? (this.idAberta() ?? undefined) : undefined),
    stream: ({ params }) => this.api.compativeis(params),
  });
  protected readonly rotulo = ROTULO_TROCA;
  protected readonly km = km;
  protected readonly metros = metros;
  protected readonly aSeparar = aSepararNoLote;

  protected readonly escolhida = signal<CompativelDto | null>(null);
  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected abrir(t: TrocaDto): void {
    this.escolhida.set(null);
    this.mensagem.set(null);
    this.erro.set(null);
    this.router.navigate([], { queryParams: { aberta: t.id } });
  }

  protected fechar(): void {
    this.router.navigate([], { queryParams: {} });
  }

  protected separar(t: TrocaDto): void {
    const c = this.escolhida();
    if (!c || !this.tag().trim()) {
      this.erro.set('Escolha a peça e leia a tag dela no armazém.');
      return;
    }
    if (c.peca.tag && c.peca.tag !== this.tag().trim()) {
      this.erro.set(`A tag lida não é a da peça ${c.peca.codigo}. Confira se pegou a peça certa.`);
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    this.api.separar(t.id, c.peca.id, leitura(this.tag(), this.metodo())).subscribe({
      next: () => {
        this.enviando.set(false);
        this.mensagem.set(`Peça ${c.peca.codigo} separada. A via já pode fazer a troca.`);
        this.escolhida.set(null);
        this.tag.set('');
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