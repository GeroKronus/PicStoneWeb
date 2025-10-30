@echo off
chcp 65001 >nul
color 0A

echo.
echo ════════════════════════════════════════════════════════
echo    🚀 PicStone - Inicializador Local
echo ════════════════════════════════════════════════════════
echo.

REM Verifica se o .NET está instalado
echo [1/4] Verificando .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ ERRO: .NET SDK não encontrado!
    echo.
    echo Por favor, instale o .NET 8.0 SDK de:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo ✅ .NET SDK encontrado:
dotnet --version
echo.

REM Navega para o diretório do Backend
cd /d "%~dp0Backend"

REM Verifica se o arquivo .csproj existe
if not exist "PicStoneFotoAPI.csproj" (
    echo ❌ ERRO: Arquivo PicStoneFotoAPI.csproj não encontrado!
    echo.
    pause
    exit /b 1
)

echo [2/4] Restaurando dependências...
dotnet restore
if %errorlevel% neq 0 (
    echo ❌ ERRO ao restaurar dependências!
    echo.
    pause
    exit /b 1
)
echo ✅ Dependências restauradas com sucesso!
echo.

echo [3/4] Compilando aplicação...
dotnet build --no-restore
if %errorlevel% neq 0 (
    echo ❌ ERRO ao compilar!
    echo.
    pause
    exit /b 1
)
echo ✅ Compilação concluída!
echo.

echo [4/4] Iniciando servidor...
echo.
echo ════════════════════════════════════════════════════════
echo    📱 APLICAÇÃO RODANDO!
echo ════════════════════════════════════════════════════════
echo.
echo 🌐 URL Local:        http://localhost:5000
echo 📱 Celular (mesma rede): http://SEU_IP:5000
echo 👤 Usuário:          admin
echo 🔑 Senha:            admin123
echo 📚 Swagger:          http://localhost:5000/swagger
echo.
echo ════════════════════════════════════════════════════════
echo.
echo 💡 Dica: Para descobrir seu IP local, abra outro terminal e digite: ipconfig
echo.
echo Pressione Ctrl+C para parar o servidor...
echo.

dotnet run --no-build

pause
