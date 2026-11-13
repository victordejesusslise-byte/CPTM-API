import { Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeituraDto, MetodoCaptura } from '../../../compartilhado/entidades/enums';
import { LeituraTag, leitura } from '../../../compartilhado/componentes/leitura-tag/leitura-tag';
import { km } from '../../../compartilhado/utilitarios/formato';
import { FORMULARIO } from '../../../compartilhado/ui';
import { Fila } from '../../../nucleo/fila/fila';
import { GUARDADO } from '../../../nucleo/fila/guardado';
import { LoteTrocaDto, ObrigatoriaDto, PecaDoLoteDto } from '../../entidades/campo';
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

/** Uma obrigatória que não deu para trocar, guardada no celular até fechar o lote (a foto fica guardada à parte). */
export interface NaoDeuNoRascunho {
  idTroca: number;
  codigo: string;
  kmProjeto: number | null;
  motivo: string;
  descricao: string | null;
  foto: string;
  registradoEm: string;
}

/** As obrigatórias ainda a trocar no lote (o lote guardado por um app antigo vem sem elas). */
export const obrigatoriasAbertas = (lote: LoteTrocaDto): ObrigatoriaDto[] => (lote.obrigatorias ?? []).filter((o) => o.situacao === 'Aberta');

/**
 * A conferência de um par no celular, a mesma do servidor (que confere de novo): a peça que sai está na via dentro da faixa do lote (pela
 * tag, ou escolhida pelo km quando a tag não responde), a que entra é uma das separadas, nenhuma se repete e não passa do aprovado mais
 * as obrigatórias (Victor, 2026-11-12). A peça separada para um avulso obrigatório só entra no lugar dele, e os últimos lugares são das
 * obrigatórias que faltam (as marcadas como "não deu", em naoDeu, liberam o lugar). Devolve o par ou o porquê não serve.
 */
