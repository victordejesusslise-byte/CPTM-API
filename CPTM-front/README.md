# CPTM-front · YBYRA

O site do YBYRA (controle de dormentes da CPTM, Via Permanente), em Angular 22. Fala com a API do `CPTM-back` na porta 8045. Padrão do código: [PADRAO.md](PADRAO.md).

## Precisa ter

- [Node.js 24 LTS](https://nodejs.org/)
- A API ligada (no Docker ou com `dotnet run`; veja o README do `CPTM-back`)

## Comandos

Todos dentro de `CPTM-front`.

| Para quê | Comando |
|---|---|
| Instalar as dependências (a primeira vez e depois de mudar o `package.json`) | `npm install` |
| Ligar o site no PC (abre em http://localhost:4200) | `npx ng serve` |
| Ligar o site aberto para o celular no mesmo Wi-Fi (abre em `http://SEU-IP:4200`) | `npm run celular` |
| Ligar o site **fechado**, na versão de produção (para apresentar; abre em http://localhost:4200) | `npm run producao` |
| O mesmo, aberto para o celular no mesmo Wi-Fi | `npm run producao-celular` |
| Rodar os testes uma vez (Vitest) | `npx ng test --watch=false` |
| Rodar os testes e continuar olhando as mudanças | `npx ng test` |
| Gerar a versão de produção (vai para `dist/`) | `npx ng build` |

## Desenvolvimento e produção

O `npx ng serve` e o `npm run celular` são o modo de **desenvolvimento**: mostram o código-fonte inteiro no DevTools (F12) e recarregam sozinhos quando o código muda. Para apresentar ou usar de verdade, use o `npm run producao` (ou o `producao-celular`): o código sai compactado, sem o mapa do código-fonte, com a política de segurança de conteúdo (CSP), e sem as ferramentas de depuração do Angular.

Mesmo assim, quem tem o site aberto sempre consegue mexer no que o navegador recebeu, por exemplo mostrar um botão escondido. Por isso quem decide o que cada cargo pode fazer é a API, que confere o token e o cargo em toda chamada. O front só esconde o que não é do cargo. O cargo vem de dentro do token, que é assinado pela API, e é conferido com a API toda vez que o sistema abre. Quem troca o cargo guardado no navegador é desconectado na hora.

## Entrar

Abra http://localhost:4200 e entre com o e-mail e a senha do supervisor do `.env` do back. O login vale 8 horas; "Sair" encerra a sessão em todos os aparelhos.

Depois de uma atualização de segurança, todos precisam entrar de novo uma vez: os logins de antes deixam de valer.

## Onde fica cada coisa

`src/app/<parte>/` com `paginas/`, `servicos/` e `entidades/` (as mesmas partes do back: pedidos, chegadas, pecas, armazens, campo, via, laudos, indicadores…); `nucleo/` (sessão, guardas, interceptadores, layout) e `compartilhado/` (componentes, como o skeleton e a folha do laudo, e utilitários). O endereço da API está em `src/environments/ambiente.ts`: usa o mesmo endereço e protocolo por onde o site foi aberto, na porta 8045.
