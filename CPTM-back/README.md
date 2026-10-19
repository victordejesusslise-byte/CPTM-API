# CPTM-back · Via Permanente API

Back-end do controle de dormentes (CPTM, Linha 9-Esmeralda). Padrão do código: [PADRAO.md](PADRAO.md).

## Comandos

| Para quê | Comando |
|---|---|
| Ligar a API no PC (http://localhost:8045) | `dotnet run --project src/ViaPermanente.Api --launch-profile http` |
| Ver se a API subiu e alcança o banco | `curl http://localhost:8045/hc` |
| Rodar todos os testes | `dotnet test` |
| Rodar só os testes que não usam o banco | `dotnet test --filter "Categoria!=Banco"` |
| Rodar os testes no Docker | `docker compose --profile testes run --rm testes` |
| Subir o back e o banco no Docker | `docker compose up -d --build` |
| Logs do back no Docker | `docker compose logs -f back` |
| Parar o Docker | `docker compose down` |
| Criar ou atualizar o banco (sem Docker) | `dotnet dotnet-ef database update --project src/ViaPermanente.Dados --startup-project src/ViaPermanente.Api` |
| Criar uma migração | `dotnet dotnet-ef migrations add NomeDaMudanca --project src/ViaPermanente.Dados --startup-project src/ViaPermanente.Api` |

## Precisa ter

- .NET SDK 8
- SQL Server Express (`localhost\SQLEXPRESS`), com o login do Windows

## Primeira vez

1. Copie `.env.exemplo` para `.env` e troque a chave do JWT e a senha do supervisor inicial.
2. Restaure a ferramenta do EF e crie o banco:

```bash
dotnet tool restore
dotnet dotnet-ef database update --project src/ViaPermanente.Dados --startup-project src/ViaPermanente.Api
```

## Rodar

```bash
dotnet run --project src/ViaPermanente.Api --launch-profile http
```

Swagger: http://localhost:8045/swagger. Faça `POST /api/auth/login` com o e-mail e a senha do supervisor do `.env`, clique em **Authorize** e cole o token.

Na primeira subida, se o banco não tem usuário, a API cria o supervisor do `.env`. Depois, o supervisor cadastra cada pessoa em `POST /api/usuarios`, cada uma com a própria senha.

## Apresentar o fluxo

No Swagger, logado como supervisor:

1. `GET /api/apresentacao/fluxo`: as cinco etapas (pedido, chegada, cadastro, qualidade, armazém) e quantas linhas cada uma tem no banco.
2. `POST /api/apresentacao/exemplo`: cria um pedido de 28 peças que passa por todas as etapas (2 reprovadas na qualidade, 26 guardadas na pilha A01 de Santo Amaro) e devolve o fluxo dele.
3. `GET /api/apresentacao/fluxo?idPedido=<o id que voltou>`: o fluxo só daquele pedido.
4. `GET /api/catalogos/armazens`: as pilhas com as peças guardadas.

## Caminho da peça pela API

1. `POST /api/catalogos/leitores` (uma vez): o módulo do celular, tipo 2.
2. `POST /api/pedidos`, depois `POST /api/pedidos/{id}/notas`.
3. `POST /api/pedidos/{id}/chegadas` com os feixes: volta o id de cada feixe e a conferência.
4. Para cada peça: `GET /api/tags/proxima` e `POST /api/dormentes` com o feixe, a tag gravada e a relida.
5. `POST /api/dormentes/{id}/verificacao` com a leitura da tag: aprovada ou reprovada (motivo e foto).
6. `POST /api/dormentes/{id}/armazem` com a leitura: vai para a pilha do tamanho e da classe.
7. `GET /api/estoque`.

## Planilha de notas (modelo provisório)

Uma linha por item; as linhas com o mesmo Pedido e a mesma Nota viram uma nota. Baixe o modelo em `GET /api/integracoes/notas/modelo`.

| Pedido | Nota | Emissão | Valor | Espécie | Comprimento (m) | Classe | Quantidade | Volume (m³) |
|---|---|---|---|---|---|---|---|---|
| PED-123 | NF-10 | 05/10/2026 | 15000 | Ocotea neesiana | 2,80 | 1 | 14 | 1,59936 |

O cabeçalho é achado pelo nome (sem acento e sem a unidade). A nota com linha inválida não entra, e o motivo vai para o log. Quando chegar o modelo real da planilha da API deles, muda só o `PlanilhaNotaRepository`.

## OCR da placa

O Tesseract roda no servidor com `tessdata/por.traineddata` (português padrão, versão rápida, 1,89 MB, de github.com/tesseract-ocr/tessdata_fast; é o modelo versão 1). As fotos vão para `fotos/` (fora do git); `Fotos__Pasta` e `Ocr__Tessdata` no `.env` mudam os lugares. As DLLs do Tesseract precisam do Visual C++ 2019 no servidor.

## Ver no SQL Server Management Studio

- **Server name:** `localhost\SQLEXPRESS`
- **Authentication:** Windows Authentication
- Marque **Trust server certificate**
- Banco: **ViaPermanente** (em Databases); as tabelas aparecem como `dbo.PEDIDO`, `dbo.DORMENTE`…

O banco `ViaPermanente_Testes` só existe enquanto os testes rodam.

## Testes

```bash
dotnet test
```

Os testes da categoria `Banco` usam o SQL Server de verdade (criam e apagam `ViaPermanente_Testes`). Para rodar só os outros:

```bash
dotnet test --filter "Categoria!=Banco"
```

No Docker, sem precisar de .NET nem de SQL Server no PC (usa o SQL Server do compose; a imagem de teste traz o Tesseract e as fontes do Linux):

```bash
docker compose --profile testes run --rm testes
```

Os testes do banco leem a conexão de `VIA_TESTE_CONEXAO`; sem ela, usam o SQL Server Express local.

## Segurança

- O token vale 8 horas (`Jwt__MinutosValidade`) e leva a versão da sessão: "Sair" (`POST /api/auth/sair`) e desativar a conta derrubam todos os tokens dela na hora.
- Login: no máximo `Seguranca__TentativasPorMinuto` (10) por aparelho; depois de 5 senhas erradas seguidas, a conta espera 30 s, 1 min e 5 min.
- `Seguranca__ExigirHttps`: ligado no `appsettings.json` (produção) e desligado no Development e no Docker local. Ligado, a API recusa HTTP vindo da rede (menos o `/hc`).
- Detalhes: `src/ViaPermanente.Api/Comum/Seguranca.cs`.

## Mudou o banco

```bash
dotnet dotnet-ef migrations add NomeDaMudanca --project src/ViaPermanente.Dados --startup-project src/ViaPermanente.Api
```

Para rodar SQL à mão no `sqlcmd`, use `-I` (os índices filtrados exigem `QUOTED_IDENTIFIER`).

## Rotas de hoje

| Rota | Quem |
|---|---|
| `GET /hc` (o back subiu e alcança o banco: 200 ou 503) | aberto |
| `POST /api/auth/login` (10 por minuto por aparelho; espera crescente por conta) | aberto |
| `POST /api/auth/sair` (encerra a sessão em todos os aparelhos) | logado |
| `GET /api/auth/eu` | logado |
| `GET /api/catalogos/tamanhos` · `GET /api/catalogos/especies` · `GET /api/catalogos/armazens` | logado |
| `GET /api/pedidos` · `GET /api/pedidos/{id}` · `POST /api/pedidos` | supervisor e operador de pátio |
| `POST /api/pedidos/verificacao` (2ª classe e oferta do fornecedor, sem gravar) | supervisor e operador de pátio |
| `GET /api/pedidos/{id}/notas` · `POST /api/pedidos/{id}/notas` (nota conferida na hora: Confere ou Diverge) | supervisor e operador de pátio |
| `GET /api/ofertas` (opcional `?idEmpresa=`) · `POST /api/ofertas` | supervisor e operador de pátio |
| `POST /api/pedidos/{id}/cancelamento` (com motivo, só antes da chegada) | supervisor e operador de pátio |
| `POST /api/pedidos/{id}/correcao` (contrato, data e itens, com motivo, só antes da chegada) · `GET /api/pedidos/{id}/historico` | supervisor e operador de pátio |
| `GET /api/pedidos/{id}/chegadas` · `POST /api/pedidos/{id}/chegadas` (chegada com os feixes; a primeira cria o lote) | supervisor e operador de pátio |
| `GET /api/tags/proxima` (o número para o módulo gravar) | supervisor e operador de pátio |
| `POST /api/dormentes` (cadastro com a tag: 201 com a peça, 409 se a gravação não vale) | supervisor e operador de pátio |
| `POST /api/dormentes/{id}/verificacao` (qualidade, com a peça lida) | supervisor e especialista |
| `POST /api/dormentes/{id}/armazem` (entrada da aprovada na pilha) | supervisor e operador de pátio |
| `GET /api/dormentes/{id}` · `GET /api/dormentes?idFeixe=` · `GET /api/tags/{numero}` · `GET /api/estoque` | logado |
| `GET /api/catalogos/leitores` · `GET /api/catalogos/modos-leitura` · `POST /api/catalogos/leitores` (supervisor) | logado |
| `POST /api/passagens` (a passagem noturna: leituras e detecções; abre os alertas) | o carro, com o cabeçalho `X-Chave-Carro` (chave em `Integracao__ChaveCarro` no `.env`), ou o supervisor |
| `GET /api/alertas?origem=&status=` · `GET /api/alertas/{id}` | técnico, operador da via e supervisor |
| `POST /api/alertas/{id}/vistoria` (com foto e a tag lida) | técnico e operador da via |
| `POST /api/alertas/{id}/descarte` ("não confere", com motivo) | técnico |
| `POST /api/inspecoes` (checklist v2; fecha os alertas da peça) | técnico |
| `GET /api/inspecoes/{id}` · `GET /api/inspecoes?idDormente=` | logado |
| `POST /api/inspecoes/{id}/correcao` (só quem fez, com motivo) | técnico |
| `POST /api/trocas` (pedir) | técnico |
| `POST /api/trocas/{id}/decisao` (aprovar ou reprovar; o cargo vem de `troca_quem_aprova`) | hoje, supervisor |
| `GET /api/trocas/{id}/compativeis` · `POST /api/trocas/{id}/separacao` | operador de pátio |
| `POST /api/trocas/{id}/execucao` · `POST /api/trocas/{id}/recusa` | operador da via |
| `POST /api/trocas/{id}/recusa/decisao` (manter ou aceitar) | técnico |
| `GET /api/trocas?status=` · `GET /api/trocas/{id}` | logado |
| `GET /api/empresas?papel=` · `GET /api/contratos?idEmpresa=` · `GET /api/estacoes` · `GET /api/trechos` | logado |
| `POST /api/empresas` · `PUT /api/empresas/{id}/ativo?ativo=` · `POST /api/contratos` · `POST /api/estacoes` · `POST /api/trechos` | supervisor |
| `GET /api/dormentes/{id}/historico` (a história da peça, em ordem de data) | logado |
| `POST /api/dormentes/{id}/pilha` (troca de pilha, lendo a peça) | supervisor e operador de pátio |
| `POST /api/armazens/{id}/conferencias` · `POST /api/conferencias/{id}/pilhas` · `POST /api/conferencias/{id}/conclusao` · `GET /api/conferencias/{id}` | operador de pátio, especialista e supervisor |
| `POST /api/tags/avisos` (tag sem cadastro em campo) | operador da via e técnico |
| `GET /api/tags/avisos` · `POST /api/tags/vinculos` (vincular a tag lida no local) | técnico e supervisor |
| `POST /api/integracoes/notas` (planilha .xlsx, campo `arquivo`) · `GET /api/integracoes/notas/modelo` · `GET /api/integracoes/log` | supervisor e operador de pátio |
| `GET /api/integracoes/erp/estoque` (planilha do estoque com o código de material) · `GET /api/integracoes/codigos-material` · `POST /api/integracoes/codigos-material` (supervisor) | supervisor e operador de pátio |
| `GET /api/relatorios-ambientais/{id}/planilha` (planilha para o IBAMA) | especialista e supervisor |
| `POST /api/ocr/placas` (foto da placa, campo `foto`, e `idPedido`, obrigatório) · `GET /api/ocr/placas?idPedido=` (as placas confirmadas do pedido, ainda sem feixe) · `GET /api/ocr/placas/{id}` · `POST /api/ocr/placas/{id}/confirmacao` · `GET /api/ocr/modelos` | operador de pátio e supervisor |
| `GET /api/indicadores?de=&ate=` (os 18 indicadores; sem datas, os últimos 30 dias) | logado |
| `GET /api/laudos/recebimento/{idRecebimento}` (BR) · `/api/laudos/inspecao/{id}` (LI) · `/api/laudos/liberacao/{idTroca}` (LB) · `/api/laudos/troca/{idTroca}` (LD), em PDF | logado |
| `GET /api/laudos/{tipo}/{id}/documento` (o mesmo laudo como documento, para a tela mostrar antes de baixar) | logado |
| `GET /api/manutencao/leituras-via` (as semanas) · `POST /api/manutencao/leituras-via` (roda a manutenção agora; ela também roda sozinha todo dia) | supervisor |
| `GET /api/relatorios-ambientais` · `POST /api/relatorios-ambientais?mes=2026-10-01` (gera o rascunho) · `POST /api/relatorios-ambientais/{id}/envio` (congela) | especialista e supervisor |
| `GET /api/apresentacao/fluxo` (opcional `?idPedido=`) | logado |
| `POST /api/apresentacao/exemplo` (só em desenvolvimento) | supervisor |
| `GET /api/parametros` | logado |
| `PUT /api/parametros/{chave}` | supervisor |
| `GET /api/usuarios` · `GET /api/usuarios/{id}` · `POST /api/usuarios` · `PUT /api/usuarios/{id}/ativo` (desativar derruba as sessões; ninguém desativa a si mesmo) | supervisor |
