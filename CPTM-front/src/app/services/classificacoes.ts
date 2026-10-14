import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../environments/ambiente';
import { consulta } from '../helpers/consulta';
import { LeituraDto } from '../models/enums';

export type MetodoClassificacao = 'Comparacao' | 'Visual' | 'Catalogo' | 'Laboratorio' | 'Nir';
export interface ClassificacaoDto { id: number; idDormente: number; idEspecie: number; especie: string; classe: number; metodo: string; data: string; idUsuario: number; anterior: number | null; observacao: string | null; umidadePct?: number | null; confiancaPct?: number | null; }

/** /api/classificacoes: o especialista confirma ou corrige a espécie da placa (a de antes fica). */
@Injectable({ providedIn: 'root' })
export class Classificacoes {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/classificacoes`;

  /** Com a pistola NIR, vai junto o que ela mediu (umidade obrigatória, confiança opcional). */
  classificar(idDormente: number, idEspecie: number, metodo: MetodoClassificacao, leitura: LeituraDto, observacao: string | null,
    nir: { umidadePct: number | null; confiancaPct: number | null } = { umidadePct: null, confiancaPct: null }): Observable<ClassificacaoDto> {
    return this.http.post<ClassificacaoDto>(this.url, { idDormente, idEspecie, metodo, leitura, observacao, ...nir, idCliente: crypto.randomUUID() });
  }
  daPeca(idDormente: number): Observable<ClassificacaoDto[]> {
    return this.http.get<ClassificacaoDto[]>(this.url, { params: consulta({ idDormente }) });
  }
  recentes(dias = 30): Observable<ClassificacaoDto[]> {
    return this.http.get<ClassificacaoDto[]>(this.url, { params: consulta({ dias }) });
  }
}
