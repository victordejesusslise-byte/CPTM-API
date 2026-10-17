import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { abrirArquivo, novoIdCliente } from '../../../compartilhado/utilitarios/arquivo';
import { nomeDaFoto } from '../../../compartilhado/utilitarios/foto';
import { dataHora, km, mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { AtaqueBiologicoInspecao, EstadoConservacao, MetodoCaptura, RachaduraInspecao } from '../../../compartilhado/entidades/enums';
import { PecaDto } from '../../../pecas/entidades/peca';
import { Nota } from '../../../compartilhado/componentes/nota/nota';
import { InspecaoDto, Inspecoes } from '../../servicos/inspecoes';
import { Laudos } from '../../../laudos/servicos/laudos';
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
  imports: [FormsModule, FORMULARIO, LeituraTag, Nota],
  templateUrl: './inspecao.html',
})
export class Inspecao {
  readonly aba = input<'checklist' | 'historico'>('checklist');
  readonly alerta = input<string>();
  private readonly pecas = inject(Pecas);
  private readonly inspecoes = inject(Inspecoes);
  private readonly trocas = inject(Trocas);
  private readonly laudos = inject(Laudos);
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
    this.pecas.porTag(this.tag()).subscribe({
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

  protected registrar(p: PecaDto): void {
    if (!this.foto()) {
      this.erro.set('Tire a foto da peça.');
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    this.inspecoes
      .registrar({
        idDormente: p.id, idAlerta: this.alerta() ? Number(this.alerta()) : null, leitura: leitura(this.tag(), this.metodo()),
        iniciadoEm: this.iniciadoEm() ?? new Date().toISOString(), finalizadoEm: new Date().toISOString(),
        estadoConservacao: this.estado(), rachadura: this.rachadura(), ataqueBiologico: this.ataque(),
        umidadePct: this.umidade(), vidaUtilMeses: this.vidaUtil(), tipoMadeiraConfere: this.madeiraConfere(),
        observacao: this.observacao().trim() || null, fotoArquivo: nomeDaFoto(this.foto(), `inspecao-${p.id}`), idCliente: novoIdCliente(),
      })
      .subscribe({
        next: (i) => {
          this.enviando.set(false);
          this.resultado.set(i);
          this.mensagem.set(i.resultado === 'Aprovada' ? 'Peça aprovada na inspeção.' : 'Peça reprovada: peça a troca abaixo.');
        },
        error: (e) => {
          this.enviando.set(false);
          this.erro.set(mensagemDeErro(e));
        },
      });
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
    this.laudos.pdf('inspecao', i.id).subscribe({ next: (pdf) => abrirArquivo(pdf, `LI-${i.id}.pdf`), error: (e) => this.erro.set(mensagemDeErro(e)) });
  }
}