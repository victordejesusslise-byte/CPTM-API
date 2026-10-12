import { Component, input } from '@angular/core';
import { data, km } from '../../helpers/formato';
import { TrocaDto } from '../../models/campo';

export const ROTULO_TROCA: Record<string, string> = {
  Pedida: 'Pedida', Aprovada: 'Aprovada', AguardandoPeca: 'Esperando peça', Separada: 'Separada', Feita: 'Feita',
  Reprovada: 'Reprovada', Recusada: 'Recusada pela via', Cancelada: 'Cancelada',
};

/** O cabeçalho da troca: a peça que sai, a que entra, onde e há quantos dias. */
@Component({
  selector: 'app-resumo-troca',
  template: `
    <div class="kv">
      <div><span class="lbl">Sai</span><b class="mono">{{ t().codigoSaiu ?? '—' }}</b><span class="nota mono">{{ t().tagSaiu ?? 'sem tag' }}</span></div>
      <div><span class="lbl">Entra</span><b class="mono">{{ t().codigoEntrou ?? 'ainda não separada' }}</b><span class="nota mono">{{ t().tagEntrou ?? '' }}</span></div>
      <div><span class="lbl">Km</span><b class="mono">{{ t().km !== null ? km(t().km) : '—' }}</b></div>
      <div><span class="lbl">Pedida em</span><b>{{ data(t().dataPedido) }}</b><span class="nota" [class.vermelho]="t().diasDesdeOPedido > 15">há {{ t().diasDesdeOPedido }} dias</span></div>
    </div>
    @if (t().motivo) { <p class="nota">Motivo: {{ t().motivo }}</p> }
  `,
})
export class ResumoTroca {
  readonly t = input.required<TrocaDto>();
  protected readonly data = data;
  protected readonly km = km;
}