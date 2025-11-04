@echo off
echo ============================================
echo  PicStone WEB - Iniciando Localmente
echo ============================================
echo.

REM Mata processos anteriores caso existam
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM python.exe 2>nul
taskkill /F /IM node.exe 2>nul

echo [1/3] Iniciando Backend (.NET Core)...
start "PicStone Backend" cmd /k "cd /d "%~dp0Backend" && dotnet run --urls http://localhost:5000"

echo [2/3] Aguardando Backend inicializar (5 segundos)...
timeout /t 5 /nobreak >nul

echo [3/3] Iniciando Frontend (Servidor HTTP)...

REM Tenta usar Python primeiro
where python >nul 2>nul
if %errorlevel% equ 0 (
    echo Usando Python para servir Frontend...
    start "PicStone Frontend" cmd /k "cd /d "%~dp0Frontend" && python -m http.server 8080"
    goto :open_browser
)

REM Se não tiver Python, tenta usar npx (Node.js)
where npx >nul 2>nul
if %errorlevel% equ 0 (
    echo Usando Node.js para servir Frontend...
    start "PicStone Frontend" cmd /k "cd /d "%~dp0Frontend" && npx http-server -p 8080 -c-1"
    goto :open_browser
)

REM Se não tiver nenhum dos dois
echo.
echo ERRO: Nenhum servidor HTTP encontrado!
echo Por favor, instale Python ou Node.js:
echo   - Python: https://www.python.org/downloads/
echo   - Node.js: https://nodejs.org/
echo.
echo Ou use a extensao Live Server no VS Code.
pause
exit /b 1

:open_browser
echo.
echo ============================================
echo  PicStone WEB rodando localmente!
echo ============================================
echo.
echo Backend:  http://localhost:5000
echo Frontend: http://localhost:8080
echo.
echo Aguardando Frontend inicializar (3 segundos)...
timeout /t 3 /nobreak >nul

echo Abrindo navegador...
start http://localhost:8080

echo.
echo Pressione qualquer tecla para ENCERRAR os servidores...
pause >nul

echo.
echo Encerrando servidores...
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM python.exe 2>nul
taskkill /F /IM node.exe 2>nul

echo.
echo Servidores encerrados!
echo.
pause
