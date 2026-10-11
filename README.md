# CPTM-API

**YBYRA**, qualidade e rastreamento de dormentes ferroviários (CPTM, Via Permanente). Tem duas partes: a API em `CPTM-back` (C# com .NET 8 e SQL Server) e o site em `CPTM-front` (Angular).

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
