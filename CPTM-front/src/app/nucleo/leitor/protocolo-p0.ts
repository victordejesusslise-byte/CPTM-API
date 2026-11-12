/**
 * O protocolo do firmware YBYRA-RFID-P0 v0.5 (ESP32-C3 + PN532, Victor 2026-11-11): linhas de texto pela serial USB, 115200, terminadas
 * por \n. Cada comando pode responder várias linhas (o SCAN, por exemplo); aqui só se interpreta o que o firmware já manda, sem
 * inventar comando. Comandos: PING, SCAN, READ, WRITE BRxxxxxxxxxx, VERIFY.
 */

/** O número do YBYRA gravado na etiqueta: BR + 10 dígitos (o mesmo do sistema). */
export const NUMERO_YBYRA = /^BR\d{10}$/;

export type Etiqueta = { uid: string; tipo: 'MIFARE Classic' | 'NFC Type 2'; gravavel: boolean };
export type ResultadoScan = Etiqueta | { erro: string };
export type ResultadoRead = { id: string } | { semId: true } | { erro: string };

/** Quando a resposta de cada comando acabou (a última linha que o firmware manda para ele). */
export const FIM: Record<'PING' | 'SCAN' | 'READ' | 'WRITE', (linha: string) => boolean> = {
  PING: (l) => l.startsWith('PONG;') || l.startsWith('ERRO;'),
  SCAN: (l) => l.startsWith('WRITE;') || l === 'TAG;TYPE=NAO_SUPORTADO' || l.startsWith('ERRO;'),
  READ: (l) => l.startsWith('READ;ID=') || l === 'READ;SEM_ID_YBYRA' || l.startsWith('ERRO;'),
  // A gravação que falha manda vários ERRO (a página, depois o geral): termina no VERIFY;OK ou no erro final.
  WRITE: (l) => l === 'VERIFY;OK' || /^ERRO;(WRITE_OU_VERIFY_FALHOU|UID_NAO_AUTORIZADO_PARA_ESCRITA|FORMATO_ESPERADO|TAG_AUSENTE|UID_DIFERENTE|EXECUTE_SCAN)/.test(l),
};

/** O primeiro erro das linhas (ERRO;CODIGO), sem o que vem depois do código. */
const erroDe = (linhas: string[]): string | null => {
  const e = linhas.find((l) => l.startsWith('ERRO;'));
  return e ? e.slice(5).split(/[;=]/)[0] : null;
};

/** SCAN: o UID, o tipo da etiqueta e se o firmware deixa gravar nela (no P0, só as etiquetas de teste liberadas por UID). */
export function lerScan(linhas: string[]): ResultadoScan {
  const erro = erroDe(linhas);
  if (erro) return { erro };
  if (linhas.includes('TAG;TYPE=NAO_SUPORTADO')) return { erro: 'TIPO_NAO_SUPORTADO' };
  const uid = linhas.find((l) => l.startsWith('TAG;UID='))?.slice(8).split(';')[0];
  const tipo = linhas.includes('TAG;TYPE=MIFARE_CLASSIC_COMPATIVEL') ? 'MIFARE Classic' : linhas.includes('TAG;TYPE=NFC_TYPE2_COMPATIVEL') ? 'NFC Type 2' : null;
  if (!uid || !tipo) return { erro: 'RESPOSTA_INCOMPLETA' };
  return { uid, tipo, gravavel: linhas.includes('WRITE;PERMITIDO') };
}

/** READ: o número do YBYRA gravado, ou que a etiqueta não tem número do YBYRA. */
export function lerRead(linhas: string[]): ResultadoRead {
  const erro = erroDe(linhas);
  if (erro) return { erro };
  const id = linhas.find((l) => l.startsWith('READ;ID='))?.slice(8).trim();
  if (id && NUMERO_YBYRA.test(id)) return { id };
  return { semId: true };
}

/** WRITE: só vale com WRITE;OK do mesmo número e VERIFY;OK (a releitura do próprio firmware). */
export function gravou(linhas: string[], numero: string): boolean {
  return linhas.includes(`WRITE;OK;ID=${numero}`) && linhas.includes('VERIFY;OK') && !erroDe(linhas);
}

const MENSAGENS: Record<string, string> = {
  TAG_AUSENTE: 'Nenhuma etiqueta no alcance. Encoste a peça no módulo (no protótipo, a leitura é de perto).',
  EXECUTE_SCAN: 'O módulo perdeu a etiqueta. Leia de novo.',
  UID_DIFERENTE: 'A etiqueta mudou entre a leitura e a gravação. Leia de novo, com uma etiqueta só perto do módulo.',
  READ_FALHOU: 'O módulo não conseguiu ler a etiqueta. Tente de novo, sem mexer.',
  UID_NAO_AUTORIZADO_PARA_ESCRITA: 'Esta etiqueta não está liberada para gravação no protótipo (só as etiquetas de teste).',
  WRITE_OU_VERIFY_FALHOU: 'A gravação não se confirmou na releitura. Nada foi considerado gravado; tente de novo na mesma etiqueta.',
  NDEF_EXISTENTE_NAO_YBYRA: 'A etiqueta já tem outro conteúdo gravado, que não é do YBYRA. Nada foi alterado.',
  BLOCO_OCUPADO_DESCONHECIDO: 'A etiqueta já tem outro conteúdo gravado, que não é do YBYRA. Nada foi alterado.',
  LAYOUT_DESCONHECIDO: 'O módulo não reconhece a memória desta etiqueta. Nada foi alterado.',
  FORMATO_ESPERADO_BR_MAIS_10_DIGITOS: 'O número precisa ter BR e 10 números (ex.: BR0000000032).',
  PN532_NAO_ENCONTRADO: 'O leitor dentro do módulo não respondeu. Desconecte e conecte o módulo de novo.',
  TIPO_NAO_SUPORTADO: 'Esta etiqueta não é de um tipo que o módulo lê.',
  COMANDO_INVALIDO: 'O módulo não entendeu o pedido. Confira se o firmware é o YBYRA-RFID-P0 v0.5.',
  SEM_RESPOSTA: 'O módulo não respondeu a tempo. Desconecte e conecte o módulo de novo.',
  RESPOSTA_INCOMPLETA: 'A resposta do módulo veio incompleta. Leia de novo.',
};

/** A mensagem para a tela, em português, de cada erro do firmware. */
export const mensagemDoErro = (codigo: string): string => MENSAGENS[codigo] ?? `O módulo respondeu ${codigo}. Leia de novo.`;
