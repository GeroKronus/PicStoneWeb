using SkiaSharp;
using PicStoneFotoAPI.Helpers;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço para geração de mockups de Kitchen (Cozinha)
    /// Gera 2 versões: Normal e 180° rotacionada
    /// </summary>
    public class KitchenService
    {
        private readonly GraphicsTransformService _transformService;
        private readonly ILogger<KitchenService> _logger;
        private readonly ImageWatermarkService _watermark;
        private readonly ImageManipulationService _imageManipulation;

        // Constantes do canvas
        private const int CANVAS_WIDTH = 1536;
        private const int CANVAS_HEIGHT = 1024;

        // Constantes das porções
        private const int LARGURA_BASE_P1 = 943;
        private const int ALTURA_TRAPEZIO_P1 = 36;
        private const int INCLINACAO_P1 = 92;

        private const int LARGURA_P2 = 943;
        private const int ALTURA_P2 = 21;

        private const int LARGURA_P3 = 17;
        private const int ALTURA_P3 = 315;

        private const int LARGURA_P4 = 35;
        private const int ALTURA_P4 = 315;
        private const int SKEW_P4 = 37;

        private const int LARGURA_P5 = 580;
        private const int ALTURA_P5 = 238;

        private const int LARGURA_P6 = 574;
        private const int ALTURA_P6 = 166;

        private const int LARGURA_P7 = 550;
        private const int ALTURA_P7 = 166;

        public KitchenService(
            GraphicsTransformService transformService,
            ILogger<KitchenService> logger,
            ImageWatermarkService watermark,
            ImageManipulationService imageManipulation)
        {
            _transformService = transformService;
            _logger = logger;
            _watermark = watermark;
            _imageManipulation = imageManipulation;
        }

        /// <summary>
        /// Gera mockup Kitchen #1 (Cozinha com ilha)
        /// </summary>
        /// <param name="imagemCropada">Imagem cropada pelo usuário</param>
        /// <returns>Lista com 2 versões: Normal e 180°</returns>
        public List<SKBitmap> GerarKitchen1(SKBitmap imagemCropada)
        {
            try
            {
                _logger.LogInformation("=== INICIANDO KITCHEN #1 ===");
                _logger.LogInformation("Imagem original: {W}x{H}", imagemCropada.Width, imagemCropada.Height);

                var resultados = new List<SKBitmap>();

                // Gera versão normal
                _logger.LogInformation("--- Gerando versão NORMAL ---");
                var versaoNormal = GerarVersao(imagemCropada, false);
                resultados.Add(versaoNormal);

                // Gera versão rotacionada 180°
                _logger.LogInformation("--- Gerando versão 180° ---");
                using var imagemRotacionada = _imageManipulation.Rotate180(imagemCropada);
                var versao180 = GerarVersao(imagemRotacionada, true);
                resultados.Add(versao180);

                _logger.LogInformation("=== KITCHEN #1 CONCLUÍDO: {Count} versões ===", resultados.Count);
                return resultados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Kitchen #1");
                throw;
            }
        }

        /// <summary>
        /// Gera uma versão do mockup (normal ou rotacionada)
        /// </summary>
        private SKBitmap GerarVersao(SKBitmap imagem, bool isRotated)
        {
            var sufixo = isRotated ? "180°" : "normal";
            _logger.LogInformation("Gerando versão {Sufixo}", sufixo);

            // === RECORTAR AS 7 PORÇÕES ===

            // Porção 1: 66% largura x 45% altura (do TOPO)
            int largura66 = (int)(imagem.Width * 0.66f);
            int altura45 = (int)(imagem.Height * 0.45f);
            int altura5 = (int)(imagem.Height * 0.05f);

            using var porcao1 = RecortarPorcao(imagem, 0, 0, largura66, altura45);
            _logger.LogInformation("Porção 1 (66%x45%): {W}x{H}", porcao1.Width, porcao1.Height);

            // Porção 2: 66% largura x 5% altura (de 45% a 50%)
            using var porcao2 = RecortarPorcao(imagem, 0, altura45, largura66, altura5);
            _logger.LogInformation("Porção 2 (66%x5%): {W}x{H}", porcao2.Width, porcao2.Height);

            // Porção 3: 5% largura x 50% altura (de Y=50% até Y=100%)
            int largura5 = (int)(imagem.Width * 0.05f);
            int altura50 = (int)(imagem.Height * 0.50f);
            int yInicio50 = (int)(imagem.Height * 0.50f);

            using var porcao3 = RecortarPorcao(imagem, 0, yInicio50, largura5, altura50);
            _logger.LogInformation("Porção 3 (5%x50%): {W}x{H}", porcao3.Width, porcao3.Height);

            // Porção 4: 30% largura (X=5% a 35%) x 50% altura (Y=50% a 100%)
            int xInicio5percent = (int)(imagem.Width * 0.05f);
            int largura30 = (int)(imagem.Width * 0.30f);

            using var porcao4 = RecortarPorcao(imagem, xInicio5percent, yInicio50, largura30, altura50);
            _logger.LogInformation("Porção 4 (30%x50%): {W}x{H}", porcao4.Width, porcao4.Height);

            // Porção 5: X=45% até 100%, Y=45% até 100% (55% largura x 55% altura)
            int xInicio45percent = (int)(imagem.Width * 0.45f);
            int yInicio45percent = (int)(imagem.Height * 0.45f);
            int largura55 = imagem.Width - xInicio45percent;
            int altura55 = imagem.Height - yInicio45percent;

            using var porcao5 = RecortarPorcao(imagem, xInicio45percent, yInicio45percent, largura55, altura55);
            _logger.LogInformation("Porção 5 (55%x55%): {W}x{H}", porcao5.Width, porcao5.Height);

            // Porção 6: 55% largura x 50% altura (do TOPO)
            int largura55p = (int)(imagem.Width * 0.55f);
            int altura50p = (int)(imagem.Height * 0.50f);

            using var porcao6 = RecortarPorcao(imagem, 0, 0, largura55p, altura50p);
            _logger.LogInformation("Porção 6 (55%x50%): {W}x{H}", porcao6.Width, porcao6.Height);

            // Porção 7: 55% largura x 50% altura (do TOPO) - será flip horizontal
            using var porcao7 = RecortarPorcao(imagem, 0, 0, largura55p, altura50p);
            _logger.LogInformation("Porção 7 (55%x50%): {W}x{H}", porcao7.Width, porcao7.Height);

            // === TRANSFORMAR E REDIMENSIONAR ===

            // Porção 1: Trapézio
            using var p1Redim = porcao1.Resize(new SKImageInfo(LARGURA_BASE_P1, ALTURA_TRAPEZIO_P1), SKBitmapHelper.HighQuality);
            using var p1Transform = _transformService.TransformPerspective(
                input: p1Redim,
                canvasWidth: LARGURA_BASE_P1,
                canvasHeight: ALTURA_TRAPEZIO_P1,
                topLeft: new SKPoint(INCLINACAO_P1, 0),
                topRight: new SKPoint(LARGURA_BASE_P1 - INCLINACAO_P1, 0),
                bottomLeft: new SKPoint(0, ALTURA_TRAPEZIO_P1),
                bottomRight: new SKPoint(LARGURA_BASE_P1, ALTURA_TRAPEZIO_P1)
            );

            // Porção 2: Retângulo simples
            using var p2Redim = porcao2.Resize(new SKImageInfo(LARGURA_P2, ALTURA_P2), SKBitmapHelper.HighQuality);

            // Porção 3: Coluna
            using var p3Redim = porcao3.Resize(new SKImageInfo(LARGURA_P3, ALTURA_P3), SKBitmapHelper.HighQuality);

            // Porção 4: Skew (lateral direita sobe)
            using var p4Redim = porcao4.Resize(new SKImageInfo(LARGURA_P4, ALTURA_P4), SKBitmapHelper.HighQuality);
            int alturaCanvasP4 = ALTURA_P4 + SKEW_P4;
            using var p4Transform = _transformService.TransformPerspective(
                input: p4Redim,
                canvasWidth: LARGURA_P4,
                canvasHeight: alturaCanvasP4,
                topLeft: new SKPoint(0, SKEW_P4),
                topRight: new SKPoint(LARGURA_P4, 0),
                bottomLeft: new SKPoint(0, alturaCanvasP4),
                bottomRight: new SKPoint(LARGURA_P4, ALTURA_P4)
            );

            // Porção 5: Retângulo grande
            using var p5Redim = porcao5.Resize(new SKImageInfo(LARGURA_P5, ALTURA_P5), SKBitmapHelper.HighQuality);

            // Porção 6: Armário esquerdo (574x166)
            using var p6Redim = porcao6.Resize(new SKImageInfo(LARGURA_P6, ALTURA_P6), SKBitmapHelper.HighQuality);

            // Porção 7: Armário direito (flip horizontal) (550x166)
            using var p7Flip = _imageManipulation.FlipHorizontal(porcao7);
            using var p7Redim = p7Flip.Resize(new SKImageInfo(LARGURA_P7, ALTURA_P7), SKBitmapHelper.HighQuality);

            // === MONTAR CANVAS ===
            var mockup = new SKBitmap(CANVAS_WIDTH, CANVAS_HEIGHT, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(mockup);
            canvas.Clear(SKColors.Transparent);

            // Ordem das camadas (de baixo para cima):
            // CAMADA 1: Porção 4 (paralelogramo/skew) em (311, 644)
            canvas.DrawBitmap(p4Transform, 311, 644);

            // CAMADA 2: Porção 5 (retângulo grande) em (346, 681)
            canvas.DrawBitmap(p5Redim, 346, 681);

            // CAMADA 3: Porção 1 (trapézio) em (294, 624)
            canvas.DrawBitmap(p1Transform, 294, 624);

            // CAMADA 4: Porção 2 (retângulo) em (294, 660)
            canvas.DrawBitmap(p2Redim, 294, 660);

            // CAMADA 5: Porção 3 (coluna) em (294, 681)
            canvas.DrawBitmap(p3Redim, 294, 681);

            // CAMADA 6: Porção 6 (armário esquerdo) em (203, 432)
            canvas.DrawBitmap(p6Redim, 203, 432);

            // CAMADA 7: Porção 7 (armário direito flip) em (777, 432)
            canvas.DrawBitmap(p7Redim, 777, 432);

            // CAMADA FINAL: Overlay cozinha1.webp
            var overlayPath = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Cozinhas", "cozinha1.webp");
            if (File.Exists(overlayPath))
            {
                using var overlayBitmap = SKBitmap.Decode(overlayPath);
                if (overlayBitmap != null)
                {
                    if (overlayBitmap.Width != CANVAS_WIDTH || overlayBitmap.Height != CANVAS_HEIGHT)
                    {
                        using var overlayResized = overlayBitmap.Resize(new SKImageInfo(CANVAS_WIDTH, CANVAS_HEIGHT), SKBitmapHelper.HighQuality);
                        canvas.DrawBitmap(overlayResized, 0, 0);
                    }
                    else
                    {
                        canvas.DrawBitmap(overlayBitmap, 0, 0);
                    }
                    _logger.LogInformation("Overlay cozinha1.webp aplicado");
                }
            }
            else
            {
                _logger.LogWarning("Overlay não encontrado: {Path}", overlayPath);
            }

            // Adiciona marca d'água
            _watermark.AddWatermark(canvas, CANVAS_WIDTH, CANVAS_HEIGHT);
            _logger.LogInformation("Marca d'água adicionada - versão {Sufixo}", sufixo);

            return mockup;
        }

        /// <summary>
        /// Recorta uma porção da imagem
        /// </summary>
        private SKBitmap RecortarPorcao(SKBitmap source, int x, int y, int width, int height)
        {
            var porcao = new SKBitmap(width, height);
            using var canvas = new SKCanvas(porcao);
            var srcRect = new SKRectI(x, y, x + width, y + height);
            canvas.DrawBitmap(source, srcRect, new SKRect(0, 0, width, height));
            return porcao;
        }
    }
}
