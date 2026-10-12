import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { mensagemDeErro } from '../../helpers/formato';
import { CampoOcrDto, LeituraOcrDto, Ocr } from '../../services/ocr';
import { Pedidos } from '../../services/pedidos';
import { FORMULARIO } from '../../shared/ui';

const NOMES: Record<string, string> = {
  Fornecedor: 'Fornecedor', Ata: 'Ata', Contrato: 'Contrato', EspecieClasse: 'Espécie e classe', Comprimento: 'Comprimento',
  Tratamento: 'Tratamento', Tratadora: 'Tratadora', Inspetora: 'Inspetora', Ano: 'Ano',
};

/** Chegada → Placa do feixe: a foto da placa lida pelo OCR no servidor; o operador confere e corrige cada campo. */
@Component({
  selector: 'app-placa-feixe',
  imports: [FormsModule, DecimalPipe, FORMULARIO],
  templateUrl: './placa-feixe.html',
})
export class PlacaFeixe {
  private readonly ocr = inject(Ocr);
  private readonly pedidos = inject(Pedidos);
  protected readonly lista = rxResource({ stream: () => this.pedidos.listar() });
  protected readonly abertos = computed(() => (this.lista.value() ?? []).filter((p) => p.situacao !== 'Cancelado' && p.situacao !== 'Recebido'));
  protected readonly idPedido = signal<number | null>(null);
  protected readonly foto = signal<File | null>(null);
  protected readonly leitura = signal<LeituraOcrDto | null>(null);
  protected readonly valores: Record<string, string> = {};
  protected readonly nomes = NOMES;
  protected readonly mensagem = signal<string | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  protected escolher(e: Event): void {
    this.foto.set((e.target as HTMLInputElement).files?.[0] ?? null);
  }

  protected ler(): void {
    const f = this.foto();
    if (!f) return this.erro.set('Tire a foto da placa do feixe.');
    this.enviar(this.ocr.ler(f, this.idPedido()), (l) => {
      for (const c of l.campos) this.valores[c.campo] = c.valorConfirmado ?? c.valorLido ?? '';
      return l.campos.some((c) => c.conferePedido === false) ? 'Placa lida. Os campos em vermelho não batem com o pedido: confira.' : 'Placa lida. Confira os campos e confirme.';
    });
  }

  protected confirmar(l: LeituraOcrDto): void {
    this.enviar(this.ocr.confirmar(l.id, l.campos.map((c) => ({ campo: c.campo, valor: (this.valores[c.campo] ?? '').trim() }))), () => 'Placa confirmada. O que foi corrigido vira amostra para treinar o OCR.');
  }

  protected confianca(c: CampoOcrDto): string {
    return c.confianca === null ? '—' : `${Math.round(c.confianca)}%`;
  }

  private enviar(acao: import('rxjs').Observable<LeituraOcrDto>, ok: (l: LeituraOcrDto) => string): void {
    this.enviando.set(true);
    this.erro.set(null);
    this.mensagem.set(null);
    acao.subscribe({
      next: (l) => {
        this.enviando.set(false);
        this.leitura.set(l);
        this.mensagem.set(ok(l));
      },
      error: (e) => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e));
      },
    });
  }
}