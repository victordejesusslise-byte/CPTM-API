import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ambiente } from '../../environments/ambiente';
import { consulta } from '../helpers/consulta';
import { CadastroPecaDto, EventoDaPecaDto, GuardarPecaDto, NovaPecaDto, PecaDto, TrocarPilhaDto, VerificarPecaDto } from '../models/peca';

/** A peça: tag, cadastro, qualidade, entrada na pilha, troca de pilha, estoque e histórico. */
@Injectable({ providedIn: 'root' })
export class Pecas {
  private readonly http = inject(HttpClient);
  private readonly api = ambiente.api;

  proximaTag(): Observable<string> {
    return this.http.get<{ tag: string }>(`${this.api}/tags/proxima`).pipe(map((r) => r.tag));
  }
  porTag(tag: string): Observable<PecaDto> {
    return this.http.get<PecaDto>(`${this.api}/tags/${encodeURIComponent(tag.trim())}`);
  }
  buscar(id: number): Observable<PecaDto> {
    return this.http.get<PecaDto>(`${this.api}/dormentes/${id}`);
  }
  doFeixe(idFeixe: number): Observable<PecaDto[]> {
    return this.http.get<PecaDto[]>(`${this.api}/dormentes`, { params: consulta({ idFeixe }) });
  }
  historico(id: number): Observable<EventoDaPecaDto[]> {
    return this.http.get<EventoDaPecaDto[]>(`${this.api}/dormentes/${id}/historico`);
  }
  /** 201 com a peça; 409 quando a gravação da tag não vale (a resposta traz a mensagem). */
  cadastrar(nova: NovaPecaDto): Observable<CadastroPecaDto> {
    return this.http.post<CadastroPecaDto>(`${this.api}/dormentes`, nova);
  }
  verificar(id: number, dto: VerificarPecaDto): Observable<PecaDto> {
    return this.http.post<PecaDto>(`${this.api}/dormentes/${id}/verificacao`, dto);
  }
  guardar(id: number, dto: GuardarPecaDto): Observable<PecaDto> {
    return this.http.post<PecaDto>(`${this.api}/dormentes/${id}/armazem`, dto);
  }
  trocarPilha(id: number, dto: TrocarPilhaDto): Observable<PecaDto> {
    return this.http.post<PecaDto>(`${this.api}/dormentes/${id}/pilha`, dto);
  }
  estoque(filtro: { idArmazem?: number | null; idTamanho?: number | null; classe?: number | null }): Observable<PecaDto[]> {
    return this.http.get<PecaDto[]>(`${this.api}/estoque`, { params: consulta(filtro) });
  }
}