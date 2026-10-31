import { Component, DestroyRef, computed, effect, inject, input, linkedSignal, signal, untracked } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { mensagemDeErro, metros } from '../../../compartilhado/utilitarios/formato';
import { ChegadaDto, NovoFeixeDto } from '../../../pedidos/entidades/pedido';
import { Catalogos } from '../../../catalogos/servicos/catalogos';
import { EspecieDto, TamanhoDto } from '../../../catalogos/entidades/catalogo';
import { Pedidos } from '../../../pedidos/servicos/pedidos';
import { Pecas } from '../../../pecas/servicos/pecas';
import { FORMULARIO } from '../../../compartilhado/ui';
import { LeituraOcrDto, Ocr } from '../../servicos/ocr';
import { PlacaNoFeixe, feixeDaPlaca, placaNoFeixe } from './placa-no-feixe';
import { LegivelPipe } from '../../../compartilhado/utilitarios/legivel';
import { valorDe } from '../../../compartilhado/utilitarios/recurso';

/**
 * A chegada da carga: os feixes que vieram, conferidos com o pedido e a nota. A primeira chegada cria o lote.
 * As placas lidas e confirmadas em Placa do feixe para este pedido já vêm aqui, cada uma num feixe: espécie, tamanho, classe e
 * tratamento preenchidos e o quadro com os 9 dados e a foto. Não há leitura aqui (Victor, 2026-10-17). Ao registrar, a placa fica no feixe.
 */
@Component({
  selector: 'app-chegada',
  imports: [LegivelPipe, Esqueleto, RouterLink, FORMULARIO],
  templateUrl: './chegada.html',
  styles: `
    .placa-lida { display: grid; grid-template-columns: minmax(0, 160px) 1fr; gap: 14px; align-items: start; margin: 4px 0 10px; padding: 12px; border: 1px solid var(--linha); border-radius: 8px; background: var(--superficie-2); }
    .placa-lida img { width: 100%; border-radius: 6px; }
    .placa-lida .sem-foto { display: grid; place-items: center; min-height: 120px; border: 1px dashed var(--linha-2); border-radius: 6px; font-size: 12px; color: var(--tinta-3); }
    .placa-lida h3 { margin: 0 0 6px; font-size: 14px; }
    .placa-lida dl { margin: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 6px 14px; font-size: 13px; }
    .placa-lida dt { color: var(--tinta-3); }
    .placa-lida dd { margin: 0; font-family: var(--fonte-mono); }
    @media (max-width: 560px) { .placa-lida { grid-template-columns: 1fr; } .placa-lida img, .placa-lida .sem-foto { max-width: 220px; } }
  `,
})
export class Chegada {
  protected readonly valorDe = valorDe;
  /** ?pedido= vindo da tela do pedido */
  readonly pedido = input<string>();
  private readonly pedidos = inject(Pedidos);
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly catalogos = inject(Catalogos);
  private readonly ocr = inject(Ocr);
  private readonly pecas = inject(Pecas);

  protected readonly metros = metros;
  protected readonly lista = rxResource({ stream: () => this.pedidos.listar() });
  protected readonly abertos = computed(() => (valorDe(this.lista) ?? []).filter((p) => p.situacao !== 'Cancelado' && p.situacao !== 'Recebido'));
  protected readonly idPedido = linkedSignal(() => (this.pedido() ? Number(this.pedido()) : null));
  protected readonly notas = rxResource({ params: () => this.idPedido() ?? undefined, stream: ({ params }) => this.pedidos.notas(params) });
  /** As placas confirmadas do pedido que ainda não viraram feixe. */
  protected readonly placasDoPedido = rxResource({ params: () => this.idPedido() ?? undefined, stream: ({ params }) => this.ocr.doPedido(params) });
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
  /** A placa de cada feixe que veio de Placa do feixe, e a foto dela (baixada com o login). */
  protected readonly placas = signal(new Map<AbstractControl, PlacaNoFeixe>());
  protected readonly fotos = signal(new Map<number, string>());
  /** O pedido cujas placas já viraram feixes (para não refazer os feixes a cada sinal). */
  private placasDe: number | null = null;

  constructor() {
    this.form.controls.feixes.valueChanges.subscribe((f) => this.somaFeixes.set(f.reduce((s, x) => s + (Number(x.quantidade) || 0), 0)));
    this.somaFeixes.set(1);
    effect(() => {
      const id = this.idPedido();
      const placas = this.placasDoPedido.hasValue() && !this.placasDoPedido.isLoading() ? this.placasDoPedido.value() : undefined;
      const especies = this.especies();
      const tamanhos = this.tamanhos();
      if (id === null || !placas || !especies.length || !tamanhos.length || this.placasDe === id) return;
      untracked(() => this.feixesDasPlacas(id, placas, especies, tamanhos));
    });
    inject(DestroyRef).onDestroy(() => this.fotos().forEach((url) => URL.revokeObjectURL(url)));
  }

  protected novoFeixe() {
    return this.fb.group({
      numeroFeixe: ['', Validators.required],
      quantidade: [1, [Validators.min(1), Validators.max(1000)]],
      idEspecie: [0, Validators.min(1)],
      idTamanho: [0, Validators.min(1)],
      /** O operador escolhe 1ª ou 2ª em todo feixe (Victor, 2026-10-17); a placa do pedido já preenche. */
      classe: [null as number | null, Validators.required],
      tratamento: [''],
      /** A placa lida em Placa do feixe de onde o feixe veio. */
      idLeituraOcr: [null as number | null],
    });
  }

  protected removerFeixe(i: number): void {
    const feixe = this.form.controls.feixes.at(i);
    this.form.controls.feixes.removeAt(i);
    this.placas.update((m) => { const n = new Map(m); n.delete(feixe); return n; });
  }

  /** Cada placa confirmada do pedido vira um feixe preenchido; sem placa, fica um feixe em branco. O número do feixe o operador digita. */
  private feixesDasPlacas(idPedido: number, placas: LeituraOcrDto[], especies: EspecieDto[], tamanhos: TamanhoDto[]): void {
    this.placasDe = idPedido;
    const feixes = this.form.controls.feixes;
    feixes.clear();
    const mapa = new Map<AbstractControl, PlacaNoFeixe>();
    for (const leitura of placas) {
      const placa = placaNoFeixe(leitura);
      const feixe = this.novoFeixe();
      feixe.patchValue({ ...Object.fromEntries(Object.entries(feixeDaPlaca(placa.valores, especies, tamanhos)).filter(([, v]) => v !== null)), idLeituraOcr: leitura.id });
      feixes.push(feixe);
      mapa.set(feixe, placa);
      this.baixarFoto(leitura);
    }
    if (!placas.length) feixes.push(this.novoFeixe());
    this.placas.set(mapa);
  }

  private baixarFoto(leitura: LeituraOcrDto): void {
    if (this.fotos().has(leitura.id)) return;
    this.pecas.arquivoFoto(leitura.fotoArquivo).subscribe({
      next: (blob) => this.fotos.update((m) => new Map(m).set(leitura.id, URL.createObjectURL(blob))),
      error: () => undefined, // sem a foto, o quadro mostra só os dados
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
