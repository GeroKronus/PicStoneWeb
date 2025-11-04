@echo off
echo ============================================
echo  Encerrando PicStone WEB Local
echo ============================================
echo.

echo Matando processos...
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM python.exe 2>nul
taskkill /F /IM node.exe 2>nul

echo.
echo Servidores encerrados!
echo.
pause
