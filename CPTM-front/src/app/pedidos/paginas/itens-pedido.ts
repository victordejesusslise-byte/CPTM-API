import { Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormGroup, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Subscription, catchError, debounceTime, of, startWith, switchMap } from 'rxjs';
import { m3, metros } from '../../compartilhado/utilitarios/formato';
import { NovoPedidoItemDto, VerificacaoPedidoDto } from '../entidades/pedido';
import { MedidaDto } from '../../catalogos/entidades/cadastro';
import { Cadastros } from '../../catalogos/servicos/cadastros';
import { Catalogos } from '../../catalogos/servicos/catalogos';
import { Pedidos } from '../servicos/pedidos';
import { FORMULARIO } from '../../compartilhado/ui';
import { ItemParaTabela } from './tabela-itens';

export type FormItem = FormGroup<{
  idTamanho: import('@angular/forms').FormControl<number>;
  classe: import('@angular/forms').FormControl<number>;
  idEspecie: import('@angular/forms').FormControl<number | null>;
  quantidade: import('@angular/forms').FormControl<number>;
  idMedida: import('@angular/forms').FormControl<number>;
}>;

export const novoItemPedido = (fb: NonNullableFormBuilder, i: Partial<NovoPedidoItemDto> = {}): FormItem =>
  fb.group({
    idTamanho: [i.idTamanho ?? 0],
    classe: [i.classe ?? 1],
    idEspecie: [(i.idEspecie ?? null) as number | null],
    quantidade: [i.quantidade ?? 20],
    idMedida: [i.idMedida ?? 0],
  });

export const itensValidos = (itens: FormArray<FormItem>): NovoPedidoItemDto[] =>
  itens.getRawValue().filter((i) => i.idTamanho > 0 && i.quantidade > 0 && i.idMedida > 0);

/** O padrão serve à espécie do item: é dela ou de todas. */
const serve = (m: MedidaDto, idEspecie: number | null) => m.idEspecie === null || m.idEspecie === idEspecie;

/** Os padrões que o item pode escolher: os ativos do fornecedor que servem à espécie, e o que ele já usa (mesmo desativado). */
export const padroesDoItem = (medidas: MedidaDto[], idEspecie: number | null, idAtual = 0): MedidaDto[] =>
  medidas.filter((m) => m.id === idAtual || (m.ativo && serve(m, idEspecie)));

/** "0,24 × 0,17 m · todas as espécies". */
export const rotuloPadrao = (m: MedidaDto): string => `${m.rotulo} · ${m.especie ?? 'todas as espécies'}`;

/** O m³ de uma peça: comprimento × largura × altura do padrão (5 casas, como na API). */
export const m3DaPeca = (comprimentoM: number, medida: Pick<MedidaDto, 'larguraM' | 'alturaM'> | undefined): number =>
  medida ? Math.round(comprimentoM * medida.larguraM * medida.alturaM * 1e5) / 1e5 : 0;

/**
 * Os itens do pedido: comprimento, classe, espécie (opcional) e o padrão de medida do fornecedor (largura × altura), o total e a regra
 * da 2ª classe conferida pela API a cada mudança. Serve para o pedido novo e para a correção. Se só um padrão serve ao item, ele vem marcado.
 */
@Component({
  selector: 'app-itens-pedido',
  imports: [ReactiveFormsModule, FORMULARIO],
  templateUrl: './itens-pedido.html',
})
export class ItensPedido {
  readonly itens = input.required<FormArray<FormItem>>();
  readonly idEmpresa = input<number | null>(null);
  readonly verificado = output<VerificacaoPedidoDto | null>();

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly pedidos = inject(Pedidos);
  private readonly catalogos = inject(Catalogos);
  private readonly fornecedores = toSignal(inject(Cadastros).empresas('Fornecedor'), { initialValue: [] });
  protected readonly tamanhos = toSignal(this.catalogos.tamanhos(), { initialValue: [] });
  protected readonly especies = toSignal(this.catalogos.especies(), { initialValue: [] });
  /** Os padrões de medida do fornecedor escolhido. */
  protected readonly medidas = computed(() => this.fornecedores().find((f) => f.id === this.idEmpresa())?.medidas ?? []);
  protected readonly metros = metros;
  protected readonly m3 = m3;
  protected readonly rotulo = rotuloPadrao;
  protected readonly versao = signal(0);
  protected readonly verificacao = signal<VerificacaoPedidoDto | null>(null);

