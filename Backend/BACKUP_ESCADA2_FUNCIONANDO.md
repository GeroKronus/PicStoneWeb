# BACKUP - CÓDIGO FUNCIONANDO DA ESCADA #2
**Data**: 2025-11-18
**Status**: ✅ FUNCIONANDO PERFEITAMENTE

## Método GerarStairs2 (StairsService.cs - linhas 493-670)

Este é o código COMPLETO e FUNCIONANDO da Escada #2.
**NÃO MODIFICAR** sem backup adicional!

```csharp
/// <summary>
/// Gera mockup de Escada #2 - IMPLEMENTAÇÃO EXATA DO VB.NET
/// </summary>
public SKBitmap GerarStairs2(SKBitmap imagemOriginal, bool rotacionado = false)
{
    _logger.LogInformation("=== GERANDO STAIRS #2 (CÓDIGO REFATORADO - SEM DEBUG) ===");

    // PASSO 1: Redimensionar para 1400x600 (50% do tamanho original VB.NET)
    const int LARGURA = 1400;
    const int ALTURA = 600;
    var imagemRedimensionada = imagemOriginal.Resize(new SKImageInfo(LARGURA, ALTURA), SKFilterQuality.High);
    _logger.LogInformation("Imagem redimensionada: {Width}x{Height}", LARGURA, ALTURA);

    // PASSO 1.5: Rotacionar 180° se necessário
    if (rotacionado)
    {
        var temp = new SKBitmap(imagemRedimensionada.Width, imagemRedimensionada.Height);
        using var canvas = new SKCanvas(temp);
        canvas.Translate(imagemRedimensionada.Width / 2f, imagemRedimensionada.Height / 2f);
        canvas.RotateDegrees(180);
        canvas.Translate(-imagemRedimensionada.Width / 2f, -imagemRedimensionada.Height / 2f);
        canvas.DrawBitmap(imagemRedimensionada, 0, 0);
        imagemRedimensionada.Dispose();
        imagemRedimensionada = temp;
        _logger.LogInformation("Imagem rotacionada 180°");
    }

    // DIVISÃO PROPORCIONAL: Mantém proporção entre degrau e espelho
    const int LARGURA_DEGRAU = 153;   // Todos os degraus com mesmo tamanho
    const int LARGURA_ESPELHO = 96;    // Todos os espelhos com mesmo tamanho

    var coordenadasDegraus = new[] {
        (x: LARGURA - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
        (x: LARGURA - 1*LARGURA_DEGRAU - 1*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
        (x: LARGURA - 2*LARGURA_DEGRAU - 2*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
        (x: LARGURA - 3*LARGURA_DEGRAU - 3*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
        (x: LARGURA - 4*LARGURA_DEGRAU - 4*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
        (x: LARGURA - 5*LARGURA_DEGRAU - 5*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU)
    };

    var coordenadasEspelhos = new[] {
        (x: LARGURA - 1*LARGURA_DEGRAU - 1*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
        (x: LARGURA - 2*LARGURA_DEGRAU - 2*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
        (x: LARGURA - 3*LARGURA_DEGRAU - 3*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
        (x: LARGURA - 4*LARGURA_DEGRAU - 4*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
        (x: LARGURA - 5*LARGURA_DEGRAU - 5*LARGURA_ESPELHO, largura: LARGURA_ESPELHO)
    };

    _logger.LogInformation("🔹 Gerando 6 DEGRAUS (em memória)...");

    // PASSO 2: Gerar todos os 6 DEGRAUS (manter em memória)
    var degrausTransformados = new SKBitmap[6];
    for (int i = 0; i < 6; i++)
    {
        var (x, largura) = coordenadasDegraus[i];
        var degrauNum = i + 1;

        var degrauRect = new SKRectI(x, 0, x + largura, ALTURA);
        var degrauOriginal = new SKBitmap(largura, ALTURA);
        using (var canvas = new SKCanvas(degrauOriginal))
        {
            canvas.DrawBitmap(imagemRedimensionada, degrauRect, new SKRect(0, 0, largura, ALTURA));
        }

        degrausTransformados[i] = TransformarDegrauEscada2(degrauOriginal);
        _logger.LogInformation($"   ✅ DEGRAU{degrauNum}: {degrausTransformados[i].Width}x{degrausTransformados[i].Height}");
        degrauOriginal.Dispose();
    }

    _logger.LogInformation("🔹 Gerando 5 ESPELHOS (em memória)...");

    // PASSO 3: Gerar todos os 5 ESPELHOS (manter em memória)
    var espelhosTransformados = new SKBitmap[5];
    for (int i = 0; i < 5; i++)
    {
        var (x, largura) = coordenadasEspelhos[i];
        var espelhoNum = i + 1;

        var espelhoRect = new SKRectI(x, 0, x + largura, ALTURA);
        var espelhoOriginal = new SKBitmap(largura, ALTURA);
        using (var canvas = new SKCanvas(espelhoOriginal))
        {
            canvas.DrawBitmap(imagemRedimensionada, espelhoRect, new SKRect(0, 0, largura, ALTURA));
        }

        espelhosTransformados[i] = TransformarEspelhoEscada2(espelhoOriginal);
        _logger.LogInformation($"   ✅ ESPELHO{espelhoNum}: {espelhosTransformados[i].Width}x{espelhosTransformados[i].Height}");
        espelhoOriginal.Dispose();
    }

    _logger.LogInformation("🖼️ Montando canvas 2100x2100...");

    // PASSO 4: COMPOSIÇÃO FINAL NO CANVAS 2100x2100
    const int CANVAS_WIDTH = 2100;
    const int CANVAS_HEIGHT = 2100;
    var canvasFinal = new SKBitmap(CANVAS_WIDTH, CANVAS_HEIGHT);

    using (var canvas = new SKCanvas(canvasFinal))
    {
        canvas.Clear(SKColors.Transparent);

        // Usar peças dos arrays de memória
        canvas.DrawBitmap(degrausTransformados[0], 1587, 334);
        canvas.DrawBitmap(espelhosTransformados[0], 1491, 484);
        canvas.DrawBitmap(degrausTransformados[1], 1338, 484);
        canvas.DrawBitmap(espelhosTransformados[1], 1242, 634);
        canvas.DrawBitmap(degrausTransformados[2], 1089, 634);
        canvas.DrawBitmap(espelhosTransformados[2], 993, 784);
        canvas.DrawBitmap(degrausTransformados[3], 840, 784);
        canvas.DrawBitmap(espelhosTransformados[3], 744, 934);
        canvas.DrawBitmap(degrausTransformados[4], 591, 934);
        canvas.DrawBitmap(espelhosTransformados[4], 495, 1084);
        canvas.DrawBitmap(degrausTransformados[5], 342, 1084);
    }

    // PASSO 5: ROTACIONAR 60 GRAUS
    _logger.LogInformation("🔄 Rotação 60°...");
    var canvasRotacionado = RotacionarImagemComOffset(canvasFinal, 60, 90, 0);

    // PASSO 6: COMPRESSÃO HORIZONTAL
    _logger.LogInformation("📐 Compressão 2100x2100 → 1500x2100...");
    var canvasComprimido = canvasRotacionado.Resize(new SKImageInfo(1500, 2100), SKFilterQuality.High);

    // PASSO 7: APLICAR 2 CAMADAS
    _logger.LogInformation("🎨 Efeito 2 camadas...");
    var canvasDuasCamadas = new SKBitmap(1500, 2100);
    using (var canvas = new SKCanvas(canvasDuasCamadas))
    {
        canvas.Clear(SKColors.Transparent);
        const int OFFSET_GLOBAL_X = -40;
        canvas.DrawBitmap(canvasComprimido, OFFSET_GLOBAL_X, 0);
        canvas.DrawBitmap(canvasComprimido, OFFSET_GLOBAL_X + 20, -20);
    }

    // PASSO 8: CROP E OFFSETS FINAIS
    _logger.LogInformation("✂️ Crop final...");
    const int CROP_TOP = 460;
    const int CROP_BOTTOM = 360;
    const int OFFSET_FINAL_X = -15;
    const int OFFSET_FINAL_Y = 103;
    const int CANVAS_FINAL_WIDTH = 1500;
    const int CANVAS_FINAL_HEIGHT = 1280;

    var canvasFinalComOffsets = new SKBitmap(CANVAS_FINAL_WIDTH, CANVAS_FINAL_HEIGHT);
    using (var canvas = new SKCanvas(canvasFinalComOffsets))
    {
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(canvasDuasCamadas, OFFSET_FINAL_X, OFFSET_FINAL_Y - CROP_TOP);
    }

    // PASSO 9: COMPOR COM OVERLAY
    _logger.LogInformation("🎨 Overlay escada2.webp...");
    var overlayPath = Path.Combine("MockupResources", "Escadas", "escada2.webp");
    SKBitmap canvasComOverlay;

    using (var overlayStream = File.OpenRead(overlayPath))
    using (var overlay = SKBitmap.Decode(overlayStream))
    {
        canvasComOverlay = new SKBitmap(overlay.Width, overlay.Height);
        using (var canvas = new SKCanvas(canvasComOverlay))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(canvasFinalComOffsets, 0, 0);
            canvas.DrawBitmap(overlay, 0, 0);
        }
    }

    // Liberar memória
    imagemRedimensionada.Dispose();
    canvasFinal.Dispose();
    canvasRotacionado.Dispose();
    canvasComprimido.Dispose();
    canvasDuasCamadas.Dispose();
    canvasFinalComOffsets.Dispose();

    foreach (var degrau in degrausTransformados)
    {
        degrau?.Dispose();
    }
    foreach (var espelho in espelhosTransformados)
    {
        espelho?.Dispose();
    }

    return canvasComOverlay;
}
```

## Métodos de Transformação Utilizados

A Escada #2 usa os seguintes métodos auxiliares:
- `TransformarDegrauEscada2()` - Transforma degrau em paralogramo ASCENDENTE
- `TransformarEspelhoEscada2()` - Transforma espelho em paralogramo DESCENDENTE
- `RotacionarImagemComOffset()` - Rotaciona a composição final em 60°

## Características Técnicas
- **Dimensões iniciais**: 1400x600px
- **Canvas intermediário**: 2100x2100px
- **Canvas final**: 1500x1280px
- **Degraus**: 6 peças (153px cada)
- **Espelhos**: 5 peças (96px cada)
- **Overlay**: escada2.webp (1500x1280px)
- **Rotação**: 60° com offsets (90, 0)
- **Compressão horizontal**: 2100 → 1500px
- **Efeito 2 camadas**: Offset global X=-40, camada superior (+20, -20)
- **Crop**: -460px topo, -360px baixo
- **Offsets finais**: X=-15, Y=103
