import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { mensagemDeErro, metros } from '../../helpers/formato';
import { ChegadaDto, NovoFeixeDto } from '../../models/pedido';
import { Catalogos } from '../../services/catalogos';
import { Pedidos } from '../../services/pedidos';
import { FORMULARIO } from '../../shared/ui';

/** A chegada da carga: os feixes que vieram, conferidos com o pedido e a nota. A primeira chegada cria o lote. */
@Component({
  selector: 'app-chegada',
  imports: [RouterLink, FORMULARIO],
  templateUrl: './chegada.html',
})
export class Chegada {
  /** ?pedido= vindo da tela do pedido */
  readonly pedido = input<string>();
  private readonly pedidos = inject(Pedidos);
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly catalogos = inject(Catalogos);

  protected readonly metros = metros;
  protected readonly lista = rxResource({ stream: () => this.pedidos.listar() });
  protected readonly abertos = computed(() => (this.lista.value() ?? []).filter((p) => p.situacao !== 'Cancelado' && p.situacao !== 'Recebido'));
  protected readonly idPedido = linkedSignal(() => (this.pedido() ? Number(this.pedido()) : null));
  protected readonly notas = rxResource({ params: () => this.idPedido() ?? undefined, stream: ({ params }) => this.pedidos.notas(params) });
  protected readonly armazens = toSignal(this.catalogos.armazens(), { initialValue: [] });
  protected readonly tamanhos = toSignal(this.catalogos.tamanhos(), { initialValue: [] });
  protected readonly especies = toSignal(this.catalogos.especies(), { initialValue: [] });

  protected readonly form = this.fb.group({
    idNota: [null as number | null],
    idArmazem: [0, Validators.min(1)],
    documentoOrigemTipo: [''],
    documentoOrigemNumero: [''],
    dataRecebimento: [''],
    quantidadeRecebida: [1, Validators.min(1)],
    observacao: [''],
    feixes: this.fb.array([this.novoFeixe()]),
  });
  protected readonly resultado = signal<ChegadaDto | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);
  protected readonly somaFeixes = signal(0);

  constructor() {
    this.form.controls.feixes.valueChanges.subscribe((f) => this.somaFeixes.set(f.reduce((s, x) => s + (Number(x.quantidade) || 0), 0)));
    this.somaFeixes.set(1);
  }

  protected novoFeixe() {
    return this.fb.group({
      numeroFeixe: ['', Validators.required],
      quantidade: [1, [Validators.min(1), Validators.max(1000)]],
      idEspecie: [0, Validators.min(1)],
      idTamanho: [0, Validators.min(1)],
      /** null = a classe do item do pedido; o operador escolhe quando o pedido tem 1ª e 2ª para o feixe */
      classe: [null as number | null],
      tratamento: [''],
    });
  }

  protected registrar(): void {
    const id = this.idPedido();
    if (!id || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.enviando.set(true);
    this.erro.set(null);
    this.pedidos
      .novaChegada(id, {
        idNota: v.idNota,
        idArmazem: v.idArmazem,
        documentoOrigemTipo: v.documentoOrigemTipo || null,
        documentoOrigemNumero: v.documentoOrigemNumero || null,
        dataRecebimento: v.dataRecebimento || null,
        quantidadeRecebida: v.quantidadeRecebida,
        observacao: v.observacao || null,
        feixes: v.feixes.map((f) => ({ ...f, tratamento: f.tratamento || null }) as NovoFeixeDto),
      })
      .subscribe({
        next: (c) => {
          this.resultado.set(c);
          this.enviando.set(false);
        },
        error: (e) => {
          this.erro.set(mensagemDeErro(e));
          this.enviando.set(false);
        },
      });
  }
}