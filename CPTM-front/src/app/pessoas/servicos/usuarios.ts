import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { NovoUsuarioDto, UsuarioDto } from '../entidades/usuario';

/** /api/usuarios: só o supervisor. Cada pessoa entra com o próprio e-mail e a própria senha. */
@Injectable({ providedIn: 'root' })
export class Usuarios {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/usuarios`;

  listar(): Observable<UsuarioDto[]> {
    return this.http.get<UsuarioDto[]>(this.url);
  }
  criar(novo: NovoUsuarioDto): Observable<UsuarioDto> {
    return this.http.post<UsuarioDto>(this.url, novo);
  }
}