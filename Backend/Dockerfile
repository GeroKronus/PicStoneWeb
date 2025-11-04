FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Install native dependencies for SkiaSharp
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libgdiplus \
    libjpeg62-turbo \
    libpng16-16 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/PicStoneFotoAPI.csproj", "Backend/"]
RUN dotnet restore "Backend/PicStoneFotoAPI.csproj"
COPY Backend/ Backend/
WORKDIR "/src/Backend"
RUN dotnet build "PicStoneFotoAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PicStoneFotoAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copiar Frontend para wwwroot (já foi copiado antes do build)
COPY Backend/wwwroot ./wwwroot

# Copiar recursos de mockup (molduras, etc)
COPY Backend/MockupResources ./MockupResources

ENTRYPOINT ["dotnet", "PicStoneFotoAPI.dll"]
