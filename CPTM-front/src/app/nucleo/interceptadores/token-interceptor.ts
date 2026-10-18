import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { Sessao } from '../sessao/sessao';

/** A chamada é para a API do próprio sistema? Compara o endereço já resolvido, não só o começo do texto. */
export const ehDaApi = (url: string): boolean => {
  try {
    const base = new URL(ambiente.api);
    const destino = new URL(url, globalThis.location?.href ?? base.href);
    return destino.origin === base.origin && (destino.pathname === base.pathname || destino.pathname.startsWith(base.pathname + '/'));
  } catch {
    return false;
  }
};

/**
 * Põe o token só nas chamadas para a API do sistema (segurança, 2026-10-18: nunca para endereço de fora). Se a API responder 401
 * (token vencido ou sessão encerrada), encerra a sessão e volta ao login.
 */
export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const sessao = inject(Sessao);
  const router = inject(Router);
  const token = sessao.token();
  const pedido = token && ehDaApi(req.url) ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(pedido).pipe(
    catchError((erro: HttpErrorResponse) => {
      if (erro.status === 401 && ehDaApi(req.url) && !req.url.endsWith('/auth/login') && !req.url.endsWith('/auth/sair')) {
        sessao.sair();
        router.navigate(['/login']);
      }
      return throwError(() => erro);
    }),
  );
};
