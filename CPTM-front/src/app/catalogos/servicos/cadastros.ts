import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ambiente } from '../../../environments/ambiente';
import { consulta } from '../../compartilhado/utilitarios/consulta';
import {
  ContratoDto, EmpresaDto, EstacaoDto, NovaEmpresaDto, NovaEstacaoDto, NovoContratoDto, NovoTrechoDto, ParametroDto, TrechoDto,
} from '../entidades/cadastro';
import { PapelEmpresa } from '../../compartilhado/entidades/enums';

/** Cadastros de apoio (empresas, contratos, estações, trechos) e parâmetros. Gravar é só do supervisor. */
@Injectable({ providedIn: 'root' })
export class Cadastros {
  private readonly http = inject(HttpClient);
  private readonly api = ambiente.api;

  empresas(papel?: PapelEmpresa | null): Observable<EmpresaDto[]> {
    return this.http.get<EmpresaDto[]>(`${this.api}/empresas`, { params: consulta({ papel }) });
  }
  novaEmpresa(dto: NovaEmpresaDto): Observable<EmpresaDto> {
    return this.http.post<EmpresaDto>(`${this.api}/empresas`, dto);
  }
  ativarEmpresa(id: number, ativo: boolean): Observable<void> {
    return this.http.put<void>(`${this.api}/empresas/${id}/ativo`, null, { params: consulta({ ativo }) });
  }
  contratos(idEmpresa?: number | null): Observable<ContratoDto[]> {
    return this.http.get<ContratoDto[]>(`${this.api}/contratos`, { params: consulta({ idEmpresa }) });
  }
  novoContrato(dto: NovoContratoDto): Observable<ContratoDto> {
    return this.http.post<ContratoDto>(`${this.api}/contratos`, dto);
  }
  estacoes(): Observable<EstacaoDto[]> {
    return this.http.get<EstacaoDto[]>(`${this.api}/estacoes`);
  }
  novaEstacao(dto: NovaEstacaoDto): Observable<EstacaoDto> {
    return this.http.post<EstacaoDto>(`${this.api}/estacoes`, dto);
  }
  trechos(): Observable<TrechoDto[]> {
    return this.http.get<TrechoDto[]>(`${this.api}/trechos`);
  }
  novoTrecho(dto: NovoTrechoDto): Observable<TrechoDto> {
    return this.http.post<TrechoDto>(`${this.api}/trechos`, dto);
  }
  parametros(): Observable<ParametroDto[]> {
    return this.http.get<ParametroDto[]>(`${this.api}/parametros`);
  }
  alterarParametro(chave: string, valor: string): Observable<ParametroDto> {
    return this.http.put<ParametroDto>(`${this.api}/parametros/${encodeURIComponent(chave)}`, { valor });
  }
}