export const conferirPar = (lote: LoteTrocaDto, feitos: Pick<ParNoRascunho, 'saiu' | 'entrou'>[], sai: { tag?: string; idDormente?: number | null },
  tagEntra: string, naoDeu: number[] = []): { saiu: PecaDoLoteDto; entrou: PecaDoLoteDto } | string => {
  const aprovadas = lote.quantidadeAprovada ?? 0;
  const abertas = obrigatoriasAbertas(lote);
  const limite = aprovadas + abertas.length;
  if (feitos.length >= limite)
    return abertas.length
      ? `O lote aprovou ${aprovadas} peças e tem ${abertas.length} obrigatórias: as ${limite} já foram trocadas. Feche o lote.`
      : `O lote aprovou ${aprovadas} peças: todas já foram trocadas. Feche o lote.`;
  const saiu = sai.tag !== undefined ? lote.pecasDaFaixa.find((p) => p.tag === sai.tag!.trim()) : lote.pecasDaFaixa.find((p) => p.id === sai.idDormente);
  if (!saiu) return 'A peça que sai não está na via dentro da faixa do lote. Confira se é o dormente certo.';
  if (feitos.some((f) => f.saiu.id === saiu.id)) return `A peça ${saiu.codigo} já saiu neste lote.`;
  const daPeca = abertas.find((o) => o.peca.id === saiu.id);
  if (daPeca && naoDeu.includes(daPeca.idTroca)) return `Você marcou que não deu a peça ${saiu.codigo}: desfaça o "não deu" para trocar.`;
  const pendentes = abertas.filter((o) => !naoDeu.includes(o.idTroca) && !feitos.some((f) => f.saiu.id === o.peca.id));
  if (!daPeca && limite - feitos.length <= pendentes.length)
    return `Os lugares que sobram são das obrigatórias: ${pendentes.map((o) => `${o.peca.codigo} (km ${km(o.peca.kmProjeto)})`).join(', ')}.`;
  const doAvulso = abertas.find((o) => o.separada?.tag === tagEntra.trim());
  if (doAvulso && doAvulso.peca.id !== saiu.id)
    return `A peça ${doAvulso.separada!.codigo} foi separada para a troca da peça ${doAvulso.peca.codigo} (km ${km(doAvulso.peca.kmProjeto)}) e só entra no lugar dela.`;
  const entrou = doAvulso?.separada ?? lote.separadas.find((p) => p.tag === tagEntra.trim());
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
    .conferida { margin: -10px 0 6px; color: var(--nota-bom-texto); }
    .lbl { margin: 12px 0 6px; }
    .obrig { display: flex; justify-content: space-between; align-items: center; gap: 8px; padding: 4px 0; }
    .obrig .feita { color: var(--nota-bom-texto); white-space: nowrap; }
    .obrig > button, .obrig > .nota { flex: none; white-space: nowrap; }
    .obrig > span:first-child { min-width: 0; }
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
        @if (obrigatorias().length) { <span class="t">Obrigatórias<b>{{ obrigatorias().length }}</b></span> }
        <span class="t">Trocadas<b>{{ rascunho().length }}</b></span>
        @if (modo() === 'fechar') { <span class="t">Voltam ao armazém<b>{{ sobram() }}</b></span> } @else { <span class="t">Faltam<b>{{ limite() - rascunho().length }}</b></span> }
      </div>

      @switch (modo()) {
        @case ('trocar') {
          @if (obrigatorias().length) {
            <div class="chamada chamada--neutra">
              <b>Obrigatórias: trocas já aprovadas nesta faixa</b>
              @for (o of obrigatorias(); track o.idTroca) {
                <div class="obrig">
                  <span><span class="mono">km {{ km(o.peca.kmProjeto) }}</span> · {{ o.peca.codigo }}@if (o.separada) { <br /><span class="nota">peça separada: {{ o.separada.codigo }}</span> }</span>
                  @switch (situacao(o)) {
                    @case ('trocada') { <span class="feita">✓ trocada</span> }
                    @case ('nao-deu') { <span class="nota">não deu <button mat-button type="button" (click)="desfazerNaoDeu(o.idTroca)">Desfazer</button></span> }
                    @default { <button mat-stroked-button type="button" (click)="abrirNaoDeu(o)">Não deu</button> }
                  }
                </div>
              }
            </div>
          }
          @if (rascunho().length < limite()) {
            <p class="lbl">Dormente {{ rascunho().length + 1 }} de {{ limite() }}</p>
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
            <p class="aviso aviso--ok">Todas as {{ limite() }} peças do lote foram trocadas. Feche o lote.</p>
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
            @for (n of naoDeu(); track n.idTroca) {
              <div class="cartao-alerta"><span class="topo-alerta"><b class="mono">{{ n.codigo }}</b><span class="nota">não deu</span></span>
                <span class="nota"><span class="mono">km {{ km(n.kmProjeto) }}</span> · {{ n.motivo }} · foto ✓</span></div>
            }
          </div>
          <mat-form-field appearance="outline"><mat-label>Equipe</mat-label><input matInput [ngModel]="equipe()" (ngModelChange)="equipe.set($event)" /></mat-form-field>
          <div class="acoes">
            <button mat-flat-button type="button" (click)="confirmar()" [disabled]="enviando() || !rascunho().length" [appCarregando]="enviando()">Confirmar o lote · {{ rascunho().length }} trocas</button>
            <button mat-stroked-button type="button" (click)="modo.set('trocar')">Voltar</button>
          </div>
          <p class="nota">Sem rede, o lote fica guardado no celular e vai sozinho quando a rede voltar.</p>
        }
        @case ('nao-deu') {
          @let o = obrigatoriaEscolhida();
          <p class="lbl">Não deu para trocar {{ o?.peca?.codigo }} · <span class="mono">km {{ km(o?.peca?.kmProjeto ?? null) }}</span></p>
          <mat-form-field appearance="outline"><mat-label>Motivo</mat-label><input matInput [ngModel]="motivo()" (ngModelChange)="motivo.set($event)" placeholder="Ex.: dormente preso no AMV" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Descrição (opcional)</mat-label><textarea matInput rows="2" [ngModel]="descricao()" (ngModelChange)="descricao.set($event)"></textarea></mat-form-field>
          <label class="btn-foto"><input type="file" accept="image/*" capture="environment" (change)="escolherFoto($event)" /> {{ foto() ? 'Foto: ' + foto()!.name : 'Tirar a foto' }}</label>
          <div class="acoes">
            <button mat-flat-button type="button" (click)="guardarNaoDeu()" [disabled]="guardando()">Guardar no lote</button>
            <button mat-stroked-button type="button" (click)="modo.set('trocar')">Voltar</button>
          </div>
          <p class="nota">Ao fechar o lote, a troca deste dormente volta para o técnico como recusa, com o motivo e a foto.</p>
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

  readonly modo = signal<'trocar' | 'fechar' | 'recusar' | 'nao-deu'>('trocar');
  readonly rascunho = signal<ParNoRascunho[]>([]);
  /** As obrigatórias que não deu para trocar, com motivo e foto (guardadas no celular até fechar o lote). */
  readonly naoDeu = signal<NaoDeuNoRascunho[]>([]);
  readonly obrigatoriaEscolhida = signal<ObrigatoriaDto | null>(null);
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
  /** As trocas avulsas já aprovadas na faixa: somam à quantidade aprovada (Victor, 2026-11-12). */
  protected readonly obrigatorias = computed(() => obrigatoriasAbertas(this.lote()));
  protected readonly limite = computed(() => this.aprovadas() + this.obrigatorias().length);
  /** As separadas do lote que não entraram (a peça do avulso que entrou no lugar dele não é do lote). */
  protected readonly sobram = computed(() => this.lote().separadas.filter((s) => !this.rascunho().some((p) => p.entrou.id === s.id)).length);
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

  protected situacao(o: ObrigatoriaDto): 'trocada' | 'nao-deu' | 'a-trocar' {
    if (this.rascunho().some((p) => p.saiu.id === o.peca.id)) return 'trocada';
    return this.naoDeu().some((n) => n.idTroca === o.idTroca) ? 'nao-deu' : 'a-trocar';
  }

  abrirNaoDeu(o: ObrigatoriaDto): void {
    this.obrigatoriaEscolhida.set(o);
    this.motivo.set('');
    this.descricao.set('');
    this.foto.set(null);
    this.erro.set(null);
    this.ok.set(null);
    this.modo.set('nao-deu');
  }

  /** Guarda no celular que não deu para trocar a obrigatória escolhida: motivo e foto. */
  async guardarNaoDeu(): Promise<void> {
    const o = this.obrigatoriaEscolhida();
    if (!o) return;
    if (!this.motivo().trim() || !this.foto()) {
      this.erro.set('Escreva o motivo e tire a foto.');
      return;
    }
    this.guardando.set(true);
    const nome = `${this.chave()}.nao-deu.${o.idTroca}.jpg`;
    await this.guardado.gravarFoto(nome, this.foto()!);
    this.naoDeu.update((l) => [...l.filter((n) => n.idTroca !== o.idTroca), {
      idTroca: o.idTroca, codigo: o.peca.codigo, kmProjeto: o.peca.kmProjeto, motivo: this.motivo().trim(), descricao: this.descricao().trim() || null,
      foto: nome, registradoEm: new Date().toISOString(),
    }]);
    await this.salvar();
    this.guardando.set(false);
    this.erro.set(null);
    this.foto.set(null);
    this.modo.set('trocar');
  }

  protected async desfazerNaoDeu(idTroca: number): Promise<void> {
    const fora = this.naoDeu().find((n) => n.idTroca === idTroca);
    this.naoDeu.update((l) => l.filter((n) => n.idTroca !== idTroca));
    if (fora) await this.guardado.apagarFoto(fora.foto);
    await this.salvar();
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
    const par = conferirPar(this.lote(), this.rascunho(), sai, this.tagEntra(), this.naoDeu().map((n) => n.idTroca));
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
    this.ok.set(`${par.saiu.codigo} → ${par.entrou.codigo} guardado. ${this.limite() - this.rascunho().length} faltam.`);
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
    const naoDeu = this.naoDeu();
    // As obrigatórias: trocadas no lote ou com o "não deu" (o servidor confere de novo).
    const faltam = this.obrigatorias().filter((o) => this.situacao(o) === 'a-trocar');
    if (faltam.length) {
      this.erro.set(`Falta${faltam.length > 1 ? 'm' : ''} ${faltam.map((o) => `a peça ${o.peca.codigo} (km ${km(o.peca.kmProjeto)})`).join(', ')}, com troca já aprovada: troque no lote ou marque que não deu.`);
      return;
    }
    const fotos: Blob[] = [];
    for (const p of pares) {
      const f = p.foto ? await this.guardado.lerFoto(p.foto) : null;
      if (!f) {
        this.erro.set(`A foto de ${p.saiu.codigo} se perdeu no celular: tire de novo esse dormente.`);
        return;
      }
      fotos.push(f);
    }
    // As fotos do "não deu" vão depois das dos pares, na ordem (a fila põe cada uma no seu lugar).
    for (const n of naoDeu) {
      const f = await this.guardado.lerFoto(n.foto);
      if (!f) {
        this.erro.set(`A foto do "não deu" de ${n.codigo} se perdeu no celular: marque de novo.`);
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
        ...(naoDeu.length ? { naoDeu: naoDeu.map((n) => ({ idTroca: n.idTroca, motivo: n.motivo, descricao: n.descricao, registradoEm: n.registradoEm })) } : {}),
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
    for (const n of this.naoDeu()) await this.guardado.apagarFoto(n.foto);
    this.rascunho.set([]);
    this.naoDeu.set([]);
    await this.salvar();
    this.modo.set('trocar');
    this.mudou.emit(r.estado === 'enviado' ? ok : semRede);
  }

  private async carregar(chave: string): Promise<void> {
    try {
      const salvo = await this.guardado.ler(chave);
      this.rascunho.set(salvo ? (JSON.parse(salvo) as ParNoRascunho[]) : []);
      const naoDeu = await this.guardado.ler(`${chave}.nao-deu`);
      this.naoDeu.set(naoDeu ? (JSON.parse(naoDeu) as NaoDeuNoRascunho[]) : []);
    } catch {
      this.rascunho.set([]);
      this.naoDeu.set([]);
    }
  }

  private async salvar(): Promise<void> {
    await this.guardado.gravar(this.chave(), JSON.stringify(this.rascunho()));
    await this.guardado.gravar(`${this.chave()}.nao-deu`, JSON.stringify(this.naoDeu()));
  }
}
