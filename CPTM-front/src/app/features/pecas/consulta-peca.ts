import { Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { dataHora, km, mensagemDeErro, metros } from '../../helpers/formato';
import { MetodoCaptura } from '../../models/enums';
import { PecaDto } from '../../models/peca';
import { Pecas } from '../../services/pecas';
import { LeituraTag } from '../../shared/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../shared/ui';

/** Consultar a peça pela tag: onde está e tudo o que aconteceu com ela. */
@Component({
  selector: 'app-consulta-peca',
  imports: [FORMULARIO, LeituraTag],
  template: `
    <p class="sobre">Peça</p>
    <h1>Consultar peça</h1>
    <app-leitura-tag [(tag)]="tag" [(metodo)]="metodo" (lida)="buscar()" />
    <div class="acoes"><button mat-stroked-button type="button" (click)="buscar()">Buscar</button></div>
    @if (erro()) { <p class="aviso aviso--erro" role="alert">{{ erro() }}</p> }
    @if (peca(); as p) {
      <div class="cartao">
        <h2 class="mono">{{ p.codigo }}</h2>
        <p>{{ p.especie }} · {{ metros(p.comprimentoM) }} · {{ p.classe }}ª classe · <span class="etiqueta">{{ p.situacao }}</span></p>
        <p class="rotulo">Tag {{ p.tag ?? '—' }} · feixe {{ p.numeroFeixe }} · {{ p.armazem }} · pilha {{ p.pilha ?? '—' }}
          @if (p.kmProjeto !== null) { · km {{ km(p.kmProjeto) }} }</p>
      </div>
      <h2>Histórico</h2>
      @for (e of historico.value() ?? []; track $index) {
        <p class="aviso"><strong>{{ dataHora(e.data) }} · {{ e.tipo }}</strong> — {{ e.descricao }}</p>
      }
    }
  `,
})
export class ConsultaPeca {
  private readonly pecas = inject(Pecas);
  protected readonly metros = metros;
  protected readonly km = km;
  protected readonly dataHora = dataHora;
  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('Pistola');
  protected readonly peca = signal<PecaDto | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly historico = rxResource({ params: () => this.peca()?.id, stream: ({ params }) => this.pecas.historico(params) });

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.erro.set(null);
    this.pecas.porTag(this.tag()).subscribe({
      next: (p) => this.peca.set(p),
      error: (e) => {
        this.peca.set(null);
        this.erro.set(e?.status === 404 ? 'Nenhuma peça com essa tag.' : mensagemDeErro(e));
      },
    });
  }
}