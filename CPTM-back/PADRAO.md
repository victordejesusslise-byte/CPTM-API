# Padrão do projeto

- **Stack:** C# com .NET 8 (SDK 8.0.425), ASP.NET Core Web API no padrão MVC (os controllers se chamam *Handler*), EF Core 8 com migrações, SQL Server Express (`localhost\SQLEXPRESS`), xUnit. Nada em Python.
- **Tipo:** API web. O front é Angular, em `../CPTM-front` (semana 3), e conversa com a API só por JSON (DTOs).
- **Pastas:**

```
CPTM-back/
├── ViaPermanente.sln
├── PADRAO.md                 este arquivo
├── README.md                 como rodar
├── .env.exemplo              modelo dos segredos (o .env de verdade fica fora do git)
├── .config/dotnet-tools.json dotnet-ef 8 local (dotnet dotnet-ef ...)
├── src/
│   ├── ViaPermanente.Api/        o "geral": a porta de entrada
│   │   ├── Program.cs            só inicia: lê o .env, registra dependências, segurança, sobe
│   │   ├── appsettings.json      configuração SEM segredo
│   │   ├── Handlers/             controllers do MVC: recebem HTTP, chamam o service, respondem
│   │   ├── Helpers/              TokenHelper (gera e valida o JWT)
│   │   └── Models/               formato da configuração (JwtOpcoes)
│   ├── ViaPermanente.Dominio/    regras, sem saber de HTTP nem de banco
│   │   ├── Models/<Grupo>/       as 51 tabelas, uma classe por tabela, e os enums
│   │   ├── Dtos/<Assunto>/       o que entra e sai da API
│   │   ├── Services/             regra de negócio
│   │   ├── Repositories/         interfaces do acesso a dados (IUsuarioRepository...)
│   │   └── Helpers/              funções puras por assunto (SenhaHelper, EmailHelper, VolumeHelper)
│   └── ViaPermanente.Dados/      o SQL Server
│       ├── ViaPermanenteContext.cs   DbContext e convenções (nomes, tipos, cascata)
│       ├── Configuracoes/        chaves, ligações, índices e carga inicial, um arquivo por grupo
│       ├── Repositories/         implementação das interfaces do Dominio, com EF Core
│       ├── Helpers/              NomeBancoHelper (nome de tabela e coluna, precisão)
│       └── Migrations/           geradas pelo dotnet-ef; nunca editar à mão
└── tests/ViaPermanente.Testes/   xUnit: Helpers, Services (repositório falso) e Banco (modelo do EF)
```

Os grupos de `Models` e `Configuracoes` são os mesmos do cérebro: Catalogos, Pessoas, Compras, Chegada, Patio, Via, Campo e Ambiental.

- **Dependência:** `Api → Dominio`, `Dados → Dominio`, `Api → Dados` só para registrar no `Program.cs`. Dentro: `Handlers → Services → Repositories (interface)`; a implementação está em `Dados`. `Models`, `Dtos` e `Helpers` servem a todos e não dependem de ninguém.

## Regras extras

1. **Banco igual ao cérebro.** Tabela em `MAIUSCULAS_COM_SUBLINHADO`, coluna em `minusculas_com_sublinhado`, gerados do nome da classe e da propriedade pelo `NomeBancoHelper`. Nunca escrever `[Table]` ou `[Column]` à mão.
2. **Tabela nova ou coluna nova:** classe em `Dominio/Models/<Grupo>/`, chaves e ligações em `Dados/Configuracoes/<Grupo>Configuracao.cs`, depois `dotnet dotnet-ef migrations add <Nome>`. Mudou o banco, muda a nota da tabela no cérebro (`3 Integração/Projeto/20 Banco/`).
3. **Sem cascata.** Toda chave estrangeira é `Restrict`. Tabela de evento só recebe inserção.
4. **Enum vira texto no banco e no JSON** (`AguardandoPeca`, não `2`).
5. **Registro de campo** (`IRegistroDeCampo`): `id_cliente` único quando preenchido; o mesmo envio do celular entra uma vez.
6. **LEITURA_VIA** tem chave `(id_leitura_via, data)` e ninguém aponta para ela com chave estrangeira (a partição semanal descarta as leituras velhas).
7. **DTO na fronteira.** O handler recebe e devolve DTO, nunca a classe do banco. Senha e hash nunca saem da API.
8. **Segredo só no .env.** Conexão, chave do JWT e supervisor inicial. `appsettings.json` não tem segredo.
9. **Segurança:** tudo exige login (política padrão); só `POST /api/auth/login` é aberto. Permissão por cargo com `[Authorize(Roles = nameof(Cargo.Supervisor))]`. Senha com PBKDF2-SHA256, 100 mil voltas, sal por usuário; mínimo de 8 caracteres.
10. **Erro de regra:** o service lança `RegraDeNegocioException`; o `ErroHandler` devolve 400 com a mensagem. Não encontrado: o service devolve nulo e o handler responde 404.

## Exigências do Victor (2026-10-11)

- Back em C#/.NET, padrão MVC com DTO, helper e handler; banco SQL Server do Windows; `appsettings.json` + `.env`.
- Três projetos (Api, Dominio, Dados) juntos numa solução; a Api é o projeto geral.
- Login pelo e-mail da PESSOA; cada usuário com a própria senha, guardada no banco.
- Projeto em 3 semanas: semana 1 e 2 o back, semana 3 o front em Angular (`ng serve`, `ng test`).

