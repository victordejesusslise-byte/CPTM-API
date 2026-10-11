import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { forkJoin, map, of, switchMap } from 'rxjs';
import { abrirArquivo } from '../../helpers/arquivo';
import { dataHora, mensagemDeErro, metros } from '../../helpers/formato';
import { ChegadaDto, PedidoDto } from '../../models/pedido';
import { Laudos } from '../../services/laudos';
import { Pedidos } from '../../services/pedidos';
import { Estado } from '../../shared/estado/estado';

type Modo = 'entregas' | 'andamento' | 'boletins';
interface Carga { pedido: PedidoDto; chegada: ChegadaDto; }

const TEXTOS: Record<Modo, { sobre: string; titulo: string; nota: string }> = {
  entregas: { sobre: 'Pátio · 02', titulo: 'Entregas recebidas', nota: 'toque numa carga para abrir o boletim (BR) em PDF' },
  andamento: { sobre: 'Pátio · 03', titulo: 'Andamento da carga', nota: 'quantas peças de cada feixe já ganharam a tag' },
  boletins: { sobre: 'Madeira · 04', titulo: 'Cargas e boletins', nota: 'o boletim de recebimento de cada carga' },
};

/** As cargas que chegaram, de todos os pedidos: entregas (Chegada), andamento (Cadastro) e boletins (Qualidade). */
@Component({
  selector: 'app-lista-chegadas',
  imports: [Estado],
  template: `
    <p class="sobre">{{ texto().sobre }}</p>
    <h1>{{ texto().titulo }}</h1>
    <section class="cartao">
      <header><h2>{{ texto().titulo }}</h2><span class="nota">{{ cargas.value()?.length ?? 0 }} cargas · {{ texto().nota }}</span></header>
      <app-estado [carregando]="cargas.isLoading()" [erro]="erro()" [vazio]="cargas.hasValue() && !cargas.value().length" textoVazio="Nenhuma carga chegou ainda." />
      @if (modo() === 'andamento') {
        @for (c of cargas.value() ?? []; track c.chegada.id) {
          <div class="pilha carga">
            <p><b class="mono">{{ c.chegada.lote }}</b> · pedido <span class="mono">{{ c.pedido.codigo }}</span> · {{ c.chegada.armazem }} · {{ dataHora(c.chegada.dataRecebimento) }}</p>
            <div class="tabela">
              <table class="simples">
                <thead><tr><th>Feixe</th><th>Espécie</th><th>Tamanho</th><th class="num">Cadastradas</th></tr></thead>
                <tbody>
                  @for (f of c.chegada.feixes; track f.id) {
                    <tr><td class="mono">{{ f.numero }}</td><td>{{ f.especie }}</td><td>{{ metros(f.comprimentoM) }} · {{ f.classe }}ª</td>
                      <td class="num">{{ f.cadastradas }} de {{ f.quantidade }}<div class="barra"><i [style.width.%]="f.quantidade ? f.cadastradas / f.quantidade * 100 : 0"></i></div></td></tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        }
      } @else if (cargas.value()?.length) {
        <div class="tabela">
          <table class="simples">
            <thead><tr><th>Chegou em</th><th>Pedido</th><th class="so-computador">Fornecedor</th><th>Armazém</th><th class="num">Peças</th><th class="so-computador">Lote</th><th>Conferência</th></tr></thead>
            <tbody>
              @for (c of cargas.value(); track c.chegada.id) {
                <tr class="clicavel" tabindex="0" (click)="boletim(c.chegada)" (keydown.enter)="boletim(c.chegada)">
                  <td>{{ dataHora(c.chegada.dataRecebimento) }}</td><td class="mono">{{ c.pedido.codigo }}</td><td class="so-computador">{{ c.pedido.empresa }}</td>
                  <td>{{ c.chegada.armazem }}</td><td class="num">{{ c.chegada.quantidadeRecebida }}</td><td class="mono so-computador">{{ c.chegada.lote }}</td>
                  <td [class.vermelho]="!c.chegada.confere">{{ c.chegada.confere ? 'confere' : c.chegada.divergencias.join(' ') }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
  styles: `.carga { padding: 12px 0; border-top: 1px solid var(--linha); } .carga:first-of-type { border-top: 0; } .carga p { margin: 0; }`,
})
export class ListaChegadas {
  /** vem do data da rota */
  readonly modo = input<Modo>('entregas');
  private readonly pedidos = inject(Pedidos);
  private readonly laudos = inject(Laudos);
  protected readonly dataHora = dataHora;
  protected readonly metros = metros;
  protected readonly texto = computed(() => TEXTOS[this.modo()]);
  protected readonly cargas = rxResource({
    stream: () =>
      this.pedidos.listar().pipe(
        switchMap((lista) => {
          const com = lista.filter((p) => p.pecasRecebidas > 0);
          return com.length ? forkJoin(com.map((p) => this.pedidos.chegadas(p.id).pipe(map((cs) => cs.map((chegada) => ({ pedido: p, chegada }) as Carga))))) : of([] as Carga[][]);
        }),
        map((grupos) => grupos.flat().sort((a, b) => b.chegada.dataRecebimento.localeCompare(a.chegada.dataRecebimento))),
      ),
  });
  protected readonly erro = computed(() => (this.cargas.error() ? mensagemDeErro(this.cargas.error()) : null));

  protected boletim(c: ChegadaDto): void {
    this.laudos.pdf('recebimento', c.id).subscribe((pdf) => abrirArquivo(pdf, `BR-${c.id}.pdf`));
  }
}