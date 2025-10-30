@echo off
chcp 65001 >nul
color 0E

echo.
echo ════════════════════════════════════════════════════════
echo    🧹 Limpeza de Arquivos de Build
echo ════════════════════════════════════════════════════════
echo.

echo Limpando arquivos temporários...
echo.

cd /d "%~dp0Backend"

REM Remove pastas bin e obj
if exist "bin" (
    echo 🗑️  Removendo pasta bin...
    rmdir /s /q "bin"
)

if exist "obj" (
    echo 🗑️  Removendo pasta obj...
    rmdir /s /q "obj"
)

if exist "logs" (
    echo 🗑️  Removendo logs antigos...
    del /q "logs\*.log" 2>nul
)

if exist "uploads" (
    echo 🗑️  Removendo uploads de teste...
    del /q "uploads\*.jpg" 2>nul
    del /q "uploads\*.jpeg" 2>nul
    del /q "uploads\*.png" 2>nul
)

echo.
echo ✅ Limpeza concluída!
echo.
echo Executar 'iniciar-local.bat' irá recompilar tudo.
echo.
pause
