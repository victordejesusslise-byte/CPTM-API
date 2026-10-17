/** Para onde o botão "Verificar" do indicador leva: a tela em que se confere e se resolve o que o número mostra. */
export interface Verificacao {
  rota: string;
  texto: string;
}

export const VERIFICAR: Record<string, Verificacao> = {
  'acerto-camera': { rota: '/alertas', texto: 'Ver os alertas da câmera' },
  'confiabilidade-dados': { rota: '/alertas/sensores', texto: 'Ver os alertas dos sensores' },
  'acerto-ocr': { rota: '/chegadas/placa', texto: 'Ler e conferir placas' },
  'alvo-unico': { rota: '/passagem', texto: 'Ver a passagem da noite' },
  'dias-peca-condenada': { rota: '/trocas-trecho', texto: 'Ver as peças a trocar' },
  'tempo-passos-troca': { rota: '/aprovar', texto: 'Ver as trocas para aprovar' },
  'disponibilidade-registros': { rota: '/integracoes/historico', texto: 'Ver as integrações' },
  'divergencia-nota-chegada': { rota: '/chegadas/entregas', texto: 'Ver as entregas recebidas' },
  'divergencia-conferencia': { rota: '/armazem/conferencia', texto: 'Ver a conferência' },
  'estoque-m3': { rota: '/armazem/estoque', texto: 'Ver o estoque' },
  'falhas-tag-7-noites': { rota: '/alertas/tags', texto: 'Ver as tags' },
  'gravacao-conferida': { rota: '/cadastro', texto: 'Ver o cadastro da peça' },
  'inspecoes-por-dia': { rota: '/inspecoes/feitas', texto: 'Ver as inspeções' },
  'taxa-retrabalho': { rota: '/inspecoes/feitas', texto: 'Ver as inspeções' },
  'tempo-medio-inspecao': { rota: '/inspecoes/feitas', texto: 'Ver as inspeções' },
  'identificado-corretamente': { rota: '/classificar/feitas', texto: 'Ver as classificações' },
  'reprovacao-fornecedor': { rota: '/qualidade/cargas', texto: 'Ver as cargas e boletins' },
  'segunda-classe-pedido': { rota: '/pedidos', texto: 'Ver os pedidos' },
  'notas-das-pecas': { rota: '/trecho', texto: 'Ver o mapa do trecho' },
};
