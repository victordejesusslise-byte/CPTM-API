import { Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeituraDto, MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { km } from '../../../compartilhado/utilitarios/formato';
import { FORMULARIO } from '../../../compartilhado/ui';
import { Fila } from '../../../nucleo/fila/fila';
import { GUARDADO } from '../../../nucleo/fila/guardado';
import { LoteTrocaDto, PecaDoLoteDto } from '../../entidades/campo';
import { ROTULO_LOTE } from './lote-aberto';

/** Um dormente já trocado no lote, guardado no celular até fechar o lote (a foto fica guardada à parte). */
export interface ParNoRascunho {
  saiu: PecaDoLoteDto;
  entrou: PecaDoLoteDto;
  tagSaiuNaoResponde?: boolean;
  leituraSaiu?: LeituraDto | null;
  leituraEntrou?: LeituraDto;
  foto?: string;
  registradoEm?: string;
}

/**
 * A conferência de um par no celular, a mesma do servidor (que confere de novo): a peça que sai está na via dentro da faixa do lote (pela
 * tag, ou escolhida pelo km quando a tag não responde), a que entra é uma das separadas, nenhuma se repete e não passa do aprovado.
 * Devolve o par ou o porquê não serve.
 */
export const conferirPar = (lote: LoteTrocaDto, feitos: Pick<ParNoRascunho, 'saiu' | 'entrou'>[], sai: { tag?: string; idDormente?: number | null },
  tagEntra: string): { saiu: PecaDoLoteDto; entrou: PecaDoLoteDto } | string => {
  const aprovadas = lote.quantidadeAprovada ?? 0;
  if (feitos.length >= aprovadas) return `O lote aprovou ${aprovadas} peças: todas já foram trocadas. Feche o lote.`;
  const saiu = sai.tag !== undefined ? lote.pecasDaFaixa.find((p) => p.tag === sai.tag!.trim()) : lote.pecasDaFaixa.find((p) => p.id === sai.idDormente);
  if (!saiu) return 'A peça que sai não está na via dentro da faixa do lote. Confira se é o dormente certo.';
  if (feitos.some((f) => f.saiu.id === saiu.id)) return `A peça ${saiu.codigo} já saiu neste lote.`;
  const entrou = lote.separadas.find((p) => p.tag === tagEntra.trim());
  if (!entrou) return 'A peça que entra não é uma das separadas para o lote.';
  if (feitos.some((f) => f.entrou.id === entrou.id)) return `A peça ${entrou.codigo} já entrou neste lote.`;
  return { saiu, entrou };
};

/**
 * Trocas do trecho, no lote de troca (Victor, 2026-11-11): a via troca um dormente por vez (a tag da peça que sai, a da que entra e uma foto),
 * e no fim confirma o lote, conferido ID por ID. O que já foi trocado fica guardado no celular até fechar; o lote sobe pela fila (sem rede,
 * sobe sozinho depois). Sem conseguir trocar nada, recusa o lote com motivo e foto.
 */
@Component({
  selector: 'app-trocar-lote',
  imports: [FormsModule, FORMULARIO, LeituraTag],
  styles: `
    .conferida { margin: -10px 0 6px; color: var(--nota-bom); }
    .lbl { margin: 12px 0 6px; }
  `,
  template: `
    @let l = lote();
    <section class="cartao pilha">
      <header>
        <h2>Lote {{ l.codigo }}</h2><span class="pilula">{{ rotulo[l.status] }}</span>
        <span class="nota">{{ l.trecho }} <button mat-button type="button" (click)="fechar.emit()">Fechar</button></span>
      </header>
      @if (erro()) { <p class="aviso aviso--erro" role="alert">{{ erro() }}</p> }
      @if (ok() && modo() === 'trocar') { <p class="aviso aviso--ok" role="status">{{ ok() }}</p> }
      <div class="totais">
        <span class="t">Aprovadas<b>{{ aprovadas() }}</b></span>
        <span class="t">Trocadas<b>{{ rascunho().length }}</b></span>
        @if (modo() === 'fechar') { <span class="t">Voltam ao armazém<b>{{ sobram() }}</b></span> } @else { <span class="t">Faltam<b>{{ aprovadas() - rascunho().length }}</b></span> }
      </div>

      @switch (modo()) {
        @case ('trocar') {
          @if (rascunho().length < aprovadas()) {
            <p class="lbl">Dormente {{ rascunho().length + 1 }} de {{ aprovadas() }}</p>
            @if (!saiNaoResponde()) {
              <app-leitura-tag rotulo="Tag da peça que sai" [compacta]="true" [(tag)]="tagSai" [(metodo)]="metodoSai" />
              @if (saiLida(); as s) { <p class="nota conferida">✓ {{ s.codigo }} · está na via, <span class="mono">km {{ km(s.kmProjeto) }}</span>, na faixa do lote</p> }
            } @else {
              <mat-form-field appearance="outline"><mat-label>Peça que sai (pelo km)</mat-label>
                <mat-select [value]="idSaiEscolhida()" (selectionChange)="idSaiEscolhida.set($event.value)"><app-busca-opcoes />
                  @for (p of l.pecasDaFaixa; track p.id) { <mat-option [value]="p.id">km {{ km(p.kmProjeto) }} · {{ p.codigo }}</mat-option> }
                </mat-select></mat-form-field>
            }
            <mat-checkbox [checked]="saiNaoResponde()" (change)="saiNaoResponde.set($event.checked)">A tag da peça que sai não responde</mat-checkbox>
            <app-leitura-tag rotulo="Tag da peça que entra" [compacta]="true" [(tag)]="tagEntra" [(metodo)]="metodoEntra" />
            <div class="acoes">
              <label class="btn-foto"><input type="file" accept="image/*" capture="environment" (change)="escolherFoto($event)" /> {{ foto() ? 'Foto: ' + foto()!.name : 'Tirar a foto deste dormente' }}</label>
            </div>
            <div class="acoes"><button mat-flat-button type="button" (click)="guardar()" [disabled]="guardando()">Guardar e ir para o próximo</button></div>
          } @else {
            <p class="aviso aviso--ok">Todas as {{ aprovadas() }} peças aprovadas foram trocadas. Feche o lote.</p>
          }
          <div class="acoes">
            <button mat-stroked-button type="button" (click)="modo.set('fechar')">Fechar o lote · {{ rascunho().length }} trocadas</button>
            <button mat-stroked-button class="perigo" type="button" (click)="modo.set('recusar')">Recusar o lote</button>
          </div>
        }
        @case ('fechar') {
          <p class="lbl">Conferido ID por ID</p>
          <div class="cartoes-alerta">
            @for (p of rascunho(); track p.saiu.id; let i = $index) {
              <div class="cartao-alerta"><span class="topo-alerta"><b class="mono">{{ p.saiu.codigo }} → {{ p.entrou.codigo }}</b><button mat-button type="button" (click)="tirar(i)">Tirar</button></span>
                <span class="nota"><span class="mono">km {{ km(p.saiu.kmProjeto) }}</span> · foto ✓@if (p.tagSaiuNaoResponde) { · tag da que sai não respondeu }</span></div>
            } @empty { <p class="nota">Nenhum dormente trocado ainda.</p> }
          </div>
          <mat-form-field appearance="outline"><mat-label>Equipe</mat-label><input matInput [ngModel]="equipe()" (ngModelChange)="equipe.set($event)" /></mat-form-field>
          <div class="acoes">
            <button mat-flat-button type="button" (click)="confirmar()" [disabled]="enviando() || !rascunho().length" [appCarregando]="enviando()">Confirmar o lote · {{ rascunho().length }} trocas</button>
            <button mat-stroked-button type="button" (click)="modo.set('trocar')">Voltar</button>
          </div>
          <p class="nota">Sem rede, o lote fica guardado no celular e vai sozinho quando a rede voltar.</p>
        }
        @case ('recusar') {
          <p class="lbl">Recusar o lote (não deu para trocar nada)</p>
          <mat-form-field appearance="outline"><mat-label>Motivo</mat-label><input matInput [ngModel]="motivo()" (ngModelChange)="motivo.set($event)" placeholder="Ex.: a interdição foi cancelada" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Descrição (opcional)</mat-label><textarea matInput rows="2" [ngModel]="descricao()" (ngModelChange)="descricao.set($event)"></textarea></mat-form-field>
          <label class="btn-foto"><input type="file" accept="image/*" capture="environment" (change)="escolherFoto($event)" /> {{ foto() ? 'Foto: ' + foto()!.name : 'Tirar a foto' }}</label>
          <div class="acoes">
            <button mat-flat-button class="perigo" type="button" (click)="recusar()" [disabled]="enviando()" [appCarregando]="enviando()">Enviar a recusa ao técnico</button>
            <button mat-stroked-button type="button" (click)="modo.set('trocar')">Voltar</button>
          </div>
        }
      }
    </section>
  `,
})
export class TrocarLote {
  readonly lote = input.required<LoteTrocaDto>();
  /** O lote foi fechado ou recusado (enviado ou guardado na fila): a mensagem para a tela. */
  readonly mudou = output<string>();
  readonly fechar = output<void>();
  private readonly fila = inject(Fila);
  private readonly guardado = inject(GUARDADO);
  protected readonly rotulo = ROTULO_LOTE;
  protected readonly km = km;

  readonly modo = signal<'trocar' | 'fechar' | 'recusar'>('trocar');
  readonly rascunho = signal<ParNoRascunho[]>([]);
  readonly tagSai = signal('');
  readonly metodoSai = signal<MetodoCaptura>('ModuloUhf');
  readonly saiNaoResponde = signal(false);
  readonly idSaiEscolhida = signal<number | null>(null);
  readonly tagEntra = signal('');
  readonly metodoEntra = signal<MetodoCaptura>('ModuloUhf');
  readonly foto = signal<File | null>(null);
  readonly equipe = signal('');
  readonly motivo = signal('');
  readonly descricao = signal('');
  protected readonly erro = signal<string | null>(null);
  protected readonly ok = signal<string | null>(null);
  protected readonly enviando = signal(false);
  protected readonly guardando = signal(false);

  protected readonly aprovadas = computed(() => this.lote().quantidadeAprovada ?? 0);
  protected readonly sobram = computed(() => this.lote().separadas.length - this.rascunho().length);
  /** A peça da tag lida, se está na faixa do lote (o aviso verde na tela, antes de guardar). */
  protected readonly saiLida = computed(() => (this.tagSai().trim() ? this.lote().pecasDaFaixa.find((p) => p.tag === this.tagSai().trim()) ?? null : null));
  private readonly chave = computed(() => `ybyra.lote.${this.lote().id}`);

  constructor() {
    // O que já foi trocado neste lote volta do celular (o app pode ter fechado no meio da noite).
    effect(() => {
      const chave = this.chave();
      untracked(() => void this.carregar(chave));
    });
  }

  protected escolherFoto(e: Event): void {
    this.foto.set((e.target as HTMLInputElement).files?.[0] ?? null);
  }

  /** Confere o dormente e guarda no celular (com a foto); limpa os campos para o próximo. */
  async guardar(): Promise<void> {
    this.ok.set(null);
    const sai = this.saiNaoResponde() ? { idDormente: this.idSaiEscolhida() } : { tag: this.tagSai() };
    if ((!this.saiNaoResponde() && !this.tagSai().trim()) || (this.saiNaoResponde() && !this.idSaiEscolhida()) || !this.tagEntra().trim()) {
      this.erro.set('Leia a tag da peça que sai (ou, se ela não responde, escolha pelo km) e a da peça que entra.');
      return;
    }
    if (!this.foto()) {
      this.erro.set('Tire a foto deste dormente: uma foto por dormente.');
      return;
    }
    const par = conferirPar(this.lote(), this.rascunho(), sai, this.tagEntra());
    if (typeof par === 'string') {
      this.erro.set(par);
      return;
    }
    this.guardando.set(true);
    const nome = `${this.chave()}.${par.saiu.id}.jpg`;
    await this.guardado.gravarFoto(nome, this.foto()!);
    this.rascunho.update((l) => [...l, {
      ...par, foto: nome, registradoEm: new Date().toISOString(), tagSaiuNaoResponde: this.saiNaoResponde(),
      leituraSaiu: this.saiNaoResponde() ? null : leitura(this.tagSai(), this.metodoSai()), leituraEntrou: leitura(this.tagEntra(), this.metodoEntra()),
    }]);
    await this.salvar();
    this.guardando.set(false);
    this.erro.set(null);
    this.ok.set(`${par.saiu.codigo} → ${par.entrou.codigo} guardado. ${this.aprovadas() - this.rascunho().length} faltam.`);
    this.tagSai.set('');
    this.tagEntra.set('');
    this.idSaiEscolhida.set(null);
    this.saiNaoResponde.set(false);
    this.foto.set(null);
  }

  protected async tirar(i: number): Promise<void> {
    const [fora] = this.rascunho().slice(i, i + 1);
    this.rascunho.update((l) => l.filter((_, j) => j !== i));
    if (fora?.foto) await this.guardado.apagarFoto(fora.foto);
    await this.salvar();
  }

  /** Manda o lote pela fila: os pares (conferidos de novo no servidor), a equipe e uma foto por par. */
  async confirmar(): Promise<void> {
    if (!this.equipe().trim()) {
      this.erro.set('Informe a equipe para fechar o lote.');
      return;
    }
    const pares = this.rascunho();
    const fotos: Blob[] = [];
    for (const p of pares) {
      const f = p.foto ? await this.guardado.lerFoto(p.foto) : null;
      if (!f) {
        this.erro.set(`A foto de ${p.saiu.codigo} se perdeu no celular: tire de novo esse dormente.`);
        return;
      }
      fotos.push(f);
    }
    await this.enviar({
      tipo: 'lote', alvo: this.lote().id, rotulo: `Lote ${this.lote().codigo}`,
      corpo: {
        equipe: this.equipe().trim(),
        pares: pares.map((p) => ({
          leituraSaiu: p.leituraSaiu ?? null, tagSaiuNaoResponde: !!p.tagSaiuNaoResponde, idDormenteSaiu: p.tagSaiuNaoResponde ? p.saiu.id : null,
          leituraEntrou: p.leituraEntrou, km: p.saiu.kmProjeto, registradoEm: p.registradoEm,
        })),
      },
    }, fotos, `Lote ${this.lote().codigo} fechado: ${pares.length} trocas.`, 'Sem rede: o lote ficou guardado no celular e sobe sozinho quando a rede voltar.');
  }

  protected async recusar(): Promise<void> {
    if (!this.motivo().trim() || !this.foto()) {
      this.erro.set('Escreva o motivo e tire a foto.');
      return;
    }
    await this.enviar({
      tipo: 'recusa-lote', alvo: this.lote().id, rotulo: `Recusa do lote ${this.lote().codigo}`,
      corpo: { motivo: this.motivo().trim(), descricao: this.descricao().trim() || null },
    }, this.foto()!, 'Recusa do lote enviada ao técnico.', 'Sem rede: a recusa ficou guardada no celular e sobe sozinha quando a rede voltar.');
  }

  private async enviar(registro: Parameters<Fila['registrar']>[0], fotos: Blob | Blob[], ok: string, semRede: string): Promise<void> {
    this.enviando.set(true);
    this.erro.set(null);
    const r = await this.fila.registrar(registro, fotos);
    this.enviando.set(false);
    if (r.estado === 'recusado') {
      this.erro.set(r.motivo);
      return;
    }
    // Enviado ou na fila: o rascunho sai do celular (a fila guardou as próprias cópias das fotos).
    for (const p of this.rascunho()) if (p.foto) await this.guardado.apagarFoto(p.foto);
    this.rascunho.set([]);
    await this.salvar();
    this.modo.set('trocar');
    this.mudou.emit(r.estado === 'enviado' ? ok : semRede);
  }

  private async carregar(chave: string): Promise<void> {
    try {
      const salvo = await this.guardado.ler(chave);
      this.rascunho.set(salvo ? (JSON.parse(salvo) as ParNoRascunho[]) : []);
    } catch {
      this.rascunho.set([]);
    }
  }

  private async salvar(): Promise<void> {
    await this.guardado.gravar(this.chave(), JSON.stringify(this.rascunho()));
  }
}
