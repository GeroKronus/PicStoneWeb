# Script de teste completo do fluxo de mockup com verificação de marca d'água

$ErrorActionPreference = "Stop"

Write-Host "=== TESTE COMPLETO DE MOCKUP COM MARCA D'ÁGUA ===" -ForegroundColor Cyan

# 1. Login
Write-Host "`n1. Fazendo login..." -ForegroundColor Yellow
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"username":"admin","password":"admin123"}'

$token = $loginResponse.token
Write-Host "   Token obtido: $($token.Substring(0,20))..." -ForegroundColor Green

# 2. Criar imagem de teste
Write-Host "`n2. Criando imagem de teste (1487x800)..." -ForegroundColor Yellow
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap(1487, 800)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([System.Drawing.Color]::LightGray)

# Adiciona padrão de mármore simulado
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.FillRectangle($brush, 100, 100, 1287, 600)

$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::Gray, 3)
for ($i = 0; $i -lt 20; $i++) {
    $x1 = Get-Random -Minimum 0 -Maximum 1487
    $y1 = Get-Random -Minimum 0 -Maximum 800
    $x2 = Get-Random -Minimum 0 -Maximum 1487
    $y2 = Get-Random -Minimum 0 -Maximum 800
    $graphics.DrawLine($pen, $x1, $y1, $x2, $y2)
}

$testImagePath = "D:\Claude Code\PicStone WEB\test-marble.jpg"
$bitmap.Save($testImagePath, [System.Drawing.Imaging.ImageFormat]::Jpeg)
$graphics.Dispose()
$bitmap.Dispose()

Write-Host "   Imagem criada: $testImagePath" -ForegroundColor Green

# 3. Upload da imagem como crop simulado
Write-Host "`n3. Fazendo upload da imagem cropada..." -ForegroundColor Yellow
$boundary = [System.Guid]::NewGuid().ToString()
$LF = "`r`n"

$imageBytes = [System.IO.File]::ReadAllBytes($testImagePath)
$imageContent = [System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($imageBytes)

$bodyLines = @(
    "--$boundary",
    "Content-Disposition: form-data; name=`"ImagemCropada`"; filename=`"test-marble.jpg`"",
    "Content-Type: image/jpeg",
    "",
    $imageContent,
    "--$boundary",
    "Content-Disposition: form-data; name=`"TipoCavalete`"",
    "",
    "simples",
    "--$boundary",
    "Content-Disposition: form-data; name=`"Fundo`"",
    "",
    "claro",
    "--$boundary--"
)

$body = $bodyLines -join $LF

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/mockup/gerar" `
        -Method POST `
        -Headers @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "multipart/form-data; boundary=$boundary"
        } `
        -Body ([System.Text.Encoding]::GetEncoding('iso-8859-1').GetBytes($body))

    Write-Host "   Mockup gerado com sucesso!" -ForegroundColor Green
    Write-Host "   Mensagem: $($response.mensagem)" -ForegroundColor Green
    Write-Host "   Arquivos gerados:" -ForegroundColor Cyan

    foreach ($caminho in $response.caminhosGerados) {
        Write-Host "      - $caminho" -ForegroundColor White
    }

    # 4. Verificar se os arquivos foram criados
    Write-Host "`n4. Verificando arquivos gerados..." -ForegroundColor Yellow
    $uploadDir = "D:\Claude Code\PicStone WEB\Backend\uploads"

    foreach ($caminho in $response.caminhosGerados) {
        $filePath = Join-Path $uploadDir $caminho
        if (Test-Path $filePath) {
            $fileInfo = Get-Item $filePath
            Write-Host "   ✓ $caminho ($($fileInfo.Length) bytes)" -ForegroundColor Green

            # Verificar dimensões da imagem
            $img = [System.Drawing.Image]::FromFile($filePath)
            Write-Host "     Dimensões: $($img.Width)x$($img.Height)" -ForegroundColor Cyan
            $img.Dispose()
        } else {
            Write-Host "   ✗ $caminho NÃO ENCONTRADO!" -ForegroundColor Red
        }
    }

    # 5. Verificar marca d'água visualmente (abrir imagens)
    Write-Host "`n5. IMPORTANTE: Verificar marca d'água manualmente" -ForegroundColor Yellow
    Write-Host "   As imagens serão abertas para inspeção visual." -ForegroundColor Yellow
    Write-Host "   Verifique se a logo amarela aparece no canto inferior direito." -ForegroundColor Yellow
    Read-Host "   Pressione ENTER para abrir as imagens"

    foreach ($caminho in $response.caminhosGerados) {
        $filePath = Join-Path $uploadDir $caminho
        if (Test-Path $filePath) {
            Start-Process $filePath
            Start-Sleep -Milliseconds 500
        }
    }

    Write-Host "`n=== TESTE CONCLUÍDO ===" -ForegroundColor Cyan
    Write-Host "Verifique visualmente se a marca d'água está presente em todas as imagens." -ForegroundColor Yellow

} catch {
    Write-Host "   ERRO ao gerar mockup!" -ForegroundColor Red
    Write-Host "   Detalhes: $_" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

# Limpar arquivo de teste
Remove-Item $testImagePath -ErrorAction SilentlyContinue
