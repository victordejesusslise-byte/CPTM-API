// Serve a versão de produção do front (dist/cptm-front/browser) com os cabeçalhos de segurança. Só o Node, sem dependência nenhuma.
// npm run producao: aberto para a rede, na porta 4300 (http://localhost:4300 no PC, http://SEU-IP:4300 nos outros aparelhos e, pela VPN,
// https://ybyra-pc.tail60ace9.ts.net). A 4200 fica para o ng serve, o modo de desenvolvimento (Victor, 2026-10-31).
// Só neste PC, sem o --rede: node servidor-producao.mjs (depois do ng build).
// Segurança (Victor, 2026-10-23): sem código-fonte legível, sem as ferramentas de depuração, com a política de conteúdo (CSP).
import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { networkInterfaces } from 'node:os';
import { extname, join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const pasta = resolve(fileURLToPath(new URL('.', import.meta.url)), 'dist', 'cptm-front', 'browser');
const rede = process.argv.includes('--rede');
const porta = 4300;

const tipos = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.txt': 'text/plain; charset=utf-8',
  '.ico': 'image/x-icon',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.svg': 'image/svg+xml',
  '.webp': 'image/webp',
  '.woff2': 'font/woff2',
  '.webmanifest': 'application/manifest+json',
};

const indice = await readFile(join(pasta, 'index.html'), 'utf8').catch(() => {
  console.error('Não achei o build de produção em dist/. Rode pelo npm run producao, que gera o build antes.');
  process.exit(1);
});

// Os scripts liberados são os da CSP automática do Angular (a meta no index.html, com o hash de cada script). O cabeçalho repete
// a mesma regra e acrescenta o que só o servidor consegue mandar, como o frame-ancestors (o site não abre dentro de outra página).
const scripts = indice.match(/script-src[^;"]*/)?.[0] ?? "script-src 'self'";
const politica = [
  "default-src 'self'",
  scripts,
  // A API fica no mesmo endereço, na porta 8045 (src/environments/ambiente.ts); o ao vivo (/hub, SignalR) vai por WebSocket na mesma porta.
  "connect-src 'self' http://*:8045 https://*:8045 ws://*:8045 wss://*:8045",
  "img-src 'self' data: blob:",
  "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
  "font-src 'self' https://fonts.gstatic.com",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "frame-ancestors 'none'",
].join('; ');

const cabecalhos = {
  'Content-Security-Policy': politica,
  'X-Content-Type-Options': 'nosniff',
  'X-Frame-Options': 'DENY',
  'Referrer-Policy': 'no-referrer',
};

const servidor = createServer(async (pedido, resposta) => {
  try {
    const caminho = decodeURIComponent(new URL(pedido.url ?? '/', 'http://x').pathname);
    let arquivo = resolve(pasta, '.' + caminho);
    // Nada fora da pasta do build.
    if (arquivo !== pasta && !arquivo.startsWith(pasta + sep)) {
      resposta.writeHead(404, cabecalhos).end();
      return;
    }
    const existe = await stat(arquivo).then((s) => s.isFile(), () => false);
    // Rota do Angular (sem extensão): devolve o index.html.
    if (!existe) {
      if (extname(caminho)) {
        resposta.writeHead(404, cabecalhos).end();
        return;
      }
      arquivo = join(pasta, 'index.html');
    }
    const ehIndice = arquivo.endsWith('index.html');
    resposta.writeHead(200, {
      ...cabecalhos,
      'Content-Type': tipos[extname(arquivo)] ?? 'application/octet-stream',
      // Os arquivos do build têm o hash no nome: guardam por um ano. O index.html sempre vem de novo.
      'Cache-Control': ehIndice ? 'no-cache' : 'public, max-age=31536000, immutable',
    });
    resposta.end(ehIndice ? indice : await readFile(arquivo));
  } catch {
    resposta.writeHead(400, cabecalhos).end();
  }
});

servidor.listen(porta, rede ? '0.0.0.0' : '127.0.0.1', () => {
  console.log(`YBYRA (produção) em http://localhost:${porta}`);
  if (rede)
    for (const ip of Object.values(networkInterfaces()).flat().filter((i) => i && i.family === 'IPv4' && !i.internal))
      console.log(`No celular, no mesmo Wi-Fi: http://${ip.address}:${porta}`);
});
