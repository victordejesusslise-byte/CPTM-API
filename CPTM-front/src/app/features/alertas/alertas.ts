import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Sessao } from '../../core/auth/sessao';
import { novoIdCliente } from '../../helpers/arquivo';
import { nomeDaFoto } from '../../helpers/foto';
import { dataHora, km, mensagemDeErro } from '../../helpers/formato';
import { AlertaDto, AvisoTagDto } from '../../models/campo';
import { PecaDto } from '../../models/peca';
import { Pecas } from '../../services/pecas';
import { MetodoCaptura } from '../../models/enums';
import { Alertas as AlertasApi } from '../../services/alertas';
import { Estado } from '../../shared/estado/estado';
import { LeituraTag, leitura } from '../../shared/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../shared/ui';
import { Nota } from '../../shared/nota/nota';

type Aba = 'imagem' | 'sensores' | 'tags';
const TITULOS: Record<Aba, string> = { imagem: 'Alertas da câmera', sensores: 'Alertas dos sensores', tags: 'Tags sem cadastro' };

/**
 * Alertas (protótipo YBYRA, etapa 09): câmera, sensores e tags sem cadastro.
 * O operador da via vai ver e registra a vistoria; quem decide é o técnico.
 */
@Component({
  selector: 'app-alertas',
  imports: [FormsModule, RouterLink, FORMULARIO, Estado, LeituraTag, Nota],
  templateUrl: './alertas.html',
})
export class Alertas {
  readonly aba = input<Aba>('imagem');
  /** ?aberto= */
  readonly aberto = input<string>();
  private readonly api = inject(AlertasApi);
  private readonly router = inject(Router);
  private readonly sessao = inject(Sessao);

  protected readonly titulo = computed(() => TITULOS[this.aba()]);
  protected readonly so = signal<'abertos' | 'todos'>('abertos');
  protected readonly lista = rxResource({
    params: () => (this.aba() === 'tags' ? undefined : { origem: this.aba() === 'imagem' ? ('Imagem' as const) : ('Sensor' as const), status: this.so() === 'abertos' ? ('Aberto' as const) : null }),
    stream: ({ params }) => this.api.listar(params.origem, params.status),
  });
  protected readonly erroLista = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly idAberto = computed(() => (this.aberto() ? Number(this.aberto()) : null));
  protected readonly alerta = rxResource({ params: () => this.idAberto() ?? undefined, stream: ({ params }) => this.api.buscar(params) });
  protected readonly podeVistoriar = computed(() => this.sessao.temCargo('OperadorVia', 'Tecnico'));
  protected readonly ehTecnico = computed(() => this.sessao.temCargo('Tecnico'));
  protected readonly motivoDescarte = signal('');
  // tags sem cadastro: o técnico acha a peça pelo código e vincula a tag lida no local
  protected readonly podeVincular = computed(() => this.sessao.temCargo('Tecnico', 'Supervisor'));
  protected readonly podeAvisar = computed(() => this.sessao.temCargo('OperadorVia', 'Tecnico'));
  protected readonly avisos = rxResource({ params: () => (this.aba() === 'tags' && this.podeVincular() ? true : undefined), stream: () => this.api.avisos() });
  private readonly pecas = inject(Pecas);
  protected readonly avisoAberto = signal<AvisoTagDto | null>(null);
  protected readonly codigoPeca = signal('');
  protected readonly pecaDoVinculo = signal<PecaDto | null>(null);
  protected readonly tagVinculo = signal('');
  protected readonly metodoVinculo = signal<MetodoCaptura>('ModuloUhf');

  protected readonly dataHora = dataHora;
  protected readonly km = km;

  // vistoria
  protected readonly viu = signal(true);
  protected readonly tagNaoResponde = signal(false);
  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly foto = signal<File | null>(null);
  protected readonly observacao = signal('');
  // aviso de tag
  protected readonly tagAviso = signal('');
  protected readonly metodoAviso = signal<MetodoCaptura>('ModuloUhf');
  protected readonly kmAviso = signal<number | null>(null);
  protected readonly obsAviso = signal('');

  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected rotuloTipo(a: AlertaDto): string {
    const t: Record<string, string> = { Rachadura: 'Rachadura', Posicao: 'Peça fora de posição', TagMuda: 'Tag não respondeu', Fenda: 'Fenda', Quebra: 'Quebra' };
    return t[a.tipo] ?? a.tipo;
  }

