@echo off
chcp 65001 >nul
color 0C

echo.
echo ════════════════════════════════════════════════════════
echo    🛑 Parando Servidor PicStone
echo ════════════════════════════════════════════════════════
echo.

echo Procurando processos do .NET na porta 5000...
echo.

REM Mata todos os processos dotnet que estão rodando a API
taskkill /F /IM dotnet.exe /T >nul 2>&1

if %errorlevel% equ 0 (
    echo ✅ Servidor parado com sucesso!
) else (
    echo ℹ️  Nenhum servidor estava rodando.
)

echo.
pause
