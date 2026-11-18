import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { dataHora, diasDesde, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { novoIdCliente } from '../../../compartilhado/utilitarios/arquivo';
import { MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { ConferenciaDto, GrupoDoLoteDto, LoteParaGuardarDto, PecaDto } from '../../../pecas/entidades/peca';
import { PilhaDto } from '../../../catalogos/entidades/catalogo';
import { Catalogos } from '../../../catalogos/servicos/catalogos';
import { Conferencias } from '../../servicos/conferencias';
import { Pecas } from '../../../pecas/servicos/pecas';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { Coluna, Tabela } from '../../../compartilhado/componentes/tabela/tabela';
import { Nota, classeNota } from '../../../compartilhado/componentes/nota/nota';
import { FORMULARIO } from '../../../compartilhado/ui';
import { CartoesNoCelular } from '../../../compartilhado/componentes/tabela/cartoes';
import { LegivelPipe } from '../../../compartilhado/utilitarios/legivel';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

/** O armazém: estoque, entrada na pilha, troca de pilha e conferência pilha por pilha. */
@Component({
  selector: 'app-armazem',
  imports: [LegivelPipe, CartoesNoCelular, Esqueleto, FormsModule, FORMULARIO, LeituraTag, Tabela, Nota],
  templateUrl: './armazem.html',
})
export class Armazem {
  protected readonly valorDe = valorDe;
  /** Buscando a peça pela tag: o cartão dela mostra o skeleton. */
  protected readonly buscando = signal(false);
  /** a aba vem do data da rota: Guardar na pilha, Pilhas dos armazéns, Estoque, Conferência */
  readonly aba = input<'guardar' | 'pilhas' | 'estoque' | 'conferencia'>('guardar');
  protected readonly titulos = { guardar: 'Guardar na pilha', pilhas: 'Pilhas dos armazéns', estoque: 'Estoque', conferencia: 'Conferência' };
  private readonly pecas = inject(Pecas);
  private readonly conferencias = inject(Conferencias);
  private readonly catalogos = inject(Catalogos);
  protected readonly metros = metros;
  protected readonly dataHora = dataHora;

  protected readonly armazens = rxResource({ stream: () => this.catalogos.armazens() });
  protected readonly tamanhos = toSignal(this.catalogos.tamanhos(), { initialValue: [] });

  // Estoque
  protected readonly filtro = signal<{ idArmazem: number | null; idTamanho: number | null; classe: number | null }>({ idArmazem: null, idTamanho: null, classe: null });
  protected readonly estoque = rxResource({ params: () => this.filtro(), stream: ({ params }) => this.pecas.estoque(params) });
  protected readonly colunasEstoque: Coluna<PecaDto>[] = [
    { titulo: 'Peça', valor: (p) => p.codigo, mono: true },
    { titulo: 'Tag', valor: (p) => p.tag, mono: true },
    { titulo: 'Espécie', valor: (p) => p.especie },
    { titulo: 'Tamanho', valor: (p) => metros(p.comprimentoM) },
    { titulo: 'Classe', valor: (p) => `${p.classe}ª` },
    { titulo: 'Armazém', valor: (p) => p.armazem },
    { titulo: 'Pilha', valor: (p) => p.pilha ?? 'sem pilha', vermelho: (p) => !p.pilha },
    { titulo: 'Nota', valor: (p) => p.nota ?? 'sem nota', pilula: (p) => classeNota(p.nota) },
    { titulo: 'No armazém', valor: (p) => (p.entradaArmazemEm ? `${diasDesde(p.entradaArmazemEm)} dias` : '—') },
  ];

  protected mudarFiltro(campo: 'idArmazem' | 'idTamanho' | 'classe', valor: number | null): void {
    this.filtro.update((f) => ({ ...f, [campo]: valor }));
  }

  // Entrada e troca de pilha
  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly peca = signal<PecaDto | null>(null);
  protected readonly idPilha = signal<number | null>(null);
  protected readonly pilhasDaPeca = computed(() => valorDe(this.armazens)?.find((a) => a.id === this.peca()?.idArmazem)?.pilhas ?? []);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.limpar();
    this.buscando.set(true);
    this.pecas.porTag(this.tag()).pipe(finalize(() => this.buscando.set(false))).subscribe({
      next: (p) => {
        this.peca.set(p);
        this.idPilha.set(p.idPilha);
        this.buscarLote();
      },
      error: (e) => this.erro.set(e?.status === 404 ? 'Nenhuma peça com essa tag.' : mensagemDeErro(e)),
    });
  }

  // O lote inteiro (Victor, 2026-11-17): a tag lida acha o lote, separado por tamanho e classe; cada grupo vai para a pilha dele.
  protected readonly lote = signal<LoteParaGuardarDto | null>(null);
  /** O armazém do lote todo: começa no da chegada da peça lida e pode trocar. */
  protected readonly armazemDoLote = linkedSignal(() => this.lote()?.idArmazem ?? null);
  /** A pilha escolhida à mão para cada grupo (chave tamanho-classe); trocar de armazém ou de lote volta para as sugeridas. */
  private readonly pilhasEscolhidas = linkedSignal<{ lote: LoteParaGuardarDto | null; armazem: number | null }, Record<string, number>>({
    source: () => ({ lote: this.lote(), armazem: this.armazemDoLote() }),
    computation: () => ({}),
  });
  protected readonly chave = (g: GrupoDoLoteDto) => `${g.idTamanho}-${g.classe}`;
  /** As pilhas do armazém escolhido que servem para o grupo: mesmo comprimento e mesma classe. */
  protected pilhasDoGrupo(g: GrupoDoLoteDto): PilhaDto[] {
    const armazem = valorDe(this.armazens)?.find((a) => a.id === this.armazemDoLote());
    return armazem?.pilhas.filter((p) => p.comprimentoM === g.comprimentoM && p.classe === g.classe) ?? [];
  }
  /** A pilha do grupo: a escolhida à mão ou, sem ela, a primeira que serve (a mesma que a API sugere). */
  protected pilhaDoGrupo(g: GrupoDoLoteDto): number | null {
    return this.pilhasEscolhidas()[this.chave(g)] ?? this.pilhasDoGrupo(g)[0]?.id ?? null;
  }
  protected escolherPilha(g: GrupoDoLoteDto, idPilha: number): void {
    this.pilhasEscolhidas.update((e) => ({ ...e, [this.chave(g)]: idPilha }));
  }
  /** Quantas peças do lote esperam pilha, e quantas delas o botão guarda (as dos grupos que têm pilha neste armazém). */
  protected readonly aGuardarNoLote = computed(() => (this.lote()?.grupos ?? []).reduce((s, g) => s + g.aGuardar, 0));
  protected readonly aGuardarComPilha = computed(() => (this.lote()?.grupos ?? []).filter((g) => g.aGuardar && this.pilhaDoGrupo(g)).reduce((s, g) => s + g.aGuardar, 0));
  protected readonly totais = computed(() => (this.lote()?.grupos ?? []).reduce((s, g) => ({ jaNaPilha: s.jaNaPilha + g.jaNaPilha, naoAprovadas: s.naoAprovadas + g.naoAprovadas }), { jaNaPilha: 0, naoAprovadas: 0 }));

  /** Busca o lote da peça lida; sem lote (ou sem permissão), a tela fica só com a peça, como antes. */
  private buscarLote(): void {
    this.pecas.loteDaTag(this.tag()).subscribe({ next: (l) => this.lote.set(l), error: () => this.lote.set(null) });
  }

  protected guardarLote(): void {
    const l = this.lote();
    const idArmazem = this.armazemDoLote();
    if (!l || !idArmazem) return;
    // Só os grupos com peça esperando e com pilha neste armazém vão no pedido.
    const grupos = l.grupos.filter((g) => g.aGuardar && this.pilhaDoGrupo(g))
      .map((g) => ({ idTamanho: g.idTamanho, classe: g.classe, idPilha: this.pilhaDoGrupo(g)! }));
    const quantas = this.aGuardarComPilha();
    this.executar(this.pecas.guardarLote({ idArmazem, leitura: leitura(this.tag(), this.metodo()), grupos }), (r) => {
      this.lote.set(r);
      this.peca.set(null);
      return `${quantas} peças do lote ${r.codigoLote} guardadas nas pilhas.`;
    });
  }

  protected guardar(): void {
    const p = this.peca();
    if (!p) return;
    this.executar(
      p.entradaArmazemEm && p.idPilha
        ? this.pecas.trocarPilha(p.id, { idPilha: this.idPilha() ?? 0, leitura: leitura(this.tag(), this.metodo()) })
        : this.pecas.guardar(p.id, { leitura: leitura(this.tag(), this.metodo()), idPilha: this.idPilha(), idCliente: novoIdCliente() }),
      (r) => `Peça ${r.codigo} na pilha ${r.pilha ?? '—'}.`,
    );
  }

  // Conferência
  protected readonly idArmazemConferencia = signal<number | null>(null);
  protected readonly conferencia = signal<ConferenciaDto | null>(null);
  protected readonly pilhaConferida = signal<number | null>(null);
  protected readonly tagsLidas = signal('');
  protected readonly pilhasDaConferencia = computed(() => valorDe(this.armazens)?.find((a) => a.id === this.conferencia()?.idArmazem)?.pilhas ?? []);

  protected iniciarConferencia(): void {
    const id = this.idArmazemConferencia();
    if (!id) return;
    this.executar(this.conferencias.iniciar(id), (c) => {
      this.conferencia.set(c);
      return `Conferência aberta: ${c.esperadas} peças esperadas.`;
    });
  }

  protected lerPilha(): void {
    const c = this.conferencia();
    const idPilha = this.pilhaConferida();
    if (!c || !idPilha) return;
    const tags = this.tagsLidas().split(/[\s,;]+/).map((t) => t.trim()).filter(Boolean);
    this.executar(this.conferencias.lerPilha(c.id, idPilha, tags), (r) => {
      this.conferencia.set(r);
      this.tagsLidas.set('');
      return `Pilha lida: ${tags.length} tags. ${r.lidas} de ${r.esperadas} até agora.`;
    });
  }

  protected concluir(): void {
    const c = this.conferencia();
    if (!c) return;
    this.executar(this.conferencias.concluir(c.id), (r) => {
      this.conferencia.set(r);
      return 'Conferência concluída.';
    });
  }

  private limpar(): void {
    this.peca.set(null);
    this.lote.set(null);
    this.mensagem.set(null);
    this.erro.set(null);
  }

  private executar<T>(acao: import('rxjs').Observable<T>, depois: (r: T) => string): void {
    this.enviando.set(true);
    this.erro.set(null);
    this.mensagem.set(null);
    acao.subscribe({
      next: (r) => {
        this.mensagem.set(depois(r));
        this.enviando.set(false);
        this.estoque.reload();
        this.armazens.reload();
      },
      error: (e) => {
        this.erro.set(mensagemDeErro(e));
        this.enviando.set(false);
      },
    });
  }
}