  protected abrir(a: AlertaDto): void {
    this.limpar();
    this.router.navigate([], { queryParams: { aberto: a.id } });
  }

  protected fechar(): void {
    this.router.navigate([], { queryParams: {} });
  }

  protected escolherFoto(evento: Event): void {
    this.foto.set((evento.target as HTMLInputElement).files?.[0] ?? null);
  }

  protected vistoriar(a: AlertaDto): void {
    if (this.viu() && !this.tagNaoResponde() && !this.tag().trim()) {
      this.erro.set('Leia a tag da peça ou marque que ela não responde.');
      return;
    }
    if (!this.foto()) {
      this.erro.set('Tire a foto da peça.');
      return;
    }
    this.enviar(
      this.api.vistoriar(a.id, {
        viu: this.viu(),
        leitura: this.viu() && !this.tagNaoResponde() ? leitura(this.tag(), this.metodo()) : null,
        tagNaoResponde: this.viu() && this.tagNaoResponde(),
        fotoArquivo: nomeDaFoto(this.foto(), `vistoria-${a.id}`),
        observacao: this.observacao().trim() || null,
        idCliente: novoIdCliente(),
      }),
      () => {
        this.alerta.reload();
        this.lista.reload();
        return 'Vistoria registrada. O técnico decide o alerta.';
      },
    );
  }

  /** Técnico: descarta o alerta com motivo (quem confirma é a inspeção reprovada). */
  protected descartar(a: AlertaDto): void {
    if (!this.motivoDescarte().trim()) {
      this.erro.set('Escreva por que o alerta é descartado.');
      return;
    }
    this.enviar(this.api.descartar(a.id, this.motivoDescarte().trim()), () => {
      this.alerta.reload();
      this.lista.reload();
      return 'Alerta descartado.';
    });
  }

  protected abrirAviso(a: AvisoTagDto): void {
    this.avisoAberto.set(a);
    this.tagVinculo.set(a.tag);
    this.pecaDoVinculo.set(null);
    this.codigoPeca.set('');
    this.erro.set(null);
    this.mensagem.set(null);
  }

  protected buscarPeca(): void {
    if (!this.codigoPeca().trim()) return;
    this.erro.set(null);
    this.pecas.porCodigo(this.codigoPeca()).subscribe({
      next: (p) => this.pecaDoVinculo.set(p),
      error: (e) => {
        this.pecaDoVinculo.set(null);
        this.erro.set(e?.status === 404 ? 'Nenhuma peça com esse código. Confira o código impresso na peça (ex.: DM-E01-L02-0034).' : mensagemDeErro(e));
      },
    });
  }

  protected vincular(): void {
    const p = this.pecaDoVinculo();
    if (!p || !this.tagVinculo().trim()) return this.erro.set('Ache a peça e leia a tag dela no local.');
    this.enviar(this.api.vincular(p.id, leitura(this.tagVinculo(), this.metodoVinculo())), (r) => {
      this.avisoAberto.set(null);
      this.pecaDoVinculo.set(null);
      this.avisos.reload();
      return `Tag ${r.tag} vinculada à peça ${r.codigo}.`;
    });
  }

  protected avisar(): void {
    if (!this.tagAviso().trim()) {
      this.erro.set('Leia a tag que respondeu sem cadastro.');
      return;
    }
    this.enviar(this.api.avisarTag({ tag: this.tagAviso().trim(), km: this.kmAviso(), observacao: this.obsAviso().trim() || null }), (r) => {
      this.tagAviso.set('');
      this.obsAviso.set('');
      return `Aviso enviado ao técnico: tag ${r.tag}.`;
    });
  }

  protected limpar(): void {
    this.mensagem.set(null);
    this.erro.set(null);
    this.viu.set(true);
    this.tagNaoResponde.set(false);
    this.tag.set('');
    this.foto.set(null);
    this.observacao.set('');
  }

  private enviar<T>(acao: import('rxjs').Observable<T>, ok: (r: T) => string): void {
    this.enviando.set(true);
    this.erro.set(null);
    this.mensagem.set(null);
    acao.subscribe({
      next: (r) => {
        this.enviando.set(false);
        this.mensagem.set(ok(r));
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}