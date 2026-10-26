import { Component, inject, input } from '@angular/core';
import { dataHora } from '../../compartilhado/utilitarios/formato';
import { TrabalhoDaNoite } from './trabalho-da-noite';

/** Sem rede (no app), no alto das telas que abrem pelo que está guardado: o que aparece e de quando é (pode estar velho). */
@Component({
  selector: 'app-aviso-guardado',
  template: `@if (trabalho.usarGuardado()) {
    <p class="chamada chamada--neutra" role="status">{{ texto() }}@if (trabalho.atualizadoEm(); as quando) {<span> · atualizado em {{ dataHora(quando) }}</span>}.</p>
  }`,
})
export class AvisoGuardado {
  readonly texto = input('Sem rede: aparece o que está guardado no celular');
  protected readonly trabalho = inject(TrabalhoDaNoite);
  protected readonly dataHora = dataHora;
}
