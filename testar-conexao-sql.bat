@echo off
chcp 65001 >nul
color 0B

echo.
echo ════════════════════════════════════════════════════════
echo    🔌 Teste de Conexão SQL Server
echo ════════════════════════════════════════════════════════
echo.

set SERVER=131.255.255.16
set PORT=11433

echo Testando conexão com %SERVER%:%PORT%...
echo.

REM Testa se a porta está acessível
powershell -Command "Test-NetConnection -ComputerName %SERVER% -Port %PORT% -InformationLevel Detailed | Select-Object -Property ComputerName, RemoteAddress, RemotePort, TcpTestSucceeded"

echo.
echo ════════════════════════════════════════════════════════
echo.

powershell -Command "$result = Test-NetConnection -ComputerName %SERVER% -Port %PORT%; if ($result.TcpTestSucceeded) { Write-Host '✅ Conexão bem-sucedida! O servidor SQL está acessível.' -ForegroundColor Green } else { Write-Host '❌ Falha na conexão! Verifique:' -ForegroundColor Red; Write-Host '   1. Firewall está bloqueando a porta %PORT%' -ForegroundColor Yellow; Write-Host '   2. SQL Server está rodando' -ForegroundColor Yellow; Write-Host '   3. SQL Server aceita conexões remotas' -ForegroundColor Yellow }"

echo.
pause
