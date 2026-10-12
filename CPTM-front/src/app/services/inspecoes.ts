import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../environments/ambiente';
import { consulta } from '../helpers/consulta';
import { AtaqueBiologicoInspecao, EstadoConservacao, LeituraDto, RachaduraInspecao } from '../models/enums';

export interface InspecaoDto {
  id: number; idDormente: number; idUsuario: number; idAlerta: number | null; iniciadoEm: string; finalizadoEm: string; duracaoS: number;
  estadoConservacao: EstadoConservacao; rachadura: RachaduraInspecao; ataqueBiologico: AtaqueBiologicoInspecao; umidadePct: number | null; vidaUtilMeses: number | null;
  tipoMadeiraConfere: boolean; resultado: 'Aprovada' | 'Reprovada'; motivosDeReprovacao: string[]; observacao: string | null; fotoArquivo: string;
}
export interface NovaInspecaoDto {
  idDormente: number; idAlerta?: number | null; leitura: LeituraDto; iniciadoEm: string; finalizadoEm: string;
  estadoConservacao: EstadoConservacao; rachadura: RachaduraInspecao; ataqueBiologico: AtaqueBiologicoInspecao;
  umidadePct?: number | null; vidaUtilMeses?: number | null; tipoMadeiraConfere: boolean; observacao?: string | null; fotoArquivo: string; idCliente?: string;
}

/** /api/inspecoes: o checklist do técnico; com alerta, a inspeção decide o alerta. */
@Injectable({ providedIn: 'root' })
export class Inspecoes {
  private readonly http = inject(HttpClient);
  private readonly url = `${ambiente.api}/inspecoes`;

  registrar(dto: NovaInspecaoDto): Observable<InspecaoDto> {
    return this.http.post<InspecaoDto>(this.url, dto);
  }
  daPeca(idDormente: number): Observable<InspecaoDto[]> {
    return this.http.get<InspecaoDto[]>(this.url, { params: consulta({ idDormente }) });
  }
}