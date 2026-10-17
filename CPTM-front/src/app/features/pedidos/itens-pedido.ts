import { Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormGroup, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Subscription, catchError, debounceTime, of, startWith, switchMap } from 'rxjs';
import { m3, metros } from '../../helpers/formato';
import { NovoPedidoItemDto, VerificacaoPedidoDto } from '../../models/pedido';
import { Catalogos } from '../../services/catalogos';
import { Pedidos } from '../../services/pedidos';
import { FORMULARIO } from '../../shared/ui';
import { ItemParaTabela } from './tabela-itens';

export type FormItem = FormGroup<{
  idTamanho: import('@angular/forms').FormControl<number>;
  classe: import('@angular/forms').FormControl<number>;
  idEspecie: import('@angular/forms').FormControl<number | null>;
  quantidade: import('@angular/forms').FormControl<number>;
}>;

export const novoItemPedido = (fb: NonNullableFormBuilder, i: Partial<NovoPedidoItemDto> = {}): FormItem =>
  fb.group({
    idTamanho: [i.idTamanho ?? 0],
    classe: [i.classe ?? 1],
    idEspecie: [(i.idEspecie ?? null) as number | null],
    quantidade: [i.quantidade ?? 20],
  });

export const itensValidos = (itens: FormArray<FormItem>): NovoPedidoItemDto[] =>
  itens.getRawValue().filter((i) => i.idTamanho > 0 && i.quantidade > 0);

/**
 * Os itens do pedido por tamanho e classe (espécie opcional), o total e a regra da 2ª classe conferida pela API
 * a cada mudança. Serve para o pedido novo e para a correção.
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
  protected readonly tamanhos = toSignal(this.catalogos.tamanhos(), { initialValue: [] });
  protected readonly especies = toSignal(this.catalogos.especies(), { initialValue: [] });
  protected readonly metros = metros;
  protected readonly m3 = m3;
  protected readonly versao = signal(0);
  protected readonly verificacao = signal<VerificacaoPedidoDto | null>(null);

  protected readonly pecas = computed(() => (this.versao(), this.itens().getRawValue().reduce((a, i) => a + (Number(i.quantidade) || 0), 0)));
  protected readonly volume = computed(() => {
    this.versao();
    return this.itens().getRawValue().reduce((a, i) => a + (this.tamanhos().find((t) => t.id === i.idTamanho)?.volumeM3 ?? 0) * (Number(i.quantidade) || 0), 0);
  });

  constructor() {
    let inscricao: Subscription | undefined;
    const destroy = inject(DestroyRef);
    effect(() => {
      const itens = this.itens();
      const idEmpresa = this.idEmpresa();
      inscricao?.unsubscribe();
      inscricao = itens.valueChanges
        .pipe(
          startWith(null),
          debounceTime(350),
          switchMap(() => {
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

  protected adicionar(): void {
    this.itens().push(novoItemPedido(this.fb));
  }

  protected tirar(i: number): void {
    if (this.itens().length > 1) this.itens().removeAt(i);
  }
}

/** Os itens do formulário no formato da tabela de revisão (com o nome da espécie e o m³ do catálogo). */
export const paraTabela = (itens: NovoPedidoItemDto[], tamanhos: { id: number; comprimentoM: number; volumeM3: number }[], especies: { id: number; nomePopular: string | null; nomeCientifico: string }[]): ItemParaTabela[] =>
  itens.map((i) => {
    const t = tamanhos.find((x) => x.id === i.idTamanho);
    const e = especies.find((x) => x.id === i.idEspecie);
    return { comprimentoM: t?.comprimentoM ?? 0, classe: i.classe, especie: e ? (e.nomePopular ?? e.nomeCientifico) : null, quantidade: i.quantidade, volumeM3: (t?.volumeM3 ?? 0) * i.quantidade };
  });