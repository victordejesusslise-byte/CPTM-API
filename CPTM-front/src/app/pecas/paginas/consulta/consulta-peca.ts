import { Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { dataHora, km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { FotoDaPecaDto, FotosDaPecaDto, PecaDto } from '../../entidades/peca';
import { Nota } from '../../../compartilhado/componentes/nota/nota';
import { Pecas } from '../../servicos/pecas';
import { LeituraTag } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';

/** Consultar a peça pela tag: onde está e tudo o que aconteceu com ela. */
@Component({
  selector: 'app-consulta-peca',
  imports: [FORMULARIO, LeituraTag, Nota],
  template: `
    <p class="sobre">Peça</p>
    <h1>Consultar peça</h1>
    <app-leitura-tag [(tag)]="tag" [(metodo)]="metodo" (lida)="buscar()" />
    <div class="acoes"><button mat-stroked-button type="button" (click)="buscar()">Buscar</button></div>
    @if (erro()) { <p class="aviso aviso--erro" role="alert">{{ erro() }}</p> }
    @if (peca(); as p) {
      <div class="cartao">
        <h2 class="mono">{{ p.codigo }}</h2>
        <p>{{ p.especie }} · {{ metros(p.comprimentoM) }} · {{ p.classe }}ª classe · <span class="etiqueta">{{ p.situacao }}</span> <app-nota [nota]="p.nota" /></p>
        <p class="rotulo">Tag {{ p.tag ?? '—' }} · feixe {{ p.numeroFeixe }} · {{ p.armazem }} · pilha {{ p.pilha ?? '—' }}
          @if (p.kmProjeto !== null) { · km {{ km(p.kmProjeto) }} }
          @if (p.posicao) { · posição <b class="mono">{{ p.posicao }}</b> }</p>
      </div>
      <h2>Como estava × como ficou</h2>
      <div class="comparar">
        <figure class="cartao">
          <figcaption class="lbl">Como estava · entrada</figcaption>
          @if (fotos()?.entrada?.[0]; as f) {
            @if (urls()[f.arquivo]; as url) { <img [src]="url" alt="Foto da peça na entrada" /> } @else { <p class="nota">{{ f.noServidor ? 'Carregando…' : 'Arquivo fora do servidor: ' + f.arquivo }}</p> }
            <p class="nota">{{ dataHora(f.data) }} · {{ f.descricao }}</p>
          } @else {
            <p class="aviso">Sem foto da entrada (a peça foi verificada antes da foto obrigatória).</p>
          }
        </figure>
        <figure class="cartao">
          <figcaption class="lbl">Como ficou · câmera do carro na via</figcaption>
          @if (fotos()?.via?.[0]; as f) {
            @if (urls()[f.arquivo]; as url) { <img [src]="url" alt="Quadro da câmera do carro" /> } @else { <p class="nota mono">Quadro {{ f.arquivo }}</p> }
            <p class="nota">{{ dataHora(f.data) }} · {{ f.descricao }}</p>
          } @else {
            <p class="aviso">A câmera do carro ainda não mandou quadro desta peça.</p>
          }
        </figure>
      </div>
      <p class="chamada chamada--neutra"><b>Análise da IA:</b> {{ analise() }}</p>
      <h2>Histórico</h2>
      @for (e of historico.value() ?? []; track $index) {
        <p class="aviso"><strong>{{ dataHora(e.data) }} · {{ e.tipo }}</strong> — {{ e.descricao }}</p>
      }
    }
  `,
})
export class ConsultaPeca implements OnInit, OnDestroy {
  /** a tag que veio na rota (?tag=), do mapa do trecho */
  readonly tagDaRota = input<string | undefined>(undefined, { alias: 'tag' });
  private readonly pecas = inject(Pecas);
  protected readonly metros = metros;
  protected readonly km = km;
  protected readonly dataHora = dataHora;
  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly peca = signal<PecaDto | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly historico = rxResource({ params: () => this.peca()?.id, stream: ({ params }) => this.pecas.historico(params) });
  protected readonly fotos = signal<FotosDaPecaDto | null>(null);
  protected readonly urls = signal<Record<string, string>>({});
  protected analise(): string {
    const f = this.fotos();
    const comAnalise = [...(f?.entrada ?? []), ...(f?.via ?? [])].find((x) => x.analiseIa);
    return comAnalise?.analiseIa ?? 'aguardando o modelo de imagem. Quando ele existir, compara a foto da entrada com o quadro da via e dá a nota.';
  }

  private carregarFotos(id: number): void {
    this.liberar();
    this.fotos.set(null);
    this.pecas.fotos(id).subscribe((f) => {
      this.fotos.set(f);
      for (const foto of [f.entrada[0], f.via[0]].filter((x): x is FotoDaPecaDto => !!x && x.noServidor))
        this.pecas.arquivoFoto(foto.arquivo).subscribe((b) => this.urls.update((u) => ({ ...u, [foto.arquivo]: URL.createObjectURL(b) })));
    });
  }

  private liberar(): void {
    Object.values(this.urls()).forEach((u) => URL.revokeObjectURL(u));
    this.urls.set({});
  }

  ngOnInit(): void {
    const tag = this.tagDaRota();
    if (tag) {
      this.tag.set(tag);
      this.buscar();
    }
  }

  ngOnDestroy(): void {
    this.liberar();
  }

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.erro.set(null);
    this.pecas.porTag(this.tag()).subscribe({
      next: (p) => {
        this.peca.set(p);
        this.carregarFotos(p.id);
      },
      error: (e) => {
        this.peca.set(null);
        this.erro.set(e?.status === 404 ? 'Nenhuma peça com essa tag.' : mensagemDeErro(e));
      },
    });
  }
}