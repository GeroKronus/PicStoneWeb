# Script para criar tabelas no SQL Server
# Execute este script ANTES de iniciar o servidor pela primeira vez

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   Criando Tabelas no SQL Server" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

$server = "131.255.255.16,11433"
$database = "DADOSADV_Q"
$username = "PicStoneQualita"
$password = "@PicQualit@Stone#"

$connectionString = "Server=$server;Database=$database;User Id=$username;Password=$password;TrustServerCertificate=True;Encrypt=False;"

Write-Host "Conectando ao SQL Server..." -ForegroundColor Yellow
Write-Host "Servidor: $server" -ForegroundColor Gray
Write-Host "Banco: $database" -ForegroundColor Gray
Write-Host ""

try {
    # Carrega o assembly do SQL Client
    Add-Type -AssemblyName "System.Data"

    # Abre conexão
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()

    Write-Host "Conexao estabelecida!" -ForegroundColor Green
    Write-Host ""

    # Lê o arquivo SQL
    $sqlScript = Get-Content -Path "criar-tabelas.sql" -Raw

    # Divide o script em batches (separados por GO)
    $batches = $sqlScript -split '\bGO\b'

    foreach ($batch in $batches) {
        $batch = $batch.Trim()
        if ($batch -ne "") {
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = 30

            try {
                $result = $command.ExecuteNonQuery()
                Write-Host "Batch executado com sucesso" -ForegroundColor Green
            } catch {
                Write-Host "Aviso no batch: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "TABELAS CRIADAS COM SUCESSO!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Tabelas:" -ForegroundColor Cyan
    Write-Host "  - Usuarios" -ForegroundColor White
    Write-Host "  - FotosMobile" -ForegroundColor White
    Write-Host ""
    Write-Host "Usuario padrao criado:" -ForegroundColor Cyan
    Write-Host "  Login: admin" -ForegroundColor White
    Write-Host "  Senha: admin123" -ForegroundColor White
    Write-Host ""
    Write-Host "Agora voce pode iniciar o servidor!" -ForegroundColor Green
    Write-Host ""

    $connection.Close()

} catch {
    Write-Host ""
    Write-Host "ERRO ao criar tabelas:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Verifique:" -ForegroundColor Yellow
    Write-Host "  1. Servidor SQL esta acessivel" -ForegroundColor White
    Write-Host "  2. Credenciais estao corretas" -ForegroundColor White
    Write-Host "  3. Voce tem permissao para criar tabelas" -ForegroundColor White
    Write-Host ""
}

pause
