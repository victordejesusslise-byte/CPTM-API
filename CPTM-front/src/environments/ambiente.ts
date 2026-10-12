/**
 * Onde está a API do CPTM-back: no mesmo endereço por onde o site foi aberto, na porta 5169.
 * No PC dá http://localhost:5169/api; no celular (http://192.168.x.x:4200) dá http://192.168.x.x:5169/api.
 * Em produção, troque pelo endereço do servidor.
 */
export const ambiente = {
  api: `http://${globalThis.location?.hostname || 'localhost'}:5169/api`,
};