  protected readonly pecas = computed(() => (this.versao(), this.itens().getRawValue().reduce((a, i) => a + (Number(i.quantidade) || 0), 0)));
  protected readonly volume = computed(() => {
    this.versao();
    const medidas = this.medidas();
    return this.itens().getRawValue().reduce((a, i) => {
      const comprimento = this.tamanhos().find((t) => t.id === i.idTamanho)?.comprimentoM ?? 0;
      return a + m3DaPeca(comprimento, medidas.find((m) => m.id === i.idMedida)) * (Number(i.quantidade) || 0);
    }, 0);
  });

  constructor() {
    let inscricao: Subscription | undefined;
    const destroy = inject(DestroyRef);
    effect(() => {
      const itens = this.itens();
      const idEmpresa = this.idEmpresa();
      this.ajustarPadroes(itens);
      inscricao?.unsubscribe();
      inscricao = itens.valueChanges
        .pipe(
          startWith(null),
          debounceTime(350),
          switchMap(() => {
            this.ajustarPadroes(itens);
            this.versao.update((v) => v + 1);
            const validos = itensValidos(itens);
            return validos.length ? this.pedidos.verificar(idEmpresa, validos).pipe(catchError(() => of(null))) : of(null);
          }),
          takeUntilDestroyed(destroy),
        )
        .subscribe((v) => {
          this.verificacao.set(v);
          this.verificado.emit(v);
        });
    });
  }

  /** Os padrões que o item pode escolher agora. */
  protected padroes(item: FormItem): MedidaDto[] {
    this.versao();
    return padroesDoItem(this.medidas(), item.controls.idEspecie.value, item.controls.idMedida.value);
  }

  /**
   * Padrão de outro fornecedor ou de outra espécie sai; se só um padrão serve ao item, ele fica marcado. Com os padrões ainda
   * carregando, não mexe (a correção já vem com o padrão de cada item).
   */
  private ajustarPadroes(itens: FormArray<FormItem>): void {
    const medidas = this.medidas();
    if (!medidas.length) return;
    for (const item of itens.controls) {
      const especie = item.controls.idEspecie.value;
      const atual = medidas.find((m) => m.id === item.controls.idMedida.value);
      if (atual && serve(atual, especie)) continue;
      const servem = medidas.filter((m) => m.ativo && serve(m, especie));
      item.controls.idMedida.setValue(servem.length === 1 ? servem[0].id : 0, { emitEvent: false });
    }
  }

  protected adicionar(): void {
    this.itens().push(novoItemPedido(this.fb));
  }

  protected tirar(i: number): void {
    if (this.itens().length > 1) this.itens().removeAt(i);
  }
}

/** Os itens do formulário no formato da tabela de revisão (com o nome da espécie, o padrão e o m³ dele). */
export const paraTabela = (
  itens: NovoPedidoItemDto[],
  tamanhos: { id: number; comprimentoM: number }[],
  especies: { id: number; nomePopular: string | null; nomeCientifico: string }[],
  medidas: MedidaDto[],
): ItemParaTabela[] =>
  itens.map((i) => {
    const t = tamanhos.find((x) => x.id === i.idTamanho);
    const e = especies.find((x) => x.id === i.idEspecie);
    const m = medidas.find((x) => x.id === i.idMedida);
    return {
      comprimentoM: t?.comprimentoM ?? 0, classe: i.classe, especie: e ? (e.nomePopular ?? e.nomeCientifico) : null,
      medida: m?.rotulo ?? null, quantidade: i.quantidade, volumeM3: m3DaPeca(t?.comprimentoM ?? 0, m) * i.quantidade,
    };
  });
