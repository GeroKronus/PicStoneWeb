@echo off
cls

echo ================================================
echo   Reiniciando aplicacao (LIMPA CACHE)
echo ================================================
echo.

echo [1/5] Parando containers...
docker-compose down
timeout /t 2 /nobreak >nul

echo [2/5] Limpando wwwroot...
rd /S /Q "Backend\wwwroot" 2>nul
mkdir "Backend\wwwroot"

echo [3/5] Copiando Frontend atualizado...
xcopy /E /I /Y "Frontend\*" "Backend\wwwroot\" >nul

echo [4/5] Reconstruindo e iniciando containers...
docker-compose up -d --build

echo [5/5] Aguardando Backend inicializar...
timeout /t 15 /nobreak >nul

echo.
echo ================================================
echo   Aplicacao reiniciada em http://localhost:5000
echo   Abra em modo ANONIMO para evitar cache!
echo ================================================
echo.

start http://localhost:5000

pause
