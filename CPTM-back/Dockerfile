# O back do YBYRA (API + domínio + dados) num container Linux.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ViaPermanente.sln ./
COPY src/ViaPermanente.Api/ViaPermanente.Api.csproj src/ViaPermanente.Api/
COPY src/ViaPermanente.Dominio/ViaPermanente.Dominio.csproj src/ViaPermanente.Dominio/
COPY src/ViaPermanente.Dados/ViaPermanente.Dados.csproj src/ViaPermanente.Dados/
RUN dotnet restore src/ViaPermanente.Api/ViaPermanente.Api.csproj
COPY src/ src/
RUN dotnet publish src/ViaPermanente.Api/ViaPermanente.Api.csproj -c Release -o /app --no-restore

# Os testes do back no Docker (docker compose --profile testes run --rm testes): o SDK com o Tesseract e as fontes do Linux,
# contra o SQL Server do compose. Não entra na imagem do back (o estágio final não usa este).
FROM build AS testes
RUN apt-get update \
 && apt-get install -y --no-install-recommends libtesseract5 liblept5 fonts-liberation \
 && rm -rf /var/lib/apt/lists/* \
 && ln -sf /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
COPY tessdata/ tessdata/
COPY tests/ tests/
RUN dotnet build tests/ViaPermanente.Testes/ViaPermanente.Testes.csproj -c Release \
 && saida=tests/ViaPermanente.Testes/bin/Release/net8.0 \
 && mkdir -p $saida/x64 \
 && ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 $saida/x64/libleptonica-1.82.0.so \
 && ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 $saida/x64/libtesseract50.so
ENTRYPOINT ["dotnet", "test", "tests/ViaPermanente.Testes/ViaPermanente.Testes.csproj", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# OCR (Tesseract e Leptonica do Linux) e a fonte do PDF (Liberation Sans, mesmas medidas da Arial).
# O carregador do Tesseract do .NET abre o libdl.so (sem número), que o Debian 12 não traz: vira um atalho.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libtesseract5 liblept5 fonts-liberation curl \
 && rm -rf /var/lib/apt/lists/* \
 && ln -sf /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so
WORKDIR /app
COPY --from=build /app ./
COPY tessdata ./tessdata
# O pacote Tesseract 5.2.0 do .NET procura as bibliotecas nativas com estes nomes na pasta x64
RUN mkdir -p x64 \
 && ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 x64/libleptonica-1.82.0.so \
 && ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 x64/libtesseract50.so
ENV ASPNETCORE_HTTP_PORTS=8045 \
    Ocr__Tessdata=/app/tessdata \
    Fotos__Pasta=/app/fotos
EXPOSE 8045
ENTRYPOINT ["dotnet", "ViaPermanente.Api.dll"]
