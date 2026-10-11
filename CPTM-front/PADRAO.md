# Padrão do projeto (front)

- **Stack:** Angular 22 (componentes standalone, signals, controle de fluxo `@if`/`@for`), TypeScript 6, SCSS. Testes com `ng test` (o padrão do Angular 22: Vitest com jsdom), specs ao lado de cada arquivo.
- **Tipo:** web, cliente da API `CPTM-back` (http://localhost:5169/api). Não tem regra de negócio: a tela mostra, o service chama a API, a API decide.
- **Visual:** a base é o sistema visual do protótipo (tokens em `src/styles.scss`: cores, Archivo nos títulos, Source Sans 3 no texto, alvo de toque de 44 px, vermelho só para exceção, cinza #6B6B6B nos rótulos). Angular Material nos formulários e diálogos; PrimeNG nas tabelas. Os dois ficam embrulhados em `shared/`: as telas não importam Material nem PrimeNG direto.
- **Pastas:**

```
CPTM-front/src/app/
├── app.ts · app.config.ts · app.routes.ts   só inicia: provedores, rotas (carregadas por etapa)
├── core/          o que vale para o app inteiro
│   ├── auth/      sessão (token JWT e usuário), interceptor do token, guarda de rota por cargo
│   └── menu/      o menu de cada cargo
├── shared/        componentes do sistema visual (casca, cartão, tabela, campo, botão…) e os embrulhos de Material e PrimeNG
├── features/      uma pasta por etapa ou tela: login, painel, pedidos, chegada, cadastro, qualidade, armazem, alertas, inspecao, trocas, supervisor…
├── services/      uma por assunto da API (pedidos, chegadas, dormentes…): só chamadas HTTP
├── models/        os DTOs do back, em TypeScript (mesmos nomes de campo do JSON)
└── helpers/       funções puras (formatar m³, km, datas, cargo…)
```

- **Dependência:** `features` → `services` → API; `features` e `services` usam `models` e `helpers`; `core` e `shared` servem a todos. A tela nunca chama `HttpClient` direto.
- **Regras extras:**
  1. Nomes como no Angular 22: arquivo sem sufixo (`login.ts`, classe `Login`), gerados com `ng generate`.
  2. Cargos iguais aos do back: `Supervisor`, `Tecnico`, `Especialista`, `OperadorPatio`, `OperadorVia`. O menu e a guarda de rota usam o cargo do token; a API confere de novo.
  3. Mensagem de erro da API (`detail` do ProblemDetails) aparece como veio, em português.
  4. Vermelho é exceção: só para erro, divergência e o que pede ação.

## Histórico

- 2026-10-11 — Criação: Angular 22.2.2, Angular Material 22.2.2, PrimeNG 22.1.2. Decisões do Victor: as três bases visuais, telas de login, fluxo da peça, campo e supervisor, pastas por etapa, `ng test` padrão.
