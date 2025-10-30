@echo off
chcp 65001 >nul
color 0B

echo.
echo ════════════════════════════════════════════════════════
echo    📸 PicStone - Inicializador Universal
echo ════════════════════════════════════════════════════════
echo.
echo Este script funciona em qualquer terminal!
echo.
echo Detectando ambiente...
echo.

REM Verifica se está no PowerShell
echo %PSModulePath% | find "WindowsPowerShell" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✅ PowerShell detectado!
    echo.
    echo Executando versão PowerShell...
    timeout /t 2 >nul
    powershell -NoProfile -ExecutionPolicy Bypass -File ".\MENU.ps1"
) else (
    echo ✅ Command Prompt detectado!
    echo.
    echo Executando versão CMD...
    timeout /t 2 >nul
    call MENU.bat
)
