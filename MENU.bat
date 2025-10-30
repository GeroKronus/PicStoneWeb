@echo off
chcp 65001 >nul
color 0A

:MENU
cls
echo.
echo ════════════════════════════════════════════════════════
echo    📸 PicStone - Menu Principal
echo ════════════════════════════════════════════════════════
echo.
echo    [1] 🚀 Iniciar Servidor Local
echo    [2] 🛑 Parar Servidor
echo    [3] 🔌 Testar Conexão SQL Server
echo    [4] 📱 Descobrir IP para Celular
echo    [5] 🧹 Limpar Build
echo    [6] 💻 Abrir no VS Code
echo    [7] 📚 Abrir Swagger (localhost:5000/swagger)
echo    [8] 🌐 Abrir Aplicação (localhost:5000)
echo    [9] 📖 Ver Documentação
echo    [0] ❌ Sair
echo.
echo ════════════════════════════════════════════════════════
echo.

set /p opcao="Escolha uma opção: "

if "%opcao%"=="1" goto INICIAR
if "%opcao%"=="2" goto PARAR
if "%opcao%"=="3" goto TESTAR_SQL
if "%opcao%"=="4" goto DESCOBRIR_IP
if "%opcao%"=="5" goto LIMPAR
if "%opcao%"=="6" goto VSCODE
if "%opcao%"=="7" goto SWAGGER
if "%opcao%"=="8" goto ABRIR_APP
if "%opcao%"=="9" goto DOCS
if "%opcao%"=="0" goto SAIR

echo.
echo ❌ Opção inválida!
timeout /t 2 >nul
goto MENU

:INICIAR
cls
call iniciar-local.bat
goto MENU

:PARAR
cls
call parar-servidor.bat
goto MENU

:TESTAR_SQL
cls
call testar-conexao-sql.bat
goto MENU

:DESCOBRIR_IP
cls
call descobrir-ip.bat
goto MENU

:LIMPAR
cls
call limpar-build.bat
goto MENU

:VSCODE
cls
call abrir-vscode.bat
goto MENU

:SWAGGER
echo.
echo 🌐 Abrindo Swagger UI...
timeout /t 1 >nul
start http://localhost:5000/swagger
echo.
echo ℹ️  Se o servidor não estiver rodando, use opção [1] primeiro.
echo.
pause
goto MENU

:ABRIR_APP
echo.
echo 🌐 Abrindo aplicação...
timeout /t 1 >nul
start http://localhost:5000
echo.
echo ℹ️  Se o servidor não estiver rodando, use opção [1] primeiro.
echo.
pause
goto MENU

:DOCS
echo.
echo 📖 Documentações disponíveis:
echo.
echo    - README.md (documentação completa)
echo    - GUIA_RAPIDO.md (guia de deploy)
echo.
echo Abrindo README.md...
timeout /t 1 >nul
start README.md
pause
goto MENU

:SAIR
echo.
echo 👋 Até logo!
echo.
timeout /t 1 >nul
exit
