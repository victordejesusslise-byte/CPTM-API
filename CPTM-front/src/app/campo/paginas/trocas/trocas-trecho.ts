import { Component, computed, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, map, of } from 'rxjs';
import { nomeDaFoto } from '../../../compartilhado/utilitarios/foto';
import { dataHora, km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { TrocaDto } from '../../entidades/campo';
import { MetodoCaptura, StatusTroca } from '../../../compartilhado/entidades/enums';
import { Trocas } from '../../servicos/trocas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';
import { ROTULO_TROCA, ResumoTroca } from './resumo-troca';

type Aba = 'trocar' | 'recusas' | 'feitas';
const ABAS: Record<Aba, { titulo: string; status: StatusTroca[] }> = {
  trocar: { titulo: 'A trocar', status: ['Separada'] },
  recusas: { titulo: 'Recusas enviadas ao técnico', status: ['Recusada'] },
  feitas: { titulo: 'Trocas feitas', status: ['Feita'] },
};

/** Trocas do trecho (protótipo YBYRA, etapa 13): o operador da via troca a peça pela separada, ou recusa com motivo e foto. */
@Component({
  selector: 'app-trocas-trecho',
  imports: [Esqueleto, FormsModule, FORMULARIO, Estado, LeituraTag, ResumoTroca],
  templateUrl: './trocas-trecho.html',
})
export class TrocasTrecho {
  readonly aba = input<Aba>('trocar');
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
  protected readonly rotulo = ROTULO_TROCA;
  protected readonly dataHora = dataHora;
  protected readonly km = km;

  protected readonly acao = signal<'executar' | 'recusar' | null>(null);
  protected readonly tagSai = signal('');
  protected readonly metodoSai = signal<MetodoCaptura>('ModuloUhf');
  protected readonly saiNaoResponde = signal(false);
  protected readonly tagEntra = signal('');
  protected readonly metodoEntra = signal<MetodoCaptura>('ModuloUhf');
  protected readonly equipe = signal('');
  protected readonly kmFeito = signal<number | null>(null);
  protected readonly motivo = signal('');
  protected readonly descricao = signal('');
  protected readonly foto = signal<File | null>(null);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected abrir(t: TrocaDto): void {
    this.acao.set(null);
    this.mensagem.set(null);
    this.erro.set(null);
    this.router.navigate([], { queryParams: { aberta: t.id } });
  }

  protected fechar(): void {
    this.router.navigate([], { queryParams: {} });
  }

  protected escolherFoto(e: Event): void {
    this.foto.set((e.target as HTMLInputElement).files?.[0] ?? null);
  }

  protected executar(t: TrocaDto): void {
    if ((!this.saiNaoResponde() && !this.tagSai().trim()) || !this.tagEntra().trim() || !this.equipe().trim() || !this.foto()) {
      this.erro.set('Leia a tag da peça que sai (ou marque que não responde), a da peça que entra, informe a equipe e tire a foto.');
      return;
    }
    this.enviar(
      this.api.executar(t.id, {
        leituraSaiu: this.saiNaoResponde() ? null : leitura(this.tagSai(), this.metodoSai()),
        tagSaiuNaoResponde: this.saiNaoResponde(),
        leituraEntrou: leitura(this.tagEntra(), this.metodoEntra()),
        equipe: this.equipe().trim(),
        fotoArquivo: nomeDaFoto(this.foto(), `troca-${t.id}`),
        km: this.kmFeito() ?? t.km,
      }),
      'Troca feita. A peça nova passa a ser lida na passagem.',
    );
  }

  protected recusar(t: TrocaDto): void {
    if (!this.motivo().trim() || !this.foto()) {
      this.erro.set('Escreva o motivo e tire a foto.');
      return;
    }
    this.enviar(
      this.api.recusar(t.id, { motivo: this.motivo().trim(), descricao: this.descricao().trim() || null, fotoArquivo: nomeDaFoto(this.foto(), `recusa-${t.id}`) }),
      'Recusa enviada ao técnico.',
    );
  }

  private enviar(acao: import('rxjs').Observable<unknown>, ok: string): void {
    this.enviando.set(true);
    this.erro.set(null);
    acao.subscribe({
      next: () => {
        this.enviando.set(false);
        this.acao.set(null);
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