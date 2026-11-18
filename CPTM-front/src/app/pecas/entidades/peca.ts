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
  /** Com o módulo: o modo da leitura. O leitor, o servidor põe o único módulo ativo cadastrado. */
  idModo?: number | null;
}
export interface VerificarPecaDto {
  leitura: LeituraDto; ataqueBiologico: AtaqueBiologicoVerificacao; rachadura: RachaduraVerificacao;
  placaAntirrachante: boolean; placaConfere: boolean; resultado: ResultadoVerificacao; motivo?: string | null; fotoArquivo?: string | null; idCliente?: string;
}
export interface GuardarPecaDto { leitura: LeituraDto; idPilha?: number | null; idCliente?: string; }
export interface TrocarPilhaDto { idPilha: number; leitura: LeituraDto; }

/** Um grupo do lote no pátio (Victor, 2026-11-17): as peças de um tamanho e uma classe. */
export interface GrupoDoLoteDto {
  /** id do tamanho (→ TAMANHO_DORMENTE) */ idTamanho: number;
  /** o comprimento, em metros */ comprimentoM: number;
  /** a classe: 1 ou 2 */ classe: number;
  /** aprovadas e sem pilha: as que o botão guarda */ aGuardar: number;
  /** as que já estão numa pilha */ jaNaPilha: number;
  /** reprovadas ou ainda não verificadas */ naoAprovadas: number;
}
/** O lote da peça lida, para guardar tudo de uma vez. */
export interface LoteParaGuardarDto {
  /** id do lote (→ LOTE) */ idLote: number;
  /** o código do lote: L- e o código do pedido */ codigoLote: string;
  /** id da peça da tag lida (→ DORMENTE) */ idDormenteLido: number;
  /** código impresso da peça lida */ codigoLido: string | null;
  /** o armazém onde a peça lida chegou: o sugerido para o lote todo */ idArmazem: number;
  /** quantas peças do lote estão no pátio */ pecas: number;
  /** as peças no pátio, por tamanho e classe */ grupos: GrupoDoLoteDto[];
}
/** Guardar o lote: a tag que achou o lote e a pilha de cada grupo, todas no mesmo armazém. */
export interface GuardarLoteDto {
  /** id do armazém do lote todo (→ ARMAZEM) */ idArmazem: number;
  /** a leitura da tag que achou o lote */ leitura: LeituraDto;
  /** a pilha (→ PILHA) de cada grupo */ grupos: { idTamanho: number; classe: number; idPilha: number }[];
}

export interface PecaConferidaDto { idDormente: number; tag: string | null; pilhaDela: string | null; pilhaOndeRespondeu: string | null; }
export interface ConferenciaDto {
  id: number; idArmazem: number; armazem: string; iniciadaEm: string; concluidaEm: string | null;
  pilhasLidas: string[]; pilhasFaltando: string[]; esperadas: number; lidas: number;
  faltando: PecaConferidaDto[]; aMais: PecaConferidaDto[]; tagsSemCadastro: string[];
}
export interface FotoDaPecaDto { momento: 'entrada' | 'via'; arquivo: string; data: string; descricao: string | null; noServidor: boolean; analiseIa: string | null; }
export interface FotosDaPecaDto { idDormente: number; entrada: FotoDaPecaDto[]; via: FotoDaPecaDto[]; }
