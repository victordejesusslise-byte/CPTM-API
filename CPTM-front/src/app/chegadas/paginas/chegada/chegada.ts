import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { ChegadaDto, NovoFeixeDto } from '../../../pedidos/entidades/pedido';
import { Catalogos } from '../../../catalogos/servicos/catalogos';
import { Pedidos } from '../../../pedidos/servicos/pedidos';
import { FORMULARIO } from '../../../compartilhado/ui';
import { LerPlaca, PlacaConfirmada } from './ler-placa';
import { PlacaNoFeixe, feixeDaPlaca, placaNoFeixe } from './placa-no-feixe';

/**
 * A chegada da carga: os feixes que vieram, conferidos com o pedido e a nota. A primeira chegada cria o lote.
 * Em cada feixe, "Ler a placa pela foto": confirmada a placa, o feixe ganha espécie, tamanho, classe e tratamento, e mostra a placa.
 */
@Component({
  selector: 'app-chegada',
  imports: [RouterLink, FORMULARIO, LerPlaca],
  templateUrl: './chegada.html',
  styles: `
    .placa-lida { display: grid; grid-template-columns: minmax(0, 160px) 1fr; gap: 14px; align-items: start; margin: 4px 0 10px; padding: 12px; border: 1px solid var(--linha); border-radius: 8px; background: var(--superficie-2); }
    .placa-lida img { width: 100%; border-radius: 6px; }
    .placa-lida h3 { margin: 0 0 6px; font-size: 14px; }
    .placa-lida dl { margin: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 6px 14px; font-size: 13px; }
    .placa-lida dt { color: var(--tinta-3); }
    .placa-lida dd { margin: 0; font-family: var(--fonte-mono); }
    @media (max-width: 560px) { .placa-lida { grid-template-columns: 1fr; } .placa-lida img { max-width: 220px; } }
  `,
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
  /** O feixe com a leitura da placa aberta (um por vez) e as placas já confirmadas, por feixe. */
  protected readonly placaAberta = signal<AbstractControl | null>(null);
  protected readonly placas = signal(new Map<AbstractControl, PlacaNoFeixe>());

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
      /** O operador escolhe 1ª ou 2ª em todo feixe (Victor, 2026-10-17); a placa lida pela foto já preenche. */
      classe: [null as number | null, Validators.required],
      tratamento: [''],
    });
  }

  /** Placa confirmada: preenche só o que ela disse (o resto fica como o operador deixou) e guarda os 9 campos e a foto para mostrar no feixe. */
  protected placaConfirmada(feixe: AbstractControl, p: PlacaConfirmada): void {
    const daPlaca = feixeDaPlaca(p.valores, this.especies(), this.tamanhos());
    feixe.patchValue(Object.fromEntries(Object.entries(daPlaca).filter(([, v]) => v !== null)));
    const anterior = this.placas().get(feixe);
    if (anterior) URL.revokeObjectURL(anterior.foto);
    this.placas.update((m) => new Map(m).set(feixe, placaNoFeixe(p)));
    this.placaAberta.set(null);
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