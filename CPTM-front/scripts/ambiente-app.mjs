// Escreve o endereço da API do APK (homol ou prod) a partir das variáveis HOMOL_API_URL e PROD_API_URL (no GitHub, "Variables").
// Uso: node scripts/ambiente-app.mjs homol | prod. Sem a variável, fica o endereço que já está no arquivo.
// Também escreve a versão do APK (APP_VERSAO_NOME, que o GitHub monta), para o app saber se tem versão nova.
import { readFileSync, writeFileSync } from 'node:fs';

const ambiente = process.argv[2] === 'prod' ? 'prod' : 'homol';
const url = process.env[ambiente === 'prod' ? 'PROD_API_URL' : 'HOMOL_API_URL'];
const arquivo = new URL(`../src/environments/ambiente.${ambiente}.ts`, import.meta.url);
const versao = process.env.APP_VERSAO_NOME;
if (versao) writeFileSync(arquivo, readFileSync(arquivo, 'utf8').replace(/versao: '[^']*'/, `versao: '${versao}'`));
if (url) {
  const api = url.replace(/\/+$/, '').replace(/\/api$/, '') + '/api';
  writeFileSync(arquivo, readFileSync(arquivo, 'utf8').replace(/api: '[^']*'/, `api: '${api}'`));
  console.log(`APK ${ambiente}: API em ${api}`);
} else {
  console.log(`APK ${ambiente}: endereço do arquivo (${readFileSync(arquivo, 'utf8').match(/api: '([^']*)'/)[1]})`);
}
