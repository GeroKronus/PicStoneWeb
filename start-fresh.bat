@echo off
cls

echo ================================================
echo   PicStone WEB - Inicio Limpo (Sem Cache)
echo ================================================
echo.

REM Para todos os processos
echo [1/5] Parando processos anteriores...
taskkill /F /IM dotnet.exe 2>nul >nul
taskkill /F /IM python.exe 2>nul >nul
taskkill /F /IM node.exe 2>nul >nul
echo       OK - Processos encerrados
echo.

REM Limpa wwwroot completamente
echo [2/5] Limpando Frontend antigo...
if exist "%~dp0Backend\wwwroot" (
    rd /S /Q "%~dp0Backend\wwwroot"
)
mkdir "%~dp0Backend\wwwroot"
echo       OK - Frontend limpo
echo.

REM Copia Frontend novamente
echo [3/5] Copiando Frontend atualizado...
xcopy /E /I /Y "%~dp0Frontend\*" "%~dp0Backend\wwwroot\" >nul
echo       OK - Frontend copiado
echo.

REM Verifica arquivos copiados
echo [4/5] Verificando arquivos...
dir /B "%~dp0Backend\wwwroot"
echo.

REM Inicia Backend
echo [5/5] Iniciando Backend em localhost:5000...
cd /d "%~dp0Backend"
start "PicStone Backend" cmd /k "dotnet run --urls http://localhost:5000"

echo       Aguardando inicializacao...
timeout /t 10 /nobreak >nul
echo       OK - Backend iniciado
echo.

echo ================================================
echo          APLICACAO RODANDO EM:
echo.
echo        http://localhost:5000
echo.
echo   IMPORTANTE: Abra em modo ANONIMO/INCOGNITO
echo   para evitar cache do navegador!
echo.
echo   Chrome: Ctrl+Shift+N
echo   Firefox: Ctrl+Shift+P
echo   Edge: Ctrl+Shift+N
echo ================================================
echo.
echo Pressione qualquer tecla para abrir o navegador...
pause >nul

start http://localhost:5000

echo.
echo Navegador aberto!
echo Para ENCERRAR: Feche a janela "PicStone Backend"
echo.
pause
