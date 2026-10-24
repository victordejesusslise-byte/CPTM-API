import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { Cargo } from '../../pessoas/entidades/usuario';
import { Sessao } from '../sessao/sessao';

/**
 * Só entra logado, e depois de a API confirmar quem é o dono do token (uma vez por abertura do sistema). Se a rota tiver data.cargos,
 * só esses cargos. Esconder a tela é conforto: quem decide é a API, que confere o cargo em toda chamada.
 */
export const cargoGuard: CanActivateFn = (route) => {
  const sessao = inject(Sessao);
  const router = inject(Router);
  if (!sessao.logado()) return router.createUrlTree(['/login']);
  const cargos = route.data?.['cargos'] as Cargo[] | undefined;
  return sessao.confirmar().pipe(
    map((ok) => (!ok ? router.createUrlTree(['/login']) : !cargos || sessao.temCargo(...cargos) ? true : router.createUrlTree(['/painel']))),
  );
};
