import { PapelEmpresa } from '../../compartilhado/entidades/enums';

export interface EmpresaDto { id: number; nome: string; cnpj: string | null; papel: PapelEmpresa; ativo: boolean; }
export interface ContratoDto { id: number; idEmpresa: number; empresa: string; numeroAta: string; numeroContrato: string; vigenciaInicio: string | null; vigenciaFim: string | null; }
export interface EstacaoDto { id: number; nome: string; linha: string; km: number; ordem: number; }
export interface TrechoDto { id: number; idEstacaoInicio: number; estacaoInicio: string; idEstacaoFim: number; estacaoFim: string; classeExigida: number; }
export interface ParametroDto { chave: string; valor: string; alteradoEm: string | null; }

export interface NovaEmpresaDto { nome: string; cnpj?: string | null; endereco?: string | null; telefone?: string | null; papel: PapelEmpresa; }
export interface NovoContratoDto { idEmpresa: number; numeroAta: string; numeroContrato: string; vigenciaInicio?: string | null; vigenciaFim?: string | null; }
export interface NovaEstacaoDto { nome: string; linha: string; km: number; ordem: number; }
export interface NovoTrechoDto { idEstacaoInicio: number; idEstacaoFim: number; classeExigida: number; }