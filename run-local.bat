@echo off
chcp 65001 >nul
color 0A
cls

echo ╔════════════════════════════════════════════════════════════╗
echo ║         PicStone WEB - Execução Local (Debug)              ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

REM Define diretórios
set "ROOT=%~dp0"
set "BACKEND=%ROOT%Backend"
set "FRONTEND=%ROOT%Frontend"
set "LOGS=%ROOT%logs"

REM Cria pasta de logs
if not exist "%LOGS%" mkdir "%LOGS%"

REM Limpa logs antigos
del /Q "%LOGS%\*.log" 2>nul

echo [STEP 1/5] Limpando processos anteriores...
taskkill /F /IM dotnet.exe 2>nul >nul
taskkill /F /IM python.exe 2>nul >nul
taskkill /F /IM node.exe 2>nul >nul
timeout /t 2 /nobreak >nul
echo           ✓ Processos limpos
echo.

echo [STEP 2/5] Verificando Backend (.NET)...
cd /d "%BACKEND%"
if not exist "PicStoneFotoAPI.csproj" (
    echo           ✗ ERRO: Backend não encontrado!
    pause
    exit /b 1
)
echo           ✓ Backend encontrado
echo.

echo [STEP 3/5] Iniciando Backend na porta 5000...
echo           → Logs em: logs\backend.log
start "PicStone Backend" /MIN cmd /c "cd /d "%BACKEND%" && dotnet run --urls http://localhost:5000 > "%LOGS%\backend.log" 2>&1"
timeout /t 8 /nobreak >nul
echo           ✓ Backend iniciado
echo.

echo [STEP 4/5] Verificando servidor para Frontend...
where python >nul 2>nul
if %errorlevel% equ 0 (
    echo           ✓ Python encontrado
    echo           → Iniciando servidor HTTP na porta 8080...
    echo           → Logs em: logs\frontend.log
    start "PicStone Frontend" /MIN cmd /c "cd /d "%FRONTEND%" && python -m http.server 8080 > "%LOGS%\frontend.log" 2>&1"
    goto :frontend_ok
)

where npx >nul 2>nul
if %errorlevel% equ 0 (
    echo           ✓ Node.js encontrado
    echo           → Iniciando servidor HTTP na porta 8080...
    echo           → Logs em: logs\frontend.log
    start "PicStone Frontend" /MIN cmd /c "cd /d "%FRONTEND%" && npx http-server -p 8080 -c-1 > "%LOGS%\frontend.log" 2>&1"
    goto :frontend_ok
)

echo           ✗ ERRO: Nenhum servidor HTTP encontrado!
echo.
echo           Instale Python ou Node.js:
echo           • Python: https://www.python.org/downloads/
echo           • Node.js: https://nodejs.org/
echo.
pause
exit /b 1

:frontend_ok
timeout /t 3 /nobreak >nul
echo           ✓ Frontend iniciado
echo.

echo [STEP 5/5] Abrindo aplicação no navegador...
timeout /t 2 /nobreak >nul
start http://localhost:8080
echo           ✓ Navegador aberto
echo.

echo ╔════════════════════════════════════════════════════════════╗
echo ║                   APLICAÇÃO RODANDO!                       ║
echo ╠════════════════════════════════════════════════════════════╣
echo ║  Frontend: http://localhost:8080                           ║
echo ║  Backend:  http://localhost:5000                           ║
echo ║                                                            ║
echo ║  Logs Backend:  logs\backend.log                           ║
echo ║  Logs Frontend: logs\frontend.log                          ║
echo ║                                                            ║
echo ║  Debug Images:  Backend\wwwroot\debug\                     ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo [OPÇÕES]
echo   1 - Ver logs do Backend em tempo real
echo   2 - Ver logs do Frontend em tempo real
echo   3 - Abrir pasta de Debug Images
echo   4 - Recarregar Backend (após mudanças no código)
echo   Q - ENCERRAR tudo e sair
echo.

:menu
set /p choice="Escolha uma opção: "

if /i "%choice%"=="1" (
    start "Backend Logs" powershell -Command "Get-Content '%LOGS%\backend.log' -Wait"
    goto :menu
)

if /i "%choice%"=="2" (
    start "Frontend Logs" powershell -Command "Get-Content '%LOGS%\frontend.log' -Wait"
    goto :menu
)

if /i "%choice%"=="3" (
    if exist "%BACKEND%\wwwroot\debug" (
        start "" "%BACKEND%\wwwroot\debug"
    ) else (
        echo Pasta debug ainda não existe. Gere uma bancada primeiro.
    )
    goto :menu
)

if /i "%choice%"=="4" (
    echo.
    echo Recarregando Backend...
    taskkill /F /IM dotnet.exe 2>nul >nul
    timeout /t 2 /nobreak >nul
    start "PicStone Backend" /MIN cmd /c "cd /d "%BACKEND%" && dotnet run --urls http://localhost:5000 > "%LOGS%\backend.log" 2>&1"
    timeout /t 8 /nobreak >nul
    echo ✓ Backend reiniciado!
    echo.
    goto :menu
)

if /i "%choice%"=="q" goto :shutdown
if /i "%choice%"=="Q" goto :shutdown

echo Opção inválida!
goto :menu

:shutdown
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║                  ENCERRANDO SERVIDORES                     ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

echo → Parando Backend...
taskkill /F /IM dotnet.exe 2>nul >nul
echo   ✓ Backend encerrado

echo → Parando Frontend...
taskkill /F /IM python.exe 2>nul >nul
taskkill /F /IM node.exe 2>nul >nul
echo   ✓ Frontend encerrado

echo.
echo ✓ Aplicação encerrada com sucesso!
echo.
echo Logs salvos em: %LOGS%
echo.
pause
exit /b 0
