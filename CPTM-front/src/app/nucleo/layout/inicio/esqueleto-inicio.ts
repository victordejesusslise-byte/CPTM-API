import { Component, input } from '@angular/core';
import { Esqueleto } from '../../../compartilhado/componentes/esqueleto/esqueleto';

/** No app, quanto tempo o skeleton do login fica na tela antes do login (Victor, 2026-10-24: pediu 5 segundos e depois achou muito: 3). */
export const TEMPO_DO_ESQUELETO_DO_LOGIN_MS = 3000;

/**
 * O skeleton enquanto a primeira tela carrega (Victor, 2026-10-24: "falta o skeleton no início quando aparece o login"): no lugar da tela
 * vazia, a forma do login (o cartão com o logo girando, os dois campos e o botão, no fundo com as faixas da marca) ou, para quem já está
 * logado, a forma de uma tela do sistema.
 */
@Component({
  selector: 'app-esqueleto-inicio',
  imports: [Esqueleto],
  template: `
    @if (tipo() === 'login') {
      <div class="inicio inicio--login">
        <div class="inicio__cartao"><app-esqueleto tipo="formulario" [quantidade]="2" rotulo="Abrindo o YBYRA…" /></div>
      </div>
    } @else {
      <div class="inicio"><app-esqueleto tipo="pagina" [quantidade]="4" rotulo="Abrindo o YBYRA…" /></div>
    }
  `,
  styles: `
    .inicio { position: fixed; inset: 0; z-index: 900; overflow: hidden; padding: calc(24px + var(--topo-seguro)) 16px 24px; background: var(--plano); }
    /* O mesmo fundo do login: as faixas da marca à esquerda. */
    .inicio--login { display: grid; place-items: center; padding: 16px; border-left: 10px solid var(--madeira);
      box-shadow: inset 10px 0 0 var(--areia), inset 20px 0 0 var(--verde), inset 30px 0 0 var(--vinho); }
    .inicio__cartao { width: min(400px, 100%); padding: 28px; border: 1px solid var(--linha); border-radius: 14px; background: var(--superficie); box-shadow: var(--sombra); }
    .inicio__cartao app-esqueleto { --linha: transparent; }
  `,
})
export class EsqueletoInicio {
  readonly tipo = input<'login' | 'pagina'>('login');
}
