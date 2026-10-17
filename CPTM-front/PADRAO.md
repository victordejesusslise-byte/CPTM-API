# Padrão do projeto (front)

- **Stack:** Angular 22 (componentes standalone, signals, controle de fluxo `@if`/`@for`), TypeScript 6, SCSS. Testes com `ng test` (o padrão do Angular 22: Vitest com jsdom), specs ao lado de cada arquivo.
- **Tipo:** web, cliente da API `CPTM-back` (http://localhost:5169/api). Não tem regra de negócio: a tela mostra, o service chama a API, a API decide.
- **Visual:** a marca **YBYRA** (PDF da marca em `2 Oficina do Claude/Arquivos/fontes/marca-ybyra.pdf`): paleta madeira #563624, areia #D5C4A1, verde #64786E, mata #1E2A20 e vinho #512027 sobre o creme #F1E9DF; Josefin Sans em tudo (Tribo e Obra Letra não estão no Google Fonts); logo dos anéis da tora em `compartilhado/componentes/marca`. Tokens em `src/styles.scss`. Sempre o tema claro (creme). Regras do protótipo mantidas: o vinho é a cor da exceção, alvo de toque de 44 px. Angular Material (MIT) nos formulários, diálogos e tabelas, embrulhado em `compartilhado/` (`ui.ts` e os componentes `tabela`, `estado`, `leitura-tag`): as telas não importam a biblioteca direto. **Sem PrimeNG:** a versão 22 virou licença paga e a CPTM não cabe na gratuita.
- **Pastas:**

```
CPTM-front/src/app/
├── app.ts · app.config.ts · app.routes.ts   só inicia: provedores, rotas (carregadas por tela)
├── <parte>/        uma pasta por parte do sistema, com os mesmos nomes do back:
│   │               pedidos, chegadas, pecas, armazens, campo, via, ambiental, integracoes,
│   │               laudos, pessoas, catalogos, indicadores, apresentacao
│   ├── paginas/    as telas da parte (uma pasta por tela: html, ts, scss e spec juntos)
│   ├── servicos/   as chamadas HTTP da parte (uma por assunto da API)
│   └── entidades/  os DTOs da parte, em TypeScript (mesmos nomes de campo do JSON)
├── nucleo/         o que vale para o app inteiro
│   ├── sessao/     token JWT e usuário
│   ├── guardas/    guarda de rota por cargo
│   ├── interceptadores/  o token em cada chamada
│   └── layout/     casca (topo e menu lateral), menu de cada cargo, tema (modo escuro)
└── compartilhado/  o que as partes reaproveitam
    ├── componentes/  tabela, estado, nota, leitura da tag, marca
    ├── entidades/    os enums do back
    ├── utilitarios/  funções puras (formatar m³, km, datas, consulta, arquivo, foto)
    ├── testes/       sessão de teste
    └── ui.ts         os embrulhos do Angular Material
```

- **Dependência:** `paginas` → `servicos` → API; `paginas` e `servicos` usam as `entidades` da parte e o `compartilhado`; uma parte pode usar o serviço de outra (ex.: a chegada usa o de pedidos); `nucleo` e `compartilhado` servem a todos. A tela nunca chama `HttpClient` direto. Tela nova vai em `<parte>/paginas/<tela>/`.
- **Regras extras:**
  1. Nomes como no Angular 22: arquivo sem sufixo (`login.ts`, classe `Login`), gerados com `ng generate`.
  2. Cargos iguais aos do back: `Supervisor`, `Tecnico`, `Especialista`, `OperadorPatio`, `OperadorVia`. O menu e a guarda de rota usam o cargo do token; a API confere de novo.
  3. Mensagem de erro da API (`detail` do ProblemDetails) aparece como veio, em português.
  4. A cor de exceção (vinho, token `--vermelho`) é só para erro, divergência e o que pede ação.
  5. Código no GitHub: repositório privado `CPTM-API` (back e front juntos; o `.git` fica na raiz do DORMENTE com um `.gitignore` que só deixa entrar `CPTM-back/` e `CPTM-front/`).

## Histórico

- 2026-10-16 — Por parte, depois por tipo (pedido do Victor): as telas, os serviços e os modelos foram para uma pasta por parte, com os mesmos nomes do back, cada uma com `paginas/`, `servicos/` e `entidades/`; `core` virou `nucleo` e `shared`+`helpers` viraram `compartilhado`. Só mudaram o lugar dos arquivos e os imports; os 43 testes passaram iguais.

- 2026-10-11 — Pedidos como no protótipo YBYRA: o item do menu abre as abas Cadastrar pedido e Acompanhar pedidos (`subs` em `core/menu/menu.ts`; no celular, abas no alto). Tokens iguais aos do protótipo. 31 testes.
- 2026-10-11 — Marca YBYRA aplicada (PDF do Victor): paleta, Josefin Sans, logo. PrimeNG retirado (licença paga; decisão do Victor): tabelas no Angular Material. Telas do fluxo da peça: pedidos (lista, novo, aberto com notas, chegadas, BR em PDF, cancelamento), chegada, cadastro da peça, qualidade, armazém (estoque, pilha, conferência) e consulta da peça. 28 testes.
- 2026-10-11 — Criação: Angular 22.2.2, Angular Material 22.2.2, PrimeNG 22.1.2. Decisões do Victor: as três bases visuais, telas de login, fluxo da peça, campo e supervisor, pastas por etapa, `ng test` padrão.
