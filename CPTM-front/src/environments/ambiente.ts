/**
 * Onde está a API do CPTM-back: no mesmo endereço e com o mesmo protocolo por onde o site foi aberto, na porta 8045.
 * No PC dá http://localhost:8045/api; no celular (http://192.168.x.x:4200) dá http://192.168.x.x:8045/api; aberto por https, a API
 * também vai por https (em produção a API recusa senha e token sem criptografia). Em produção, troque pelo endereço do servidor.
 */
const local = globalThis.location;
export const ambiente = {
  api: `${local?.protocol === 'https:' ? 'https:' : 'http:'}//${local?.hostname || 'localhost'}:8045/api`,
};
