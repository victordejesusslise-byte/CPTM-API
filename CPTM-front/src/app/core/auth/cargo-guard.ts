import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Cargo } from '../../models/usuario';
import { Sessao } from './sessao';

/** Só entra logado; se a rota tiver data.cargos, só esses cargos. A API confere de novo. */
export const cargoGuard: CanActivateFn = (route) => {
  const sessao = inject(Sessao);
  const router = inject(Router);
  if (!sessao.logado()) return router.createUrlTree(['/login']);
  const cargos = route.data?.['cargos'] as Cargo[] | undefined;
  return !cargos || sessao.temCargo(...cargos) ? true : router.createUrlTree(['/painel']);
};