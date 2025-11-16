# Implementação Living Room 2 (Liveroom #3) - 90% CONCLUÍDA

## Status Atual

✅ **BACKEND CONCLUÍDO:**
1. ✅ Método `GerarLivingRoom2()` adicionado em `LivingRoomService.cs` (linhas 160-312)
2. ✅ Métodos helper adicionados: `FlipVertical()` e `Rotate180()` (linhas 354-384)
3. ✅ **Endpoint `livingroom2/progressive` adicionado em `MockupController.cs` (linhas 1849-1974)**

✅ **FRONTEND CONCLUÍDO:**
1. ✅ Card Living Room 2 adicionado no `index.html` (linhas 654-660)
2. ✅ JavaScript já suporta múltiplos tipos (função usa `numero` para determinar endpoint)
3. ✅ Thumbnail `thumb-liveroom2.webp` criado TEMPORARIAMENTE (cópia de sala1)
4. ✅ Arquivos copiados para `Backend/wwwroot`

⚠️ **PENDENTE (NÃO CRÍTICO):**
1. ⚠️ Substituir `thumb-liveroom2.webp` por thumbnail real mostrando resultado BookMatch 2x2 + Skew
   - Atualmente usando imagem temporária (cópia de Living Room 1)
   - Funcionalidade completa, apenas thumbnail é provisório

## Próximos Passos (FRONTEND)

### 1. Adicionar endpoint no MockupController.cs

Adicionar após o endpoint `livingroom1/progressive` (linha ~1850):

```csharp
[HttpPost("livingroom2/progressive")]
public async Task GerarLivingRoom2Progressive(
    [FromForm] string imageId,
    [FromForm] string fundo = "claro",
    [FromForm] int? cropX = null,
    [FromForm] int? cropY = null,
    [FromForm] int? cropWidth = null,
    [FromForm] int? cropHeight = null)
{
    Response.ContentType = "text/event-stream";
    Response.Headers.Add("Cache-Control", "no-cache");
    Response.Headers.Add("Connection", "keep-alive");

    try
    {
        _logger.LogInformation("=== INÍCIO Living Room #2 Progressive (imageId + crop) ===");

        var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await EnviarEventoSSE("inicio", new { mensagem = "Gerando Living Room #2..." });

        // Carrega imagem do servidor usando imageId
        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        var caminhoImagemOriginal = Path.Combine(imagePath, imageId);

        if (!System.IO.File.Exists(caminhoImagemOriginal))
        {
            _logger.LogError("Imagem não encontrada em wwwroot/images: {ImageId}", imageId);
            await EnviarEventoSSE("erro", new { mensagem = $"Imagem não encontrada: {imageId}" });
            return;
        }

        using var bitmapOriginal = SKBitmap.Decode(caminhoImagemOriginal);
        if (bitmapOriginal == null)
        {
            await EnviarEventoSSE("erro", new { mensagem = "Erro ao decodificar imagem original" });
            return;
        }

        _logger.LogInformation("Imagem original carregada: {W}x{H}", bitmapOriginal.Width, bitmapOriginal.Height);

        // Aplica crop se coordenadas foram fornecidas
        SKBitmap bitmapCropado;
        if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
        {
            _logger.LogInformation("Aplicando crop: ({X},{Y}) {W}x{H}", cropX.Value, cropY.Value, cropWidth.Value, cropHeight.Value);

            var info = new SKImageInfo(cropWidth.Value, cropHeight.Value);
            bitmapCropado = new SKBitmap(info);

            using var canvas = new SKCanvas(bitmapCropado);
            var srcRect = new SKRect(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
            var destRect = new SKRect(0, 0, cropWidth.Value, cropHeight.Value);
            canvas.DrawBitmap(bitmapOriginal, srcRect, destRect);

            _logger.LogInformation("Crop aplicado com sucesso: {W}x{H}", bitmapCropado.Width, bitmapCropado.Height);
        }
        else
        {
            _logger.LogWarning("⚠️ Nenhuma coordenada de crop fornecida - usando imagem ORIGINAL");
            bitmapCropado = bitmapOriginal.Copy();
        }

        _logger.LogInformation("Imagem final para processamento: {W}x{H}", bitmapCropado.Width, bitmapCropado.Height);

        // Gera os 4 quadrantes usando LivingRoomService
        await EnviarEventoSSE("progresso", new { etapa = "Processando transformações...", porcentagem = 10 });

        var quadrantesBitmaps = _livingRoomService.GerarLivingRoom2(bitmapCropado);

        if (quadrantesBitmaps == null || quadrantesBitmaps.Count == 0)
        {
            await EnviarEventoSSE("erro", new { mensagem = "Erro ao gerar Living Room #2" });
            return;
        }

        _logger.LogInformation("Living Room #2 gerado: {Count} quadrantes", quadrantesBitmaps.Count);

        // Salva cada quadrante e envia progressivamente
        var caminhos = new List<string>();
        var porcentagemPorQuadrante = 90 / quadrantesBitmaps.Count;

        for (int i = 0; i < quadrantesBitmaps.Count; i++)
        {
            var quadrante = i + 1;
            var nomeArquivo = $"liveroom2_q{quadrante}_user{usuarioId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var caminhoCompleto = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "mockups", nomeArquivo);

            // Salva quadrante
            using (var image = SKImage.FromBitmap(quadrantesBitmaps[i]))
            using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 95))
            using (var stream = File.OpenWrite(caminhoCompleto))
            {
                data.SaveTo(stream);
            }

            caminhos.Add(nomeArquivo);

            // Envia evento de progresso incremental
            var porcentagem = 10 + (porcentagemPorQuadrante * (i + 1));
            await EnviarEventoSSE("progresso", new
            {
                quadrante = quadrante,
                caminho = nomeArquivo,
                porcentagem = porcentagem
            });

            _logger.LogInformation("Quadrante {Q}/4 salvo: {Nome}", quadrante, nomeArquivo);
        }

        // Dispose dos bitmaps após salvar
        foreach (var bitmap in quadrantesBitmaps)
        {
            bitmap.Dispose();
        }

        // Envia evento de sucesso
        await EnviarEventoSSE("sucesso", new { caminhos = caminhos });

        _logger.LogInformation("=== Living Room #2 Progressive CONCLUÍDO ===");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao gerar Living Room #2 Progressive");
        await EnviarEventoSSE("erro", new { mensagem = $"Erro interno: {ex.Message}" });
    }
}
```

