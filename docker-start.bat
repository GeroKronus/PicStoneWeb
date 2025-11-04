@echo off
cls

echo ================================================
echo   PicStone WEB - Docker (PostgreSQL)
echo   Ambiente identico ao Railway
echo ================================================
echo.

echo [1/4] Parando containers anteriores...
docker-compose down 2>nul

echo [2/4] Copiando Frontend para Backend/wwwroot...
xcopy /E /I /Y "%~dp0Frontend\*" "%~dp0Backend\wwwroot\" >nul
echo       OK - Frontend copiado

echo [3/4] Iniciando containers (PostgreSQL + Backend)...
echo       Isso pode demorar na primeira vez (download de imagens)...
docker-compose up -d --build

echo [4/4] Aguardando Backend inicializar...
timeout /t 15 /nobreak >nul

echo.
echo ================================================
echo   INICIALIZANDO BANCO DE DADOS...
echo ================================================
echo.
echo Acessando: http://localhost:5000/api/migration/run
curl -s http://localhost:5000/api/migration/run
echo.
echo.
echo Acessando: http://localhost:5000/api/migration/populate-materials
curl -s http://localhost:5000/api/migration/populate-materials
echo.
echo.

echo ================================================
echo          APLICACAO RODANDO EM:
echo.
echo        http://localhost:5000
echo.
echo   PostgreSQL: localhost:5432
echo   User: admin / Senha: admin123
echo.
echo   Ver logs: docker-compose logs -f backend
echo   Parar: docker-compose down
echo ================================================
echo.

start http://localhost:5000

pause
