# CPTM-API

Projeto de análise e controle de estoque de dormentes: **YBYRA**, qualidade e rastreamento de dormentes ferroviários (CPTM, Via Permanente).

| Pasta | O que é | Como rodar |
|---|---|---|
| [`CPTM-back`](CPTM-back) | API em C# com .NET 8 (handlers, DTOs, helpers), EF Core e SQL Server | `dotnet run --project CPTM-back/src/ViaPermanente.Api` · testes: `dotnet test CPTM-back` |
| [`CPTM-front`](CPTM-front) | Front em Angular 22 com Angular Material, na marca YBYRA | `cd CPTM-front` · `npm install` · `npx ng serve` · testes: `npx ng test` |

- Os segredos ficam no `CPTM-back/.env`, que **não** vai para o git. Copie `CPTM-back/.env.exemplo` e preencha.
- O passo a passo do banco, as rotas e os modelos de planilha estão em [`CPTM-back/README.md`](CPTM-back/README.md).
- O padrão de cada parte está em `CPTM-back/PADRAO.md` e `CPTM-front/PADRAO.md`.
