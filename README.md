# CPTM-API

**YBYRA**, qualidade e rastreamento de dormentes ferroviários (CPTM, Via Permanente). Tem duas partes: a API em `CPTM-back` (C# com .NET 8 e SQL Server) e o site em `CPTM-front` (Angular).

## Todos os comandos

### Com Docker (o jeito mais simples; dentro de `CPTM-back`)

| Para quê | Comando |
|---|---|
| Subir o back e o banco (a primeira vez baixa as imagens e demora) | `docker compose up -d --build` |
| Ver se o back subiu e alcança o banco | `curl http://localhost:8045/hc` (ou abra o endereço no navegador) |
| Ver os logs do back | `docker compose logs -f back` |
| Ver os logs do banco | `docker compose logs -f banco` |
| Ver o que está rodando | `docker compose ps` |
| Reconstruir o back depois de mudar o código | `docker compose up -d --build back` |
| Rodar os testes do back no Docker | `docker compose --profile testes run --rm testes` |
| Parar sem apagar nada | `docker compose stop` |
| Parar e remover os containers (o banco e as fotos ficam nos volumes) | `docker compose down` |

### Sem Docker (dentro de `CPTM-back`)

| Para quê | Comando |
|---|---|
| Restaurar a ferramenta do EF (a primeira vez) | `dotnet tool restore` |
| Criar ou atualizar o banco | `dotnet dotnet-ef database update --project src/ViaPermanente.Dados --startup-project src/ViaPermanente.Api` |
| Ligar a API no PC | `dotnet run --project src/ViaPermanente.Api --launch-profile http` |
| Ligar a API aberta para o celular | `dotnet run --project src/ViaPermanente.Api --launch-profile celular` |
| Rodar todos os testes | `dotnet test` |
| Rodar só os testes que não usam o banco | `dotnet test --filter "Categoria!=Banco"` |
| Criar uma migração depois de mudar o banco | `dotnet dotnet-ef migrations add NomeDaMudanca --project src/ViaPermanente.Dados --startup-project src/ViaPermanente.Api` |

### O site (dentro de `CPTM-front`)

| Para quê | Comando |
|---|---|
| Instalar as dependências | `npm install` |
| Ligar o site (http://localhost:4200) | `npx ng serve` |
| Ligar o site aberto para o celular | `npm run celular` |
| Rodar os testes | `npx ng test --watch=false` |
| Gerar a versão de produção | `npx ng build` |

## Como rodar em outra máquina

### 1. Instale

- [Git](https://git-scm.com/)
- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads), com a instância `SQLEXPRESS` e o login do Windows
- [Node.js 24 LTS](https://nodejs.org/)

### 2. Baixe o sistema

O repositório é privado: entre com uma conta do GitHub que tenha acesso.

```bash
git clone https://github.com/victordejesusslise-byte/CPTM-API.git
```

### 3. Crie o arquivo de senhas

Em `CPTM-back`, copie `.env.exemplo` para `.env` e preencha:

- `Jwt__Chave`: pelo menos 32 caracteres aleatórios;
- `UsuarioInicial__Nome`, `UsuarioInicial__Email` e `UsuarioInicial__Senha`: o supervisor que entra primeiro;
- `Integracao__ChaveCarro`: outra chave aleatória.

A linha do banco já serve se a instância se chamar `SQLEXPRESS`. O `.env` nunca vai para o git.

### 4. Crie o banco

Dentro de `CPTM-back`:

```bash
dotnet tool restore
dotnet dotnet-ef database update --project src/ViaPermanente.Dados --startup-project src/ViaPermanente.Api
```

### 5. Ligue a API

Dentro de `CPTM-back`, e deixe o terminal aberto:

```bash
dotnet run --project src/ViaPermanente.Api --launch-profile http
```

### 6. Ligue o site

Em outro terminal, dentro de `CPTM-front`:

```bash
npm install
npx ng serve
```

### 7. Entre

Abra http://localhost:4200 e entre com o e-mail e a senha do supervisor que você pôs no `.env`. Depois, o supervisor cadastra as outras pessoas, cada uma com a própria senha.

## No celular (mesmo Wi-Fi)

1. No PC, rode `ipconfig` e anote o **Endereço IPv4** (ex.: `192.168.0.10`).
2. Ligue a API aberta para a rede, dentro de `CPTM-back`:

```bash
dotnet run --project src/ViaPermanente.Api --launch-profile celular
```

3. Ligue o site aberto para a rede, dentro de `CPTM-front`:

```bash
npm run celular
```

4. No celular, abra `http://SEU-IP:4200` (ex.: `http://192.168.0.10:4200`) e entre com o seu e-mail e senha.

Se o Windows perguntar sobre o firewall, permita em **redes privadas**. Funciona só no mesmo Wi-Fi; para usar fora, o sistema precisa ir para um servidor.

## Banco de demonstração

Para apresentar o sistema, a API cria (e depois apaga) uma demonstração **no banco de trabalho**: 1 fornecedor, 3 pedidos (um recebido, um recebido em parte e um aberto), 3 peças na via com 3 alertas e 3 trocas em passos diferentes. Tudo leva a marca `DEMO-`, e a limpeza apaga só isso.

Com a API ligada, abra http://localhost:8045/swagger, entre com o supervisor (`POST /api/auth/login`, **Authorize**) e rode:

- **Criar:** `POST /api/apresentacao/demonstracao`
- **Apagar:** `DELETE /api/apresentacao/demonstracao`

Só funciona em desenvolvimento.

## Com Docker (o back, o banco e os logs)

Precisa só do [Docker Desktop](https://www.docker.com/products/docker-desktop/) aberto; não precisa de .NET nem de SQL Server instalados.

1. No `CPTM-back/.env`, além do que está no passo 3, preencha `Banco__SenhaSa` (a senha do banco do container: letras maiúsculas, minúsculas e números, 12 ou mais).
2. Dentro de `CPTM-back`, suba tudo (a primeira vez baixa as imagens e demora alguns minutos):

```bash
docker compose up -d --build
```

3. O back fica em http://localhost:8045 (o site Angular continua com `npx ng serve`). Na primeira subida ele cria o banco e o supervisor do `.env`.
4. Os logs:

```bash
docker compose logs -f back
```

5. Os testes do back, no Docker (usam o SQL Server do compose; o banco de teste é criado e apagado a cada teste):

```bash
docker compose --profile testes run --rm testes
```

Para ver se o back está no ar: http://localhost:8045/hc (responde 200 quando o back e o banco estão bem).

Para parar: `docker compose down` (o banco e as fotos ficam guardados nos volumes do Docker). O banco do container também abre no SSMS em `localhost,14330`, usuário `sa`.

## Segurança

- O login vale 8 horas. "Sair" encerra a sessão em todos os aparelhos, e o supervisor desativa uma conta em Usuários (a sessão dela cai na hora).
- Depois de 5 senhas erradas seguidas, a conta espera (30 s, depois 1 min, depois 5 min); cada aparelho tenta no máximo 10 vezes por minuto.
- Em produção (`Seguranca__ExigirHttps`, ligado fora do Development), a API recusa senha e token sem HTTPS vindos da rede: o servidor precisa de certificado.
- Depois de uma atualização de segurança, todos precisam entrar de novo uma vez.
