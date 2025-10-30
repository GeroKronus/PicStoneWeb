# Iniciar Servidor Local - PicStone

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "   PicStone - Inicializador Local" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ""

# Verifica se o .NET esta instalado
Write-Host "[1/4] Verificando .NET SDK..." -ForegroundColor Cyan

try {
    $dotnetVersion = dotnet --version
    Write-Host "OK - .NET SDK encontrado: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERRO: .NET SDK nao encontrado!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Por favor, instale o .NET 8.0 SDK de:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
    Write-Host ""
    pause
    exit 1
}

Write-Host ""

# Navega para o diretorio do Backend
Set-Location "Backend"

# Verifica se o arquivo .csproj existe
if (-not (Test-Path "PicStoneFotoAPI.csproj")) {
    Write-Host "ERRO: Arquivo PicStoneFotoAPI.csproj nao encontrado!" -ForegroundColor Red
    Write-Host ""
    pause
    Set-Location ".."
    exit 1
}

# Restaura dependencias
Write-Host "[2/4] Restaurando dependencias..." -ForegroundColor Cyan
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO ao restaurar dependencias!" -ForegroundColor Red
    Write-Host ""
    pause
    Set-Location ".."
    exit 1
}

Write-Host "OK - Dependencias restauradas com sucesso!" -ForegroundColor Green
Write-Host ""

# Compila a aplicacao
Write-Host "[3/4] Compilando aplicacao..." -ForegroundColor Cyan
dotnet build --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO ao compilar!" -ForegroundColor Red
    Write-Host ""
    pause
    Set-Location ".."
    exit 1
}

Write-Host "OK - Compilacao concluida!" -ForegroundColor Green
Write-Host ""

# Inicia o servidor
Write-Host "[4/4] Iniciando servidor..." -ForegroundColor Cyan
Write-Host ""
Write-Host "========================================================" -ForegroundColor Yellow
Write-Host "   APLICACAO RODANDO!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "URL Local:        http://localhost:5000" -ForegroundColor Cyan
Write-Host "Celular:          http://SEU_IP:5000" -ForegroundColor Cyan
Write-Host "Usuario:          admin" -ForegroundColor White
Write-Host "Senha:            admin123" -ForegroundColor White
Write-Host "Swagger:          http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "========================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Dica: Para descobrir seu IP local, use a opcao [4] do menu" -ForegroundColor Gray
Write-Host ""
Write-Host "Pressione Ctrl+C para parar o servidor..." -ForegroundColor Yellow
Write-Host ""

dotnet run --no-build

Set-Location ".."
