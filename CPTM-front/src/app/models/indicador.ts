export interface DetalheIndicadorDto {
  rotulo: string;
  valor: string;
}

/** Um indicador do painel. valor nulo: ainda não há dado. */
export interface IndicadorDto {
  chave: string;
  nome: string;
  valor: number | null;
  unidade: string;
  resumo: string;
  referencia: string | null;
  vermelho: boolean;
  detalhes: DetalheIndicadorDto[];
}