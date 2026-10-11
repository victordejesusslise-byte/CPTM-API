import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { mensagemDeErro, metros } from '../../helpers/formato';
import { novoIdCliente } from '../../helpers/arquivo';
import { MetodoCaptura } from '../../models/enums';
import { PecaDto } from '../../models/peca';
import { FeixeDto } from '../../models/pedido';
import { Pecas } from '../../services/pecas';
import { Pedidos } from '../../services/pedidos';
import { LeituraTag } from '../../shared/leitura-tag/leitura-tag';
import { Coluna, Tabela } from '../../shared/tabela/tabela';
import { FORMULARIO } from '../../shared/ui';

/**
 * O cadastro da peça: escolhe o feixe, pega a próxima tag, grava no módulo e relê.
 * A API devolve 201 com a peça ou 409 quando a gravação não vale (tag relida diferente, mais de uma tag no campo).
 */
@Component({
  selector: 'app-cadastro',
  imports: [FormsModule, FORMULARIO, LeituraTag, Tabela],
  templateUrl: './cadastro.html',
})
export class Cadastro {
  readonly pedido = input<string>();
  readonly chegada = input<string>();
  private readonly pedidos = inject(Pedidos);
  private readonly pecas = inject(Pecas);

  protected readonly metros = metros;
  protected readonly lista = rxResource({ stream: () => this.pedidos.listar() });
  protected readonly comChegada = computed(() => (this.lista.value() ?? []).filter((p) => p.pecasRecebidas > 0));
  protected readonly idPedido = linkedSignal(() => (this.pedido() ? Number(this.pedido()) : null));
  protected readonly chegadas = rxResource({ params: () => this.idPedido() ?? undefined, stream: ({ params }) => this.pedidos.chegadas(params) });
  protected readonly idChegada = linkedSignal(() => (this.chegada() ? Number(this.chegada()) : null));
  protected readonly feixes = computed(() => this.chegadas.value()?.find((c) => c.id === this.idChegada())?.feixes ?? []);
  protected readonly feixe = signal<FeixeDto | null>(null);
  protected readonly doFeixe = rxResource({ params: () => this.feixe()?.id, stream: ({ params }) => this.pecas.doFeixe(params) });

  protected readonly tag = signal('');
  protected readonly tagRelida = signal('');
  protected readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly tagFornecedor = signal('');
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected readonly colunas: Coluna<PecaDto>[] = [
    { titulo: 'Peça', valor: (p) => p.codigo, mono: true },
    { titulo: 'Tag', valor: (p) => p.tag, mono: true },
    { titulo: 'Situação', valor: (p) => p.situacao },
  ];

  protected escolherFeixe(f: FeixeDto): void {
    this.feixe.set(f);
    this.proximaTag();
  }

  protected proximaTag(): void {
    this.pecas.proximaTag().subscribe({ next: (t) => { this.tag.set(t); this.tagRelida.set(''); }, error: (e) => this.erro.set(mensagemDeErro(e)) });
  }

  protected cadastrar(): void {
    const f = this.feixe();
    if (!f || !this.tag().trim()) return;
    this.enviando.set(true);
    this.erro.set(null);
    this.mensagem.set(null);
    this.pecas
      .cadastrar({
        idFeixe: f.id, metodo: this.metodo(), tag: this.tag().trim(), tagRelida: this.tagRelida().trim() || null,
        tagFornecedor: this.tagFornecedor().trim() || null, tagsVistas: 1, idCliente: novoIdCliente(),
      })
      .subscribe({
        next: (r) => {
          this.mensagem.set(r.mensagem || `Peça ${r.peca?.codigo} cadastrada.`);
          this.enviando.set(false);
          this.tagFornecedor.set('');
          this.doFeixe.reload();
          this.chegadas.reload();
          this.proximaTag();
        },
        error: (e) => {
          this.erro.set(mensagemDeErro(e));
          this.enviando.set(false);
        },
      });
  }
}