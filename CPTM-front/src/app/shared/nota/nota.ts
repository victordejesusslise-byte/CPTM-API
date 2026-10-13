import { Component, input } from '@angular/core';

/** A classe da pílula de cada nota: Bom, Regular, Ruim ou Crítico. */
export function classeNota(nota: string | null | undefined): string {
  switch (nota) {
    case 'Bom': return 'pilula nota-peca nota-peca--bom';
    case 'Regular': return 'pilula nota-peca nota-peca--regular';
    case 'Ruim': return 'pilula nota-peca nota-peca--ruim';
    case 'Crítico': return 'pilula nota-peca nota-peca--critico';
    default: return 'pilula pilula--quieta';
  }
}

/**
 * A nota única de estado da peça, a mesma em todas as telas: vale a avaliação mais recente
 * (verificação de qualidade, inspeção ou alerta aberto/confirmado). Uso: <app-nota [nota]="p.nota" />
 */
@Component({
  selector: 'app-nota',
  template: `<span [class]="classe()" title="Nota única de estado: a avaliação mais recente (qualidade, inspeção ou alerta)">{{ nota() ?? 'sem nota' }}</span>`,
})
export class Nota {
  readonly nota = input<string | null | undefined>(null);
  protected classe = () => classeNota(this.nota());
}
