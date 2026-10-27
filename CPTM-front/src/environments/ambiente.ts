/**
 * Onde está a API do CPTM-back: no mesmo endereço e com o mesmo protocolo por onde o site foi aberto, na porta 8045.
 * No PC dá http://localhost:8045/api; no celular (http://192.168.x.x:4200) dá http://192.168.x.x:8045/api; aberto por https, a API
 * também vai por https (em produção a API recusa senha e token sem criptografia). Em produção, troque pelo endereço do servidor.
 * Pelo Tailscale (https://ybyra-pc.xxx.ts.net) a API vem pelo mesmo endereço, em /api: o "tailscale serve" repassa para a 8045.
 */
const local = globalThis.location;
const pelaTailnet = local?.hostname?.endsWith('.ts.net') ?? false;
export const ambiente = {
  /** O site não tem versão de app (o aviso de versão nova é só do APK). */
  versao: '',
  api: pelaTailnet
    ? `${local.origin}/api`
    : `${local?.protocol === 'https:' ? 'https:' : 'http:'}//${local?.hostname || 'localhost'}:8045/api`,
};
