import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { dataHora, diasDesde, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { novoIdCliente } from '../../../compartilhado/utilitarios/arquivo';
import { MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { ConferenciaDto, PecaDto } from '../../../pecas/entidades/peca';
import { Catalogos } from '../../../catalogos/servicos/catalogos';
import { Conferencias } from '../../servicos/conferencias';
import { Pecas } from '../../../pecas/servicos/pecas';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { Coluna, Tabela } from '../../../compartilhado/componentes/tabela/tabela';
import { Nota, classeNota } from '../../../compartilhado/componentes/nota/nota';
import { FORMULARIO } from '../../../compartilhado/ui';

/** O armazém: estoque, entrada na pilha, troca de pilha e conferência pilha por pilha. */
@Component({
  selector: 'app-armazem',
  imports: [FormsModule, FORMULARIO, LeituraTag, Tabela, Nota],
  templateUrl: './armazem.html',
})
export class Armazem {
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
  protected readonly pilhasDaPeca = computed(() => this.armazens.value()?.find((a) => a.id === this.peca()?.idArmazem)?.pilhas ?? []);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.limpar();
    this.pecas.porTag(this.tag()).subscribe({
      next: (p) => {
        this.peca.set(p);
        this.idPilha.set(p.idPilha);
      },
      error: (e) => this.erro.set(e?.status === 404 ? 'Nenhuma peça com essa tag.' : mensagemDeErro(e)),
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
  protected readonly pilhasDaConferencia = computed(() => this.armazens.value()?.find((a) => a.id === this.conferencia()?.idArmazem)?.pilhas ?? []);

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