import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, map, of } from 'rxjs';
import { Sessao } from '../../core/auth/sessao';
import { abrirArquivo } from '../../helpers/arquivo';
import { dataHora, km, mensagemDeErro } from '../../helpers/formato';
import { TrocaDto } from '../../models/campo';
import { StatusTroca } from '../../models/enums';
import { Laudos, TipoLaudo } from '../../services/laudos';
import { Trocas } from '../../services/trocas';
import { Estado } from '../../shared/estado/estado';
import { FORMULARIO } from '../../shared/ui';
import { ROTULO_TROCA, ResumoTroca } from './resumo-troca';

type Aba = 'aprovar' | 'decididas' | 'andamento' | 'recusas' | 'feitas';
const ABAS: Record<Aba, { sobre: string; titulo: string; status: StatusTroca[] }> = {
  aprovar: { sobre: 'Troca · 11', titulo: 'Para aprovar', status: ['Pedida'] },
  decididas: { sobre: 'Troca · 11', titulo: 'Decididas', status: ['Aprovada', 'AguardandoPeca', 'Reprovada'] },
  andamento: { sobre: 'Campo', titulo: 'Trocas em andamento', status: ['Pedida', 'Aprovada', 'AguardandoPeca', 'Separada'] },
  recusas: { sobre: 'Campo', titulo: 'Recusas da via', status: ['Recusada'] },
  feitas: { sobre: 'Campo', titulo: 'Trocas feitas', status: ['Feita'] },
};

/** Aprovar trocas (supervisor, etapa 11) e Trocas e laudos (técnico e supervisor): decidir, acompanhar, responder a recusa e os laudos. */
@Component({
  selector: 'app-trocas-gestao',
  imports: [FormsModule, FORMULARIO, Estado, ResumoTroca],
  templateUrl: './trocas-gestao.html',
})
export class TrocasGestao {
  readonly aba = input<Aba>('andamento');
  readonly aberta = input<string>();
  private readonly api = inject(Trocas);
  private readonly laudos = inject(Laudos);
  private readonly router = inject(Router);
  private readonly sessao = inject(Sessao);

  protected readonly texto = computed(() => ABAS[this.aba()]);
  protected readonly lista = rxResource({
    params: () => ABAS[this.aba()].status,
    stream: ({ params }) => forkJoin(params.map((s) => this.api.listar(s))).pipe(map((l) => l.flat().sort((a, b) => b.dataPedido.localeCompare(a.dataPedido)))),
  });
  protected readonly erroLista = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly idAberta = computed(() => (this.aberta() ? Number(this.aberta()) : null));
  protected readonly troca = rxResource({ params: () => this.idAberta() ?? undefined, stream: ({ params }) => (params ? this.api.buscar(params) : of(null)) });
  protected readonly ehSupervisor = computed(() => this.sessao.temCargo('Supervisor'));
  protected readonly ehTecnico = computed(() => this.sessao.temCargo('Tecnico'));
  protected readonly rotulo = ROTULO_TROCA;
  protected readonly dataHora = dataHora;
  protected readonly km = km;

  protected readonly motivo = signal('');
  protected readonly resposta = signal('');
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected abrir(t: TrocaDto): void {
    this.mensagem.set(null);
    this.erro.set(null);
    this.motivo.set('');
    this.resposta.set('');
    this.router.navigate([], { queryParams: { aberta: t.id } });
  }

  protected fechar(): void {
    this.router.navigate([], { queryParams: {} });
  }

  protected decidir(t: TrocaDto, aprovar: boolean): void {
    if (!aprovar && !this.motivo().trim()) {
      this.erro.set('Para reprovar, escreva o motivo.');
      return;
    }
    this.enviar(this.api.decidir(t.id, aprovar, this.motivo().trim() || null), aprovar ? 'Troca aprovada. O pátio já pode separar a peça.' : 'Troca reprovada.');
  }

  protected decidirRecusa(t: TrocaDto, decisao: 'Manter' | 'Aceitar'): void {
    this.enviar(
      this.api.decidirRecusa(t.id, decisao, this.resposta().trim() || null),
      decisao === 'Manter' ? 'Troca mantida: volta para a via fazer.' : 'Recusa aceita: a troca foi cancelada.',
    );
  }

  protected laudo(t: TrocaDto, tipo: TipoLaudo): void {
    this.laudos.pdf(tipo, t.id).subscribe({
      next: (pdf) => abrirArquivo(pdf, `${tipo === 'liberacao' ? 'LB' : 'LD'}-${t.id}.pdf`),
      error: (e) => this.erro.set(mensagemDeErro(e)),
    });
  }

  private enviar(acao: import('rxjs').Observable<unknown>, ok: string): void {
    this.enviando.set(true);
    this.erro.set(null);
    acao.subscribe({
      next: () => {
        this.enviando.set(false);
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