### 2. Frontend - Adicionar card Living Room 2

Em `index.html`, adicionar card na grade de living rooms (após Living Room 1):

```html
<div class="ambiente-card" onclick="selectLivingRoomAndGenerate(2)">
    <img src="images/thumb-liveroom2.webp" alt="Living Room 2">
    <div class="ambiente-info">
        <h3>Living Room 2</h3>
        <p>BookMatch 2x2 + Skew</p>
    </div>
</div>
```

### 3. Frontend - Modificar app.js

Adicionar lógica para chamar endpoint correto baseado no tipo:

```javascript
function selectLivingRoomAndGenerate(tipo) {
    if (!state.sharedImageState?.uploadedImageId) {
        alert('Por favor, faça o crop da imagem primeiro');
        return;
    }

    state.livingRoomState.selectedType = tipo;

    // Define endpoint baseado no tipo
    const endpoints = {
        1: '/api/mockup/livingroom1/progressive',
        2: '/api/mockup/livingroom2/progressive'
    };

    const endpoint = endpoints[tipo];
    if (!endpoint) {
        alert('Tipo de Living Room inválido');
        return;
    }

    generateLivingRoomProgressive(endpoint, tipo);
}

async function generateLivingRoomProgressive(endpoint, tipo) {
    // ... código existente ...

    // Atualiza mensagem de loading
    elements.loadingMessage.textContent = `Gerando Living Room ${tipo}...`;

    // ... resto do código ...
}
```

### 4. Criar thumbnail

Criar imagem `thumb-liveroom2.webp` em:
- `Frontend/images/thumb-liveroom2.webp`
- `Backend/wwwroot/images/thumb-liveroom2.webp`

### 5. Teste

1. Build e restart do backend
2. Hard refresh no frontend
3. Upload de imagem → Crop → Living Room → Card Living Room 2
4. Verificar geração dos 4 quadrantes
5. Testar botão "Gerar Novos"

## Parâmetros Living Room 2 (Liveroom #3)

```
Moldura: 1080x1334
Quadro sem Skew: 316x979
Coordenadas Plot: (456, 7)
Lado Maior: 979
Lado Menor: 830
Fator Inclinação: 161
Largura Tile: 700px
```

## BookMatch 2x2 - Arranjos

**Quadrante 1:**
```
ORI  | FH
FV   | 180
```

**Quadrante 2:**
```
FH   | ORI
180  | FV
```

**Quadrante 3:**
```
FV   | 180
ORI  | FH
```

**Quadrante 4:**
```
180  | FV
FH   | ORI
```

## Transformações Aplicadas

1. Redimensiona para largura 700px
2. Cria 4 versões (ORI, FV, FH, 180)
3. Monta 4 mosaicos 2x2 diferentes
4. Aplica DistortionInclina (979 → 830)
5. Aplica Skew (transformação de 3 pontos)
6. Redimensiona para 316x979
7. Plota em canvas 1080x1334 na posição (456, 7)
