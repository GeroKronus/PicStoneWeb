# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o arquivo de projeto e restaura dependências
COPY Backend/PicStoneFotoAPI.csproj Backend/
RUN dotnet restore Backend/PicStoneFotoAPI.csproj

# Copia o restante do código e compila
COPY Backend/ Backend/
WORKDIR /src/Backend
RUN dotnet build PicStoneFotoAPI.csproj -c Release -o /app/build
RUN dotnet publish PicStoneFotoAPI.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Instala dependências do sistema
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Copia os arquivos publicados
COPY --from=build /app/publish .

# Copia o frontend para wwwroot
COPY Frontend/ ./wwwroot/

# Copia as pastas de recursos (molduras e logo)
COPY Backend/Molduras/ ./Molduras/
COPY Backend/Cavaletes/ ./Cavaletes/

# Cria diretório para uploads
RUN mkdir -p /app/uploads && chmod 777 /app/uploads

# Cria diretório para logs
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Expõe a porta (Railway usa variável PORT)
EXPOSE 8080

# Define variáveis de ambiente padrão
ENV ASPNETCORE_ENVIRONMENT=Production
# Railway define a variável PORT automaticamente
ENV ASPNETCORE_URLS=http://+:8080

# Comando de inicialização
ENTRYPOINT ["dotnet", "PicStoneFotoAPI.dll"]
