@echo off
echo.
echo ════════════════════════════════════════════════════════
echo    Habilitando Scripts PowerShell
echo ════════════════════════════════════════════════════════
echo.
echo Este script irá habilitar a execução de scripts PowerShell
echo na sua máquina (apenas para o usuário atual).
echo.
echo Pressione qualquer tecla para continuar...
pause >nul

powershell -Command "Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force"

if %errorlevel% equ 0 (
    echo.
    echo ✅ Scripts PowerShell habilitados com sucesso!
    echo.
    echo Agora você pode executar: .\MENU.ps1
) else (
    echo.
    echo ❌ Erro ao habilitar. Execute como Administrador.
)

echo.
pause
