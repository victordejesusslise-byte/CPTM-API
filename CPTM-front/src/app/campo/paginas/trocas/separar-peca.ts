import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { forkJoin, map, of } from 'rxjs';
import { km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { CompativelDto, TrocaDto } from '../../entidades/campo';
import { MetodoCaptura, StatusTroca } from '../../../compartilhado/entidades/enums';
import { Trocas } from '../../servicos/trocas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';
import { ROTULO_TROCA, ResumoTroca } from './resumo-troca';

type Aba = 'fila' | 'separadas';
const ABAS: Record<Aba, { titulo: string; status: StatusTroca[] }> = {
  fila: { titulo: 'Trocas para separar', status: ['Aprovada', 'AguardandoPeca'] },
  separadas: { titulo: 'Separadas, esperando a via', status: ['Separada'] },
};

/** Separar peça (protótipo YBYRA, etapa 12): o pátio separa uma peça compatível; o sistema sugere a da frente da pilha. */
@Component({
  selector: 'app-separar-peca',
  imports: [FORMULARIO, Estado, LeituraTag, ResumoTroca],
  templateUrl: './separar-peca.html',
})
export class SepararPeca {
  readonly aba = input<Aba>('fila');
  readonly aberta = input<string>();
  private readonly api = inject(Trocas);
  private readonly router = inject(Router);

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