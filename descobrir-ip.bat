@echo off
chcp 65001 >nul
color 0D

echo.
echo ════════════════════════════════════════════════════════
echo    📱 Descobrir IP para Acesso Mobile
echo ════════════════════════════════════════════════════════
echo.

echo Seus endereços IP:
echo.

REM Mostra apenas IPs IPv4 relevantes
ipconfig | findstr /i "IPv4"

echo.
echo ════════════════════════════════════════════════════════
echo.
echo 💡 Para acessar do celular (mesma rede Wi-Fi):
echo.
echo    http://SEU_IP:5000
echo.
echo    Exemplo: http://192.168.1.100:5000
echo.
echo ════════════════════════════════════════════════════════
echo.
echo ⚠️  IMPORTANTE:
echo    - Celular e PC devem estar na mesma rede Wi-Fi
echo    - Firewall do Windows pode bloquear (libere porta 5000)
echo    - Use o IP da interface Wi-Fi ou Ethernet
echo.
pause
