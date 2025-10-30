@echo off
chcp 65001 >nul

echo.
echo 🚀 Abrindo projeto no Visual Studio Code...
echo.

cd /d "%~dp0"

REM Tenta abrir com o comando 'code' (se VSCode está no PATH)
code . >nul 2>&1

if %errorlevel% neq 0 (
    echo ⚠️  Visual Studio Code não encontrado no PATH.
    echo.
    echo Para usar este comando:
    echo 1. Abra o VSCode
    echo 2. Pressione Ctrl+Shift+P
    echo 3. Digite "shell command" e selecione "Install 'code' command in PATH"
    echo.
    echo Ou abra manualmente: File ^> Open Folder ^> PicStone WEB
    echo.
    pause
) else (
    echo ✅ Projeto aberto no VSCode!
    timeout /t 2 >nul
)