## Histórico

- 2026-10-11 — Criação: três projetos + testes, 51 tabelas na migração `Inicial`, login com JWT e cargo, catálogos e parâmetros.
- 2026-10-11 — Migração `CargaEspeciesArmazensPilhas` (22 espécies de exemplo, 5 armazéns, 40 pilhas). Apresentação do fluxo (`ApresentacaoService`). Testes da categoria `Banco` passam a usar o SQL Server de verdade, num banco próprio que é criado e apagado (`ViaPermanente_Testes`).
- 2026-10-11 — Pedido (`PedidoService`) e a regra da 2ª classe (`LimiteSegundaClasse`, classe estática em `Services` porque é regra de negócio sem dependência). Os testes de banco ficam na coleção `Banco` (rodam um de cada vez) e usam `BancoDeTeste`. Diferença do plano de 26/09: o item da regra se chama `ItemParaLimite` (não `ItemPedido`, para não confundir com `PedidoItem`), e o resultado `ResultadoLimite` também traz o percentual.
- 2026-10-11 — Nota fiscal (`NotaFiscalService` + regra `ConferenciaNota`) e oferta do fornecedor (`OfertaService`). `FormatoHelper` reúne os números em português (m, m³, %). `POST /api/pedidos/limite` virou `/verificacao` (2ª classe e oferta). Migração `ParametroToleranciaNota`.
- 2026-10-11 — Chegada (`ChegadaService` + regra `ConferenciaChegada`), cadastro da peça (`PecaService` + `GravacaoRigorosa`), leitura com alvo único (`LeituraService` + `AlvoUnico`), verificação de qualidade (`QualidadeService`) e entrada no armazém (`ArmazemService`). As consultas da peça usam `PecaResumo` (uma consulta só, filtrada antes de montar o registro). O cadastro com gravação que não vale responde 409 com o resultado.
- 2026-10-11 — Semana 2, etapa 1: varredura noturna (`VarreduraService` + `PosicaoNaVia`), alertas e vistoria (`AlertaService`), inspeção (`InspecaoService` + `ChecklistInspecao`). Migração `ParametrosDesvio` (`desvio_limite_mm` = 40 e `desvio_atencao_mm` = 20, provisórios).
- 2026-10-11 — Semana 2, etapa 2: troca em quatro passos (`TrocaService` + `CompatibilidadeTroca`) e correção da inspeção. Cada passo grava tudo junto por `MudancasDaTroca` (`ITrocaRepository.Gravar`). A peça está na via quando tem trecho ou km.
- 2026-10-11 — Relatório ambiental (`RelatorioAmbientalService` + `RelatorioAmbientalCalculo`), Tarefa 18.
- 2026-10-11 — Partição semanal de LEITURA_VIA (migração `ParticaoSemanalLeituraVia`, SQL próprio do SQL Server no `ParticaoRepository`), manutenção diária (`ManutencaoDiariaHandler`, um `BackgroundService`: a porta de entrada do relógio) e a regra `RetencaoLeituraVia`.
- 2026-10-11 — Armazém: troca de pilha (`ArmazemService`), conferência pilha a pilha (`ConferenciaService`), tag sem cadastro em campo (`VinculoTagService`), `IArmazemRepository`. Migração `LeituraNaPilha` (LEITURA_DORMENTE.id_pilha).
- 2026-10-11 — Histórico da peça (`HistoricoService` + `HistoricoRepository`): montado das tabelas de evento, nada guardado à parte.
- 2026-10-11 — Cadastros de apoio (`CadastroService`): empresas, contratos, estações e trechos.
- 2026-10-11 — Correção do pedido (`PedidoService.Corrigir`) com o retrato de antes e depois, itens incluídos.
- 2026-10-11 — Integração com o carro: esquema de autenticação `ChaveCarro` (`ChaveCarroHandler`, cabeçalho `X-Chave-Carro`, chave no `.env`), que só abre `POST /api/passagens`. `ChaveHelper` compara em tempo constante.
- 2026-10-11 — Importação da nota pela planilha (`ImportacaoNotaService`; leitura do .xlsx com ClosedXML 0.104.2, MIT, no `PlanilhaNotaRepository`) e `INTEGRACAO_LOG`. A planilha é um repositório (acesso a arquivo). Build sem avisos: o SQL da partição passou a usar parâmetros.
- 2026-10-11 — Exportações (`ExportacaoService` + `PlanilhaExportacaoRepository`): relatório ambiental para o IBAMA e estoque com código de material para o ERP; cadastro do código de material. Modelos de coluna provisórios.
- 2026-10-11 — Laudos em PDF (`LaudoService` monta o conteúdo em `DocumentoPdf`; `DocumentoPdfRepository` desenha com PDFsharp-MigraDoc 6.1.1, MIT; `FontesDoWindows` entrega a Arial).
- 2026-10-11 — Indicadores (`IndicadorService` + `IndicadorRepository`): os 18 do cérebro; a contagem das falhas de tag é feita no SQL Server.
- 2026-10-11 — OCR da placa (`OcrPlacaService` + regra `PlacaDoFeixe`; `TesseractOcrRepository` com o pacote Tesseract 5.2.0, Apache 2.0; `FotoRepository`; migração `ModeloOcrInicial`). `tessdata/` vai no projeto; `fotos/` fica fora do git.
