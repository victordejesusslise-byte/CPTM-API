import { DecimalPipe } from '@angular/common';
import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { mensagemDeErro } from '../../../compartilhado/utilitarios/formato';
import { CampoOcrDto, LeituraOcrDto, Ocr } from '../../servicos/ocr';
import { FORMULARIO } from '../../../compartilhado/ui';

export const NOMES_PLACA: Record<string, string> = {
  Fornecedor: 'Fornecedor', Ata: 'Ata', Contrato: 'Contrato', EspecieClasse: 'Espécie e classe', Comprimento: 'Comprimento',
  Tratamento: 'Tratamento', Tratadora: 'Tratadora', Inspetora: 'Inspetora', Ano: 'Ano',
};

/** A placa confirmada: a leitura, os valores que o operador confirmou (campo → valor) e a foto tirada. */
export interface PlacaConfirmada {
  leitura: LeituraOcrDto;
  valores: Record<string, string>;
  foto: File;
}

/**
 * Ler a placa do feixe pela foto: o servidor lê (OCR), o operador confere e corrige cada campo e confirma.
 * Usado na tela Placa do feixe e dentro de cada feixe da chegada, que preenche o feixe com o que foi confirmado.
 */
@Component({
  selector: 'app-ler-placa',
  imports: [FormsModule, DecimalPipe, FORMULARIO],
  templateUrl: './ler-placa.html',
})
export class LerPlaca {
  /** O pedido com que a placa é conferida; nulo: "sem pedido". */
  readonly idPedido = input<number | null>(null);
  readonly confirmada = output<PlacaConfirmada>();
  private readonly ocr = inject(Ocr);
  protected readonly foto = signal<File | null>(null);
  protected readonly leitura = signal<LeituraOcrDto | null>(null);
  protected readonly valores: Record<string, string> = {};
  protected readonly nomes = NOMES_PLACA;
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
      if (!l.campos.length) return 'A foto foi lida, mas nenhum campo da placa apareceu. Tire outra foto, de frente e mais perto.';
      return l.campos.some((c) => c.conferePedido === false) ? 'Placa lida. Os campos em vermelho não batem com o pedido: confira.' : 'Placa lida. Confira os campos e confirme.';
    });
  }

  protected confirmar(l: LeituraOcrDto): void {
    const foto = this.foto()!;
    this.enviar(this.ocr.confirmar(l.id, l.campos.map((c) => ({ campo: c.campo, valor: (this.valores[c.campo] ?? '').trim() }))), (confirmada) => {
      this.confirmada.emit({ leitura: confirmada, valores: { ...this.valores }, foto });
      return 'Placa confirmada. O que foi corrigido vira amostra para treinar o OCR.';
    });
  }

  protected confianca(c: CampoOcrDto): string {
    return c.confianca === null ? '—' : `${Math.round(c.confianca)}%`;
  }

  private enviar(acao: Observable<LeituraOcrDto>, ok: (l: LeituraOcrDto) => string): void {
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
