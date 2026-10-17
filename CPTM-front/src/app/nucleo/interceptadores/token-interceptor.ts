import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Sessao } from '../sessao/sessao';

/** Põe o token em toda chamada; se a API responder 401 (token vencido), encerra a sessão e volta ao login. */
export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const sessao = inject(Sessao);
  const router = inject(Router);
  const token = sessao.token();
  const pedido = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(pedido).pipe(
    catchError((erro: HttpErrorResponse) => {
      if (erro.status === 401 && !req.url.endsWith('/auth/login')) {
        sessao.sair();
        router.navigate(['/login']);
      }
      return throwError(() => erro);
    }),
  );
};