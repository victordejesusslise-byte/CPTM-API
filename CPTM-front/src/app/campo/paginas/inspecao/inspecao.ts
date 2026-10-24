import { Component, inject, input, signal } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { finalize } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { Fila } from '../../../nucleo/fila/fila';
import { TrabalhoDaNoite } from '../../../nucleo/ao-vivo/trabalho-da-noite';
import { dataHora, km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { AtaqueBiologicoInspecao, EstadoConservacao, MetodoCaptura, RachaduraInspecao } from '../../../compartilhado/entidades/enums';
import { PecaDto } from '../../../pecas/entidades/peca';
import { Nota } from '../../../compartilhado/componentes/nota/nota';
import { InspecaoDto, Inspecoes } from '../../servicos/inspecoes';
import { PreviaLaudo } from '../../../laudos/componentes/previa-laudo';
import { Pecas } from '../../../pecas/servicos/pecas';
import { Trocas } from '../../servicos/trocas';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { FORMULARIO } from '../../../compartilhado/ui';

/**
 * Inspeção (protótipo YBYRA, etapa 10; técnico): lê a peça, preenche o checklist com foto e, se reprovar, pede a troca.
 * Aberta a partir de um alerta (?alerta=), a inspeção decide o alerta. Em "Inspeções feitas", as da peça com o laudo (LI).
 */
@Component({
  selector: 'app-inspecao',
  imports: [Esqueleto, FormsModule, FORMULARIO, LeituraTag, Nota, PreviaLaudo],
  templateUrl: './inspecao.html',
})
export class Inspecao {
  /** Buscando a peça pela tag: o cartão dela mostra o skeleton. */
  protected readonly buscando = signal(false);
  readonly aba = input<'checklist' | 'historico'>('checklist');
  readonly alerta = input<string>();
  private readonly pecas = inject(Pecas);
  private readonly inspecoes = inject(Inspecoes);
  private readonly fila = inject(Fila);
  private readonly trabalho = inject(TrabalhoDaNoite);
  protected readonly semRede = this.trabalho.usarGuardado;
  private readonly trocas = inject(Trocas);
  /** A inspeção cujo laudo (LI) está aberto na primeira visão, sem baixar. */
  protected readonly laudoDe = signal<number | null>(null);
  protected readonly metros = metros;
  protected readonly km = km;
  protected readonly dataHora = dataHora;

  protected readonly tag = signal('');
  protected readonly metodo = signal<MetodoCaptura>('ModuloUhf');
  protected readonly peca = signal<PecaDto | null>(null);
  protected readonly iniciadoEm = signal<string | null>(null);
  protected readonly feitas = signal<InspecaoDto[]>([]);
  protected readonly estado = signal<EstadoConservacao>('Bom');
  protected readonly rachadura = signal<RachaduraInspecao>('Nenhuma');
  protected readonly ataque = signal<AtaqueBiologicoInspecao>('Nenhum');
  protected readonly umidade = signal<number | null>(null);
  protected readonly vidaUtil = signal<number | null>(null);
  protected readonly madeiraConfere = signal(true);
  protected readonly observacao = signal('');
  protected readonly foto = signal<File | null>(null);
  protected readonly resultado = signal<InspecaoDto | null>(null);
  protected readonly trocaPedida = signal(false);
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected buscar(): void {
    if (!this.tag().trim()) return;
    this.erro.set(null);
    this.mensagem.set(null);
    this.resultado.set(null);
    this.trocaPedida.set(false);
    if (this.semRede()) {
      const p = this.trabalho.pecaPorTag(this.tag());
      this.peca.set(p);
      if (p) this.iniciadoEm.set(new Date().toISOString());
      else this.erro.set('Sem rede: só abrem as peças do trabalho da noite (as das trocas e dos alertas).');
      return;
    }
    this.buscando.set(true);
    this.pecas.porTag(this.tag()).pipe(finalize(() => this.buscando.set(false))).subscribe({
      next: (p) => {
        this.peca.set(p);
        this.iniciadoEm.set(new Date().toISOString());
        if (this.aba() === 'historico') this.inspecoes.daPeca(p.id).subscribe((l) => this.feitas.set(l));
      },
      error: (e) => {
        this.peca.set(null);
        this.erro.set(e?.status === 404 ? 'Nenhuma peça com essa tag.' : mensagemDeErro(e));
      },
    });
  }

  protected escolherFoto(e: Event): void {
    this.foto.set((e.target as HTMLInputElement).files?.[0] ?? null);
  }

  /** Pela fila: com rede, sobe na hora e mostra o resultado; sem rede, fica guardada e o resultado sai quando ela subir. */
  protected async registrar(p: PecaDto): Promise<void> {
    if (!this.foto()) {
      this.erro.set('Tire a foto da peça.');
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    const r = await this.fila.registrar(
      {
        tipo: 'inspecao', alvo: p.id, rotulo: `Inspeção da peça ${p.codigo}`,
        corpo: {
          idDormente: p.id, idAlerta: this.alerta() ? Number(this.alerta()) : null, leitura: leitura(this.tag(), this.metodo()),
          iniciadoEm: this.iniciadoEm() ?? new Date().toISOString(), finalizadoEm: new Date().toISOString(),
          estadoConservacao: this.estado(), rachadura: this.rachadura(), ataqueBiologico: this.ataque(),
          umidadePct: this.umidade(), vidaUtilMeses: this.vidaUtil(), tipoMadeiraConfere: this.madeiraConfere(),
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
    if (r.estado === 'guardado') {
      this.mensagem.set('Sem rede: a inspeção ficou guardada no celular. O resultado aparece quando ela subir.');
      return;
    }
    const i = r.resposta as InspecaoDto;
    this.resultado.set(i);
    this.mensagem.set(i.resultado === 'Aprovada' ? 'Peça aprovada na inspeção.' : 'Peça reprovada: peça a troca abaixo.');
  }

  protected pedirTroca(p: PecaDto, i: InspecaoDto): void {
    this.enviando.set(true);
    this.trocas.pedir(p.id, i.id, i.motivosDeReprovacao.join('; ') || 'Reprovada na inspeção.', p.kmProjeto).subscribe({
      next: () => {
        this.enviando.set(false);
        this.trocaPedida.set(true);
        this.mensagem.set('Troca pedida. Agora ela espera a aprovação do supervisor.');
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }

  protected laudo(i: InspecaoDto): void {
    this.laudoDe.set(this.laudoDe() === i.id ? null : i.id);
  }
}