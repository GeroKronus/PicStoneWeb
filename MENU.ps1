# Menu Principal - PicStone
# Executar com: .\MENU.ps1

function Show-Menu {
    Clear-Host
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "   PicStone - Menu Principal" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   [1] Iniciar Servidor Local" -ForegroundColor White
    Write-Host "   [2] Parar Servidor" -ForegroundColor White
    Write-Host "   [3] Testar Conexao SQL Server" -ForegroundColor White
    Write-Host "   [4] Descobrir IP para Celular" -ForegroundColor White
    Write-Host "   [5] Limpar Build" -ForegroundColor White
    Write-Host "   [6] Abrir no VS Code" -ForegroundColor White
    Write-Host "   [7] Abrir Swagger (localhost:5000/swagger)" -ForegroundColor White
    Write-Host "   [8] Abrir Aplicacao (localhost:5000)" -ForegroundColor White
    Write-Host "   [9] Ver Documentacao" -ForegroundColor White
    Write-Host "   [0] Sair" -ForegroundColor White
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Start-Server {
    Clear-Host
    & ".\iniciar-local.ps1"
    pause
}

function Stop-Server {
    Clear-Host
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Red
    Write-Host "   Parando Servidor PicStone" -ForegroundColor Red
    Write-Host "========================================================" -ForegroundColor Red
    Write-Host ""

    $processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

    if ($processes) {
        $processes | Stop-Process -Force
        Write-Host "Servidor parado com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "Nenhum servidor estava rodando." -ForegroundColor Yellow
    }

    Write-Host ""
    pause
}

function Test-SqlConnection {
    Clear-Host
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "   Teste de Conexao SQL Server" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host ""

    $server = "131.255.255.16"
    $port = 11433

    Write-Host "Testando conexao com $server`:$port..." -ForegroundColor Yellow
    Write-Host ""

    $result = Test-NetConnection -ComputerName $server -Port $port -InformationLevel Detailed

    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host ""

    if ($result.TcpTestSucceeded) {
        Write-Host "Conexao bem-sucedida! O servidor SQL esta acessivel." -ForegroundColor Green
    } else {
        Write-Host "Falha na conexao! Verifique:" -ForegroundColor Red
        Write-Host "   1. Firewall esta bloqueando a porta $port" -ForegroundColor Yellow
        Write-Host "   2. SQL Server esta rodando" -ForegroundColor Yellow
        Write-Host "   3. SQL Server aceita conexoes remotas" -ForegroundColor Yellow
    }

    Write-Host ""
    pause
}

function Get-LocalIP {
    Clear-Host
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Magenta
    Write-Host "   Descobrir IP para Acesso Mobile" -ForegroundColor Magenta
    Write-Host "========================================================" -ForegroundColor Magenta
    Write-Host ""

    Write-Host "Seus enderecos IP:" -ForegroundColor Cyan
    Write-Host ""

    Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" } | ForEach-Object {
        Write-Host "   $($_.InterfaceAlias): $($_.IPAddress)" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Para acessar do celular (mesma rede Wi-Fi):" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   http://SEU_IP:5000" -ForegroundColor White
    Write-Host ""
    Write-Host "   Exemplo: http://192.168.1.100:5000" -ForegroundColor Gray
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "IMPORTANTE:" -ForegroundColor Yellow
    Write-Host "   - Celular e PC devem estar na mesma rede Wi-Fi" -ForegroundColor White
    Write-Host "   - Firewall do Windows pode bloquear (libere porta 5000)" -ForegroundColor White
    Write-Host "   - Use o IP da interface Wi-Fi ou Ethernet" -ForegroundColor White
    Write-Host ""
    pause
}

function Clear-Build {
    Clear-Host
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Yellow
    Write-Host "   Limpeza de Arquivos de Build" -ForegroundColor Yellow
    Write-Host "========================================================" -ForegroundColor Yellow
    Write-Host ""

    Write-Host "Limpando arquivos temporarios..." -ForegroundColor Cyan
    Write-Host ""

    Set-Location "Backend"

    if (Test-Path "bin") {
        Write-Host "Removendo pasta bin..." -ForegroundColor Gray
        Remove-Item -Path "bin" -Recurse -Force
    }

    if (Test-Path "obj") {
        Write-Host "Removendo pasta obj..." -ForegroundColor Gray
        Remove-Item -Path "obj" -Recurse -Force
    }

    if (Test-Path "logs") {
        Write-Host "Removendo logs antigos..." -ForegroundColor Gray
        Remove-Item -Path "logs\*.log" -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path "uploads") {
        Write-Host "Removendo uploads de teste..." -ForegroundColor Gray
        Remove-Item -Path "uploads\*.jpg" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "uploads\*.jpeg" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "uploads\*.png" -Force -ErrorAction SilentlyContinue
    }

    Set-Location ".."

    Write-Host ""
    Write-Host "Limpeza concluida!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Executar 'Iniciar Servidor' ira recompilar tudo." -ForegroundColor Cyan
    Write-Host ""
    pause
}

function Open-VSCode {
    Clear-Host
    Write-Host ""
    Write-Host "Abrindo projeto no Visual Studio Code..." -ForegroundColor Cyan
    Write-Host ""

    try {
        code .
        Write-Host "Projeto aberto no VSCode!" -ForegroundColor Green
        Start-Sleep -Seconds 2
    } catch {
        Write-Host "Visual Studio Code nao encontrado no PATH." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Para usar este comando:" -ForegroundColor White
        Write-Host "1. Abra o VSCode" -ForegroundColor Gray
        Write-Host "2. Pressione Ctrl+Shift+P" -ForegroundColor Gray
        Write-Host "3. Digite 'shell command' e selecione 'Install code command in PATH'" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Ou abra manualmente: File - Open Folder - PicStone WEB" -ForegroundColor Gray
        Write-Host ""
        pause
    }
}

function Open-Swagger {
    Write-Host ""
    Write-Host "Abrindo Swagger UI..." -ForegroundColor Cyan
    Start-Sleep -Seconds 1
    Start-Process "http://localhost:5000/swagger"
    Write-Host ""
    Write-Host "Se o servidor nao estiver rodando, use opcao [1] primeiro." -ForegroundColor Yellow
    Write-Host ""
    pause
}

function Open-App {
    Write-Host ""
    Write-Host "Abrindo aplicacao..." -ForegroundColor Cyan
    Start-Sleep -Seconds 1
    Start-Process "http://localhost:5000"
    Write-Host ""
    Write-Host "Se o servidor nao estiver rodando, use opcao [1] primeiro." -ForegroundColor Yellow
    Write-Host ""
    pause
}

function Open-Docs {
    Write-Host ""
    Write-Host "Documentacoes disponiveis:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   - README.md (documentacao completa)" -ForegroundColor White
    Write-Host "   - GUIA_RAPIDO.md (guia de deploy)" -ForegroundColor White
    Write-Host ""
    Write-Host "Abrindo README.md..." -ForegroundColor Cyan
    Start-Sleep -Seconds 1
    Start-Process "README.md"
    pause
}

# Loop principal
do {
    Show-Menu
    $opcao = Read-Host "Escolha uma opcao"

    switch ($opcao) {
        "1" { Start-Server }
        "2" { Stop-Server }
        "3" { Test-SqlConnection }
        "4" { Get-LocalIP }
        "5" { Clear-Build }
        "6" { Open-VSCode }
        "7" { Open-Swagger }
        "8" { Open-App }
        "9" { Open-Docs }
        "0" {
            Write-Host ""
            Write-Host "Ate logo!" -ForegroundColor Green
            Write-Host ""
            Start-Sleep -Seconds 1
            exit
        }
        default {
            Write-Host ""
            Write-Host "Opcao invalida!" -ForegroundColor Red
            Start-Sleep -Seconds 2
        }
    }
} while ($true)
