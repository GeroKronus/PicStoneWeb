@echo off
chcp 65001 >nul
cls

echo ╔════════════════════════════════════════════╗
echo ║      PicStone WEB - Localhost:5000         ║
echo ╚════════════════════════════════════════════╝
echo.

REM Limpa processos anteriores
taskkill /F /IM dotnet.exe 2>nul >nul

echo [1/3] Copiando Frontend para Backend/wwwroot...
xcopy /E /I /Y "%~dp0Frontend\*" "%~dp0Backend\wwwroot\" >nul
echo       ✓ Frontend copiado
echo.

echo [2/3] Iniciando Backend em localhost:5000...
cd /d "%~dp0Backend"
start "PicStone Backend" cmd /k "dotnet run --urls http://localhost:5000"

echo       Aguardando inicialização...
timeout /t 10 /nobreak >nul
echo       ✓ Backend iniciado
echo.

echo [3/3] Abrindo navegador...
start http://localhost:5000
echo       ✓ Navegador aberto
echo.

echo ╔════════════════════════════════════════════╗
echo ║         APLICAÇÃO RODANDO EM:              ║
echo ║                                            ║
echo ║       http://localhost:5000                ║
echo ║                                            ║
echo ║  Para ENCERRAR: Feche a janela do Backend  ║
echo ║  ou pressione Ctrl+C nela                 ║
echo ╚════════════════════════════════════════════╝
echo.
pause
