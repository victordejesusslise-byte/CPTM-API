import { Component, computed, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Sessao } from '../../../nucleo/sessao/sessao';
import { Fila } from '../../../nucleo/fila/fila';
import { TrabalhoDaNoite } from '../../../nucleo/ao-vivo/trabalho-da-noite';
import { of } from 'rxjs';
import { dataHora, km, mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { AlertaDto, AvisoTagDto } from '../../entidades/campo';
import { PecaDto } from '../../../pecas/entidades/peca';
import { Pecas } from '../../../pecas/servicos/pecas';
import { MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { Alertas as AlertasApi } from '../../servicos/alertas';
import { Estado } from '../../../compartilhado/componentes/estado/estado';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';
import { Nota } from '../../../compartilhado/componentes/nota/nota';

type Aba = 'imagem' | 'sensores' | 'tags';
const TITULOS: Record<Aba, string> = { imagem: 'Alertas da câmera', sensores: 'Alertas dos sensores', tags: 'Tags sem cadastro' };

/**
 * Alertas (protótipo YBYRA, etapa 09): câmera, sensores e tags sem cadastro.
 * O operador da via vai ver e registra a vistoria; quem decide é o técnico.
 */
@Component({
  selector: 'app-alertas',
  imports: [Esqueleto, FormsModule, RouterLink, FORMULARIO, Estado, LeituraTag, Nota],
  templateUrl: './alertas.html',
})
export class Alertas {
  readonly aba = input<Aba>('imagem');
  /** ?aberto= */
  readonly aberto = input<string>();
  private readonly api = inject(AlertasApi);
  private readonly router = inject(Router);
  private readonly sessao = inject(Sessao);
  private readonly fila = inject(Fila);
  private readonly trabalho = inject(TrabalhoDaNoite);
  protected readonly semRede = this.trabalho.usarGuardado;

  protected readonly titulo = computed(() => TITULOS[this.aba()]);
  protected readonly so = signal<'abertos' | 'todos'>('abertos');
  protected readonly lista = rxResource({
    params: () =>
      this.aba() === 'tags'
        ? undefined
        : { origem: this.aba() === 'imagem' ? ('Imagem' as const) : ('Sensor' as const), status: this.so() === 'abertos' ? ('Aberto' as const) : null, guardado: this.semRede() },
    // Sem rede (no app): os alertas abertos do plano guardado.
    stream: ({ params }) => (params.guardado ? of(this.trabalho.alertas(params.origem)) : this.api.listar(params.origem, params.status)),
  });
  protected readonly erroLista = computed(() => (this.lista.error() ? mensagemDeErro(this.lista.error()) : null));
  protected readonly idAberto = computed(() => (this.aberto() ? Number(this.aberto()) : null));
  protected readonly alerta = rxResource({
    params: () => (this.idAberto() ? { id: this.idAberto()!, guardado: this.semRede() } : undefined),
    stream: ({ params }) => (params.guardado ? of(this.trabalho.alerta(params.id)!) : this.api.buscar(params.id)),
  });
  protected readonly podeVistoriar = computed(() => this.sessao.temCargo('OperadorVia', 'Tecnico'));
  protected readonly ehTecnico = computed(() => this.sessao.temCargo('Tecnico'));
  protected readonly motivoDescarte = signal('');
  // tags sem cadastro: o técnico acha a peça pelo código e vincula a tag lida no local
  protected readonly podeVincular = computed(() => this.sessao.temCargo('Tecnico', 'Supervisor'));
  // Quem avisa é o operador da via; o técnico e o supervisor vinculam direto (a tela não manda o técnico avisar a si mesmo).
  protected readonly podeAvisar = computed(() => this.sessao.temCargo('OperadorVia', 'Tecnico'));
  protected readonly avisos = rxResource({ params: () => (this.aba() === 'tags' && this.podeVincular() ? true : undefined), stream: () => this.api.avisos() });
  private readonly pecas = inject(Pecas);
  protected readonly avisoAberto = signal<AvisoTagDto | null>(null);
  /** O técnico leu agora uma tag sem cadastro e vai vinculá-la (sem aviso da via). */
  protected readonly vinculoDireto = signal(false);
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
    void this.vistoriarPelaFila(a);
  }

  /** Pela fila: com rede, sobe na hora; sem rede, fica guardada no celular e sobe quando a rede voltar. */
  private async vistoriarPelaFila(a: AlertaDto): Promise<void> {
    this.enviando.set(true);
    this.erro.set(null);
    this.mensagem.set(null);
    const r = await this.fila.registrar(
      {
        tipo: 'vistoria', alvo: a.id, rotulo: `Vistoria do alerta ${a.id}`,
        corpo: {
          viu: this.viu(),
          leitura: this.viu() && !this.tagNaoResponde() ? leitura(this.tag(), this.metodo()) : null,
          tagNaoResponde: this.viu() && this.tagNaoResponde(),
          observacao: this.observacao().trim() || null,
        },
      },
      this.foto()!,
    );
    this.enviando.set(false);
    if (r.estado === 'recusado') {
      this.erro.set(r.motivo);
      return;
    }
    this.mensagem.set(r.estado === 'enviado' ? 'Vistoria registrada. O técnico decide o alerta.' : 'Sem rede: a vistoria ficou guardada no celular e sobe sozinha quando a rede voltar.');
    this.alerta.reload();
    this.lista.reload();
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

  /** Técnico ou supervisor: a tag que ele mesmo leu vai direto para o vínculo. */
  protected vincularDireto(): void {
    if (!this.tagAviso().trim()) {
      this.erro.set('Leia a tag que respondeu sem cadastro.');
      return;
    }
    this.avisoAberto.set(null);
    this.vinculoDireto.set(true);
    this.tagVinculo.set(this.tagAviso().trim());
    this.metodoVinculo.set(this.metodoAviso());
    this.pecaDoVinculo.set(null);
    this.codigoPeca.set('');
    this.erro.set(null);
    this.mensagem.set(null);
  }

  protected fecharVinculo(): void {
    this.avisoAberto.set(null);
    this.vinculoDireto.set(false);
  }

  protected abrirAviso(a: AvisoTagDto): void {
    this.vinculoDireto.set(false);
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
      this.vinculoDireto.set(false);
      this.tagAviso.set('');
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