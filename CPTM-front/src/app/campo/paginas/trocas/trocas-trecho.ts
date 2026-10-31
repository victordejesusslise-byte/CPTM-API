import { Component, computed, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, map, of } from 'rxjs';
import { dataHora, km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { Fila, NovoRegistro } from '../../../nucleo/fila/fila';
import { TrabalhoDaNoite } from '../../../nucleo/ao-vivo/trabalho-da-noite';
import { TrocaDto } from '../../entidades/campo';
import { MetodoCaptura, StatusTroca } from '../../../compartilhado/entidades/enums';
import { Trocas } from '../../servicos/trocas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';
import { ROTULO_TROCA, ResumoTroca } from './resumo-troca';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

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
  protected readonly valorDe = valorDe;
  readonly aba = input<Aba>('trocar');
  readonly aberta = input<string>();
  private readonly api = inject(Trocas);
  private readonly fila = inject(Fila);
  private readonly trabalho = inject(TrabalhoDaNoite);
  protected readonly semRede = this.trabalho.usarGuardado;
  private readonly router = inject(Router);

  protected readonly titulo = computed(() => ABAS[this.aba()].titulo);
  // Sem rede (no app): o plano guardado, sem as trocas que já foram feitas ou recusadas e esperam na fila.
  protected readonly lista = rxResource({
    params: () => ({ status: ABAS[this.aba()].status, guardado: this.semRede() }),
    stream: ({ params }) =>
      params.guardado
        ? of(this.trabalho.trocas(params.status).filter((t) => !this.fila.itens().some((i) => (i.tipo === 'troca' || i.tipo === 'recusa') && i.alvo === t.id)))
        : forkJoin(params.status.map((s) => this.api.listar(s))).pipe(map((l) => l.flat())),
  });
  protected readonly erroLista = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly idAberta = computed(() => (this.aberta() ? Number(this.aberta()) : null));
  protected readonly troca = rxResource({
    params: () => (this.idAberta() ? { id: this.idAberta()!, guardado: this.semRede() } : undefined),
    stream: ({ params }) => (params.guardado ? of(this.trabalho.troca(params.id)) : this.api.buscar(params.id)),
  });
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
    void this.registrar(
      {
        tipo: 'troca', alvo: t.id, rotulo: `Troca da peça ${t.codigoSaiu ?? t.idDormenteSaiu}`,
        corpo: {
          leituraSaiu: this.saiNaoResponde() ? null : leitura(this.tagSai(), this.metodoSai()),
          tagSaiuNaoResponde: this.saiNaoResponde(),
          leituraEntrou: leitura(this.tagEntra(), this.metodoEntra()),
          equipe: this.equipe().trim(),
          km: this.kmFeito() ?? t.km,
        },
      },
      'Troca feita. A peça nova passa a ser lida na passagem.',
      'Sem rede: a troca ficou guardada no celular e sobe sozinha quando a rede voltar.',
    );
  }

  protected recusar(t: TrocaDto): void {
    if (!this.motivo().trim() || !this.foto()) {
      this.erro.set('Escreva o motivo e tire a foto.');
      return;
    }
    void this.registrar(
      {
        tipo: 'recusa', alvo: t.id, rotulo: `Recusa da troca da peça ${t.codigoSaiu ?? t.idDormenteSaiu}`,
        corpo: { motivo: this.motivo().trim(), descricao: this.descricao().trim() || null },
      },
      'Recusa enviada ao técnico.',
      'Sem rede: a recusa ficou guardada no celular e sobe sozinha quando a rede voltar.',
    );
  }

  /** Pela fila: com rede, sobe na hora; sem rede, fica guardada no celular. O que o servidor recusa na hora aparece aqui. */
  private async registrar(registro: NovoRegistro, ok: string, semRede: string): Promise<void> {
    this.enviando.set(true);
    this.erro.set(null);
    const r = await this.fila.registrar(registro, this.foto()!);
    this.enviando.set(false);
    if (r.estado === 'recusado') {
      this.erro.set(r.motivo);
      return;
    }
    this.acao.set(null);
    this.mensagem.set(r.estado === 'enviado' ? ok : semRede);
    this.troca.reload();
    this.lista.reload();
  }
}