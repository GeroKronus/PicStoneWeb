@echo off
echo Parando todos os processos...
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM python.exe 2>nul
taskkill /F /IM node.exe 2>nul
timeout /t 2 /nobreak >nul

echo Limpando wwwroot...
rd /S /Q "Backend\wwwroot" 2>nul
mkdir "Backend\wwwroot"

echo Copiando Frontend atualizado...
xcopy /E /I /Y "Frontend\*" "Backend\wwwroot\" >nul

echo Iniciando Backend...
cd Backend
start "PicStone Backend - LOGS" cmd /k "dotnet run --urls http://localhost:5000"

timeout /t 8 /nobreak >nul
echo.
echo ================================================
echo   Aplicacao rodando em http://localhost:5000
echo   Abra em modo ANONIMO para evitar cache!
echo ================================================
echo.
start http://localhost:5000
pause
