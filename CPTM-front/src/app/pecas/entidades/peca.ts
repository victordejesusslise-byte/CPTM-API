import { AtaqueBiologicoVerificacao, LeituraDto, MetodoCaptura, RachaduraVerificacao, ResultadoVerificacao } from '../../compartilhado/entidades/enums';

export interface PecaDto {
  id: number; codigo: string; tag: string | null; tagFornecedor: string | null; idPedido: number; idLote: number; idFeixe: number; numeroFeixe: string;
  idEspecie: number; especie: string; classe: number; idTamanho: number; comprimentoM: number;
  idArmazem: number; armazem: string; idPilha: number | null; pilha: string | null; entradaArmazemEm: string | null;
  idTrecho: number | null; kmProjeto: number | null; situacao: string;
  /** a nota única de estado: Bom, Regular, Ruim ou Crítico (nula sem avaliação) */
  nota?: string | null;
  /** o ID de posição na via: L09-213100-0012 = linha · km · nº da peça no trecho (nulo fora da via) */
  posicao?: string | null;
}
export interface CadastroPecaDto { resultado: string; mensagem: string; peca: PecaDto | null; }
export interface EventoDaPecaDto { data: string; tipo: string; descricao: string; idUsuario: number | null; origem: string; }
export interface NovaPecaDto {
  idFeixe: number; tagFornecedor?: string | null; tagFornecedorLeitura?: MetodoCaptura | null; metodo: MetodoCaptura;
  tag: string; tagRelida?: string | null; tagsVistas?: number; idCliente?: string;
}
export interface VerificarPecaDto {
  leitura: LeituraDto; ataqueBiologico: AtaqueBiologicoVerificacao; rachadura: RachaduraVerificacao;
  placaAntirrachante: boolean; placaConfere: boolean; resultado: ResultadoVerificacao; motivo?: string | null; fotoArquivo?: string | null; idCliente?: string;
}
export interface GuardarPecaDto { leitura: LeituraDto; idPilha?: number | null; idCliente?: string; }
export interface TrocarPilhaDto { idPilha: number; leitura: LeituraDto; }

export interface PecaConferidaDto { idDormente: number; tag: string | null; pilhaDela: string | null; pilhaOndeRespondeu: string | null; }
export interface ConferenciaDto {
  id: number; idArmazem: number; armazem: string; iniciadaEm: string; concluidaEm: string | null;
  pilhasLidas: string[]; pilhasFaltando: string[]; esperadas: number; lidas: number;
  faltando: PecaConferidaDto[]; aMais: PecaConferidaDto[]; tagsSemCadastro: string[];
}
export interface FotoDaPecaDto { momento: 'entrada' | 'via'; arquivo: string; data: string; descricao: string | null; noServidor: boolean; analiseIa: string | null; }
export interface FotosDaPecaDto { idDormente: number; entrada: FotoDaPecaDto[]; via: FotoDaPecaDto[]; }
