using SkiaSharp;
using System;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço de transformações gráficas avançadas
    /// Baseado no GraphicsTransform.vb do PicStone PageMaker
    /// </summary>
    public class GraphicsTransformService
    {
        private readonly ILogger<GraphicsTransformService> _logger;

        public GraphicsTransformService(ILogger<GraphicsTransformService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Aplica transformação Skew (inclinação) com perspectiva 3D
        /// </summary>
        /// <param name="input">Bitmap de entrada</param>
        /// <param name="ratio">Razão de compressão vertical</param>
        /// <param name="distanciaTopo">Distância do topo em pixels</param>
        /// <returns>Bitmap transformado</returns>
        public SKBitmap Skew(SKBitmap input, float ratio, int distanciaTopo)
        {
            int w = input.Width;
            int h = input.Height;
            float h2 = h / ratio;

            _logger.LogInformation($"Skew: input={w}x{h}, ratio={ratio}, distanciaTopo={distanciaTopo}, h2={h2}");

            // Calcula tamanho necessário para a surface considerando a transformação
            int surfaceHeight = (int)Math.Max(h, h2 + distanciaTopo + 100);

            var surface = SKSurface.Create(new SKImageInfo(w, surfaceHeight));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Calcula matriz de transformação 2D para perspectiva
            float[] t = Transform2d(w, h, 0, distanciaTopo, w, 0, 0, h2 + distanciaTopo, w, h);

            _logger.LogInformation($"Transform2d result: [{t[0]}, {t[1]}, {t[2]}, {t[3]}, {t[4]}, {t[5]}, {t[6]}, {t[7]}, {t[8]}]");

            // SKMatrix expects column-major order!
            // Transform2d returns: [m11, m12, m13, m21, m22, m23, m31, m32, m33]
            // SKMatrix layout: ScaleX=m11, SkewX=m21, TransX=m31, SkewY=m12, ScaleY=m22, TransY=m32, Persp0=m13, Persp1=m23, Persp2=m33
            var matrix = new SKMatrix
            {
                ScaleX = t[0],  // m11
                SkewX = t[3],   // m21 (not t[1]!)
                TransX = t[6],  // m31 (not t[2]!)
                SkewY = t[1],   // m12 (not t[3]!)
                ScaleY = t[4],  // m22
                TransY = t[7],  // m32 (not t[5]!)
                Persp0 = t[2],  // m13 (not t[6]!)
                Persp1 = t[5],  // m23 (not t[7]!)
                Persp2 = t[8]   // m33
            };

            _logger.LogInformation($"SKMatrix: ScaleX={matrix.ScaleX}, SkewX={matrix.SkewX}, TransX={matrix.TransX}");
            _logger.LogInformation($"SKMatrix: SkewY={matrix.SkewY}, ScaleY={matrix.ScaleY}, TransY={matrix.TransY}");
            _logger.LogInformation($"SKMatrix: Persp0={matrix.Persp0}, Persp1={matrix.Persp1}, Persp2={matrix.Persp2}");

            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(input, 0, 0);

            var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
        }

        /// <summary>
        /// Mapeia uma imagem diretamente para 4 vértices específicos usando Transform2d
        /// </summary>
        /// <param name="input">Bitmap de entrada</param>
        /// <param name="canvasWidth">Largura do canvas de destino</param>
        /// <param name="canvasHeight">Altura do canvas de destino</param>
        /// <param name="v1x">Vértice 1 (top-left) - coordenada X</param>
        /// <param name="v1y">Vértice 1 (top-left) - coordenada Y</param>
        /// <param name="v2x">Vértice 2 (top-right) - coordenada X</param>
        /// <param name="v2y">Vértice 2 (top-right) - coordenada Y</param>
        /// <param name="v4x">Vértice 4 (bottom-left) - coordenada X</param>
        /// <param name="v4y">Vértice 4 (bottom-left) - coordenada Y</param>
        /// <param name="v3x">Vértice 3 (bottom-right) - coordenada X</param>
        /// <param name="v3y">Vértice 3 (bottom-right) - coordenada Y</param>
        /// <returns>Bitmap transformado no canvas especificado</returns>
        public SKBitmap MapToVertices(SKBitmap input, int canvasWidth, int canvasHeight,
                                      float v1x, float v1y, float v2x, float v2y,
                                      float v4x, float v4y, float v3x, float v3y)
        {
            int w = input.Width;
            int h = input.Height;

            _logger.LogInformation($"MapToVertices: input={w}x{h}, canvas={canvasWidth}x{canvasHeight}");
            _logger.LogInformation($"  V1 (top-left): ({v1x}, {v1y})");
            _logger.LogInformation($"  V2 (top-right): ({v2x}, {v2y})");
            _logger.LogInformation($"  V4 (bottom-left): ({v4x}, {v4y})");
            _logger.LogInformation($"  V3 (bottom-right): ({v3x}, {v3y})");

            var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Calcula matriz de transformação 2D mapeando os 4 cantos da imagem para os 4 vértices
            // Transform2d(w, h, x1_dest, y1_dest, x2_dest, y2_dest, x3_dest, y3_dest, x4_dest, y4_dest)
            // Origem: (0,0), (w,0), (0,h), (w,h)
            // Destino: V1, V2, V4, V3
            float[] t = Transform2d(w, h, v1x, v1y, v2x, v2y, v4x, v4y, v3x, v3y);

            _logger.LogInformation($"Transform2d result: [{t[0]}, {t[1]}, {t[2]}, {t[3]}, {t[4]}, {t[5]}, {t[6]}, {t[7]}, {t[8]}]");

            var matrix = new SKMatrix
            {
                ScaleX = t[0],  // m11
                SkewX = t[3],   // m21
                TransX = t[6],  // m31
                SkewY = t[1],   // m12
                ScaleY = t[4],  // m22
                TransY = t[7],  // m32
                Persp0 = t[2],  // m13
                Persp1 = t[5],  // m23
                Persp2 = t[8]   // m33
            };

            _logger.LogInformation($"SKMatrix: ScaleX={matrix.ScaleX}, SkewX={matrix.SkewX}, TransX={matrix.TransX}");
            _logger.LogInformation($"SKMatrix: SkewY={matrix.SkewY}, ScaleY={matrix.ScaleY}, TransY={matrix.TransY}");
            _logger.LogInformation($"SKMatrix: Persp0={matrix.Persp0}, Persp1={matrix.Persp1}, Persp2={matrix.Persp2}");

            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(input, 0, 0);

            var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 3 - TOPO (2/3 da largura)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (560, 714)
        ///   V2 (top-right):    (1624, 929)
        ///   V3 (bottom-right): (1083, 1006)
        ///   V4 (bottom-left):  (198, 734)
        ///
        /// Características da distorção natural:
        ///   - Quadrilátero irregular com perspectiva complexa
        ///   - Largura superior: 1064px (560→1624)
        ///   - Largura inferior: 885px (198→1083)
        ///   - Altura esquerda: 20px (714→734)
        ///   - Altura direita: 77px (929→1006)
        ///   - Perspectiva suave e natural para bancada de mármore
        /// </summary>
        /// <param name="input">Bitmap de entrada (qualquer retângulo)</param>
        /// <param name="canvasWidth">Largura do canvas de destino</param>
        /// <param name="canvasHeight">Altura do canvas de destino</param>
        /// <returns>Bitmap transformado no quadrilátero com distorção natural</returns>
        public SKBitmap MapToCustomQuadrilateral(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral - TOPO DA BANCADA 3 ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (560, 714)");
            _logger.LogInformation("  V2 (top-right):    (1624, 929)");
            _logger.LogInformation("  V3 (bottom-right): (1083, 1006)");
            _logger.LogInformation("  V4 (bottom-left):  (198, 734)");

            // Usar MapToVertices com coordenadas CORRETAS especificadas pelo usuário
            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 560,  v1y: 714,   // top-left
                v2x: 1624, v2y: 929,   // top-right
                v4x: 198,  v4y: 734,   // bottom-left
                v3x: 1083, v3y: 1006   // bottom-right
            );
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 3 - PÉ/LATERAL (1/3 da largura principal - 95%)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (1083, 1005)
        ///   V2 (top-right):    (1614, 928)
        ///   V3 (bottom-right): (1580, 1528)
        ///   V4 (bottom-left):  (1065, 1715)
        /// </summary>
        /// <param name="input">Bitmap de entrada (1/3 da largura do crop)</param>
        /// <param name="canvasWidth">Largura do canvas de destino (2000)</param>
        /// <param name="canvasHeight">Altura do canvas de destino (1863)</param>
        /// <returns>Bitmap transformado no quadrilátero do pé da bancada</returns>
        public SKBitmap MapToCustomQuadrilateral_Pe(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Pe - PÉ/LATERAL DA BANCADA 3 ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (1083, 1005)");
            _logger.LogInformation("  V2 (top-right):    (1614, 928)");
            _logger.LogInformation("  V3 (bottom-right): (1580, 1528)");
            _logger.LogInformation("  V4 (bottom-left):  (1065, 1715)");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 1083, v1y: 1005,  // top-left
                v2x: 1614, v2y: 928,   // top-right
                v4x: 1065, v4y: 1715,  // bottom-left
                v3x: 1580, v3y: 1528   // bottom-right
            );
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 3 - FAIXA LATERAL SUPERIOR (2/3 da faixa 5%)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (197, 733)
        ///   V2 (top-right):    (1083, 1005)
        ///   V3 (bottom-right): (1058, 1033)
        ///   V4 (bottom-left):  (197, 757)
        /// </summary>
        /// <param name="input">Bitmap de entrada (2/3 da largura da faixa 5%)</param>
        /// <param name="canvasWidth">Largura do canvas de destino (2000)</param>
        /// <param name="canvasHeight">Altura do canvas de destino (1863)</param>
        /// <returns>Bitmap transformado no quadrilátero da faixa lateral superior</returns>
        public SKBitmap MapToCustomQuadrilateral_FaixaSuperior(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_FaixaSuperior - FAIXA LATERAL SUPERIOR ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (197, 733)");
            _logger.LogInformation("  V2 (top-right):    (1083, 1005)");
            _logger.LogInformation("  V3 (bottom-right): (1058, 1033)");
            _logger.LogInformation("  V4 (bottom-left):  (197, 757)");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 197,  v1y: 733,   // top-left
                v2x: 1083, v2y: 1005,  // top-right
                v4x: 197,  v4y: 757,   // bottom-left
                v3x: 1058, v3y: 1033   // bottom-right
            );
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 3 - FAIXA LATERAL INFERIOR (1/3 da faixa 5%)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (1054, 1030)
        ///   V2 (top-right):    (1083, 1004)
        ///   V3 (bottom-right): (1066, 1714)
        ///   V4 (bottom-left):  (1039, 1702)
        /// </summary>
        /// <param name="input">Bitmap de entrada (1/3 da largura da faixa 5%)</param>
        /// <param name="canvasWidth">Largura do canvas de destino (2000)</param>
        /// <param name="canvasHeight">Altura do canvas de destino (1863)</param>
        /// <returns>Bitmap transformado no quadrilátero da faixa lateral inferior</returns>
        public SKBitmap MapToCustomQuadrilateral_FaixaInferior(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_FaixaInferior - FAIXA LATERAL INFERIOR ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (1054, 1030)");
            _logger.LogInformation("  V2 (top-right):    (1083, 1004)");
            _logger.LogInformation("  V3 (bottom-right): (1066, 1714)");
            _logger.LogInformation("  V4 (bottom-left):  (1039, 1702)");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 1054, v1y: 1030,  // top-left
                v2x: 1083, v2y: 1004,  // top-right
                v4x: 1039, v4y: 1702,  // bottom-left
                v3x: 1066, v3y: 1714   // bottom-right
            );
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 6 - TOPO (2/3 da largura principal - 95%)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (700, 887)
        ///   V2 (top-right):    (1569, 1024)
        ///   V3 (bottom-right): (986, 1132)
        ///   V4 (bottom-left):  (339, 907)
        /// </summary>
        /// <param name="input">Bitmap de entrada (2/3 da largura do crop)</param>
        /// <param name="canvasWidth">Largura do canvas de destino (2000)</param>
        /// <param name="canvasHeight">Altura do canvas de destino (1863)</param>
        /// <returns>Bitmap transformado no quadrilátero do topo da bancada 6</returns>
        public SKBitmap MapToCustomQuadrilateral_Bancada6_Topo(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Bancada6_Topo - TOPO DA BANCADA 6 ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (700, 887)");
            _logger.LogInformation("  V2 (top-right):    (1569, 1024)");
            _logger.LogInformation("  V3 (bottom-right): (986, 1132)");
            _logger.LogInformation("  V4 (bottom-left):  (339, 907)");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 700,  v1y: 887,   // top-left
                v2x: 1569, v2y: 1024,  // top-right
                v4x: 339,  v4y: 907,   // bottom-left
                v3x: 986,  v3y: 1132   // bottom-right
            );
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 6 - PÉ/LATERAL (1/3 da largura principal - 95%)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (984, 1130)
        ///   V2 (top-right):    (1569, 1024)
        ///   V3 (bottom-right): (1570, 1531)
        ///   V4 (bottom-left):  (984, 1844)
        /// </summary>
        /// <param name="input">Bitmap de entrada (1/3 da largura do crop)</param>
        /// <param name="canvasWidth">Largura do canvas de destino (2000)</param>
        /// <param name="canvasHeight">Altura do canvas de destino (1863)</param>
        /// <returns>Bitmap transformado no quadrilátero do pé da bancada 6</returns>
        public SKBitmap MapToCustomQuadrilateral_Bancada6_Pe(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Bancada6_Pe - PÉ/LATERAL DA BANCADA 6 ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (984, 1130)");
            _logger.LogInformation("  V2 (top-right):    (1569, 1024)");
            _logger.LogInformation("  V3 (bottom-right): (1570, 1531)");
            _logger.LogInformation("  V4 (bottom-left):  (984, 1844)");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 984,  v1y: 1130,  // top-left
                v2x: 1569, v2y: 1024,  // top-right
                v4x: 984,  v4y: 1844,  // bottom-left
                v3x: 1570, v3y: 1531   // bottom-right
            );
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 6 - FAIXA SUPERIOR (2/3 da faixa lateral - 5%)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (339, 907)
        ///   V2 (top-right):    (985, 1130)
        ///   V3 (bottom-right): (952, 1165)
        ///   V4 (bottom-left):  (338, 931)
        /// </summary>
        /// <param name="input">Bitmap de entrada (2/3 da faixa)</param>
        /// <param name="canvasWidth">Largura do canvas de destino (2500)</param>
        /// <param name="canvasHeight">Altura do canvas de destino (1632)</param>
        /// <returns>Bitmap transformado no quadrilátero da faixa superior da bancada 6</returns>
        public SKBitmap MapToCustomQuadrilateral_Bancada6_FaixaSuperior(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Bancada6_FaixaSuperior - FAIXA SUPERIOR DA BANCADA 6 ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (339, 907)");
            _logger.LogInformation("  V2 (top-right):    (985, 1130)");
            _logger.LogInformation("  V3 (bottom-right): (952, 1165)");
            _logger.LogInformation("  V4 (bottom-left):  (338, 931)");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 339, v1y: 907,   // top-left
                v2x: 985, v2y: 1130,  // top-right
                v4x: 338, v4y: 931,   // bottom-left
                v3x: 952, v3y: 1165   // bottom-right
            );
        }

        /// <summary>
        /// Transformação especializada: mapeia qualquer retângulo para o quadrilátero irregular com distorção natural
        /// BANCADA 6 - FAIXA INFERIOR (1/3 da faixa lateral - 5%)
        /// Imagem entra ROTACIONADA -90° (vertical/em pé)
        /// Vértices fixos especificados pelo usuário:
        ///   V1 (top-left):     (952, 1165)
        ///   V2 (top-right):    (984, 1131)
        ///   V3 (bottom-right): (984, 1829)
        ///   V4 (bottom-left):  (958, 1829)
        /// </summary>
        /// <param name="input">Bitmap de entrada (rotacionado -90°, vertical)</param>
        /// <param name="canvasWidth">Largura do canvas de destino (2500)</param>
        /// <param name="canvasHeight">Altura do canvas de destino (1632)</param>
        /// <returns>Bitmap transformado no quadrilátero da faixa inferior da bancada 6</returns>
        public SKBitmap MapToCustomQuadrilateral_Bancada6_FaixaInferior(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Bancada6_FaixaInferior - FAIXA INFERIOR DA BANCADA 6 ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height} (rotacionado -90°, vertical)");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");
            _logger.LogInformation("Vértices alvo (quadrilátero irregular):");
            _logger.LogInformation("  V1 (top-left):     (952, 1165)");
            _logger.LogInformation("  V2 (top-right):    (984, 1131)");
            _logger.LogInformation("  V3 (bottom-right): (984, 1800)");
            _logger.LogInformation("  V4 (bottom-left):  (958, 1800)");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 952, v1y: 1165,  // top-left
                v2x: 984, v2y: 1131,  // top-right
                v4x: 958, v4y: 1800,  // bottom-left
                v3x: 984, v3y: 1800   // bottom-right
            );
        }

        /// <summary>
        /// Cria matriz de transformação 2D para projeção em perspectiva
        /// </summary>
        private float[] Transform2d(float w, float h,
                                     float x1, float y1,
                                     float x2, float y2,
                                     float x3, float y3,
                                     float x4, float y4)
        {
            float[] t = General2DProjection(
                0, 0, x1, y1,
                w, 0, x2, y2,
                0, h, x3, y3,
                w, h, x4, y4
            );

            // Normaliza pela última componente
            for (int i = 0; i < 9; i++)
            {
                t[i] = t[i] / t[8];
            }

            // Retorna matriz 3x3 em ordem row-major: [m11, m12, m13, m21, m22, m23, m31, m32, m33]
            return new float[] { t[0], t[3], t[6], t[1], t[4], t[7], t[2], t[5], t[8] };
        }

        /// <summary>
        /// Projeção 2D geral usando 4 pares de pontos
        /// </summary>
        private float[] General2DProjection(float x1s, float y1s, float x1d, float y1d,
                                            float x2s, float y2s, float x2d, float y2d,
                                            float x3s, float y3s, float x3d, float y3d,
                                            float x4s, float y4s, float x4d, float y4d)
        {
            float[] s = BasisToPoints(x1s, y1s, x2s, y2s, x3s, y3s, x4s, y4s);
            float[] d = BasisToPoints(x1d, y1d, x2d, y2d, x3d, y3d, x4d, y4d);
            float[] proj = Multmm(d, Adj(s));
            return proj;
        }

        /// <summary>
        /// Converte 4 pontos em matriz de base
        /// </summary>
        private float[] BasisToPoints(float x1, float y1, float x2, float y2,
                                       float x3, float y3, float x4, float y4)
        {
            float[] m = new float[] { x1, x2, x3, y1, y2, y3, 1, 1, 1 };
            float[] v = Multmv(Adj(m), new float[] { x4, y4, 1 });
            float[] mm = Multmm(m, new float[] { v[0], 0, 0, 0, v[1], 0, 0, 0, v[2] });
            return mm;
        }

        /// <summary>
        /// Multiplica duas matrizes 3x3
        /// </summary>
        private float[] Multmm(float[] a, float[] b)
        {
            float[] c = new float[9];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    float cij = 0;
                    for (int k = 0; k < 3; k++)
                    {
                        cij += a[3 * i + k] * b[3 * k + j];
                    }
                    c[3 * i + j] = cij;
                }
            }
            return c;
        }

        /// <summary>
        /// Multiplica matriz 3x3 por vetor
        /// </summary>
        private float[] Multmv(float[] m, float[] v)
        {
            return new float[]
            {
                m[0] * v[0] + m[1] * v[1] + m[2] * v[2],
                m[3] * v[0] + m[4] * v[1] + m[5] * v[2],
                m[6] * v[0] + m[7] * v[1] + m[8] * v[2]
            };
        }

        /// <summary>
        /// Calcula matriz adjunta (adjugate)
        /// </summary>
        private float[] Adj(float[] m)
        {
            return new float[]
            {
                m[4] * m[8] - m[5] * m[7],
                m[2] * m[7] - m[1] * m[8],
                m[1] * m[5] - m[2] * m[4],
                m[5] * m[6] - m[3] * m[8],
                m[0] * m[8] - m[2] * m[6],
                m[2] * m[3] - m[0] * m[5],
                m[3] * m[7] - m[4] * m[6],
                m[1] * m[6] - m[0] * m[7],
                m[0] * m[4] - m[1] * m[3]
            };
        }

        /// <summary>
        /// Aplica distorção com inclinação (usado para perspectiva de bancadas/nichos)
        /// </summary>
        public SKBitmap DistortionInclina(SKBitmap imagem, int ladoMaior, int ladoMenor,
                                          int novaLargura, int novaAltura, int inclinacao)
        {
            // Redimensiona para o tamanho desejado
            var bmpImage = imagem.Resize(new SKImageInfo(novaLargura, novaAltura), SKFilterQuality.High);

            // Aplica skew com os parâmetros calculados
            float fatorDeDistortion = (float)ladoMaior / ladoMenor;
            var bmp2 = Skew(bmpImage, fatorDeDistortion, inclinacao);

            return bmp2;
        }

        /// <summary>
        /// Rotaciona uma imagem com alta qualidade
        /// Replica exatamente o comportamento do VB.NET RotateImage
        /// </summary>
        public SKBitmap RotateImage(SKBitmap img, float angle)
        {
            // VB.NET mantém o tamanho original (não expande)
            int larg = img.Width;
            int altu = img.Height;

            var retBMP = new SKBitmap(larg, altu);
            using (var canvas = new SKCanvas(retBMP))
            {
                canvas.Clear(SKColors.Transparent);

                // Rotaciona em torno do centro
                canvas.Translate(img.Width / 2f, img.Height / 2f);
                canvas.RotateDegrees(angle);
                canvas.Translate(-img.Width / 2f, -img.Height / 2f);

                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };

                // VB.NET: g.DrawImage(img, New PointF(-30, -30))
                // Desenha com offset -30, -30
                canvas.DrawBitmap(img, -30, -30, paint);
            }

            return retBMP;
        }

        /// <summary>
        /// Aplica Skew usando 3 pontos (replica VB.NET Skew)
        /// VB.NET: DrawImage(imagem, destinationPoints)
        /// </summary>
        /// <param name="imagem">Bitmap de entrada</param>
        /// <param name="acrescimo">Pixels adicionados à altura</param>
        /// <param name="fatorSkew">Deslocamento do ponto superior direito</param>
        /// <returns>Bitmap com skew aplicado</returns>
        public SKBitmap SkewSimples(SKBitmap imagem, int acrescimo, int fatorSkew)
        {
            int largura = imagem.Width;
            int altura = imagem.Height + acrescimo;

            // Cria canvas com altura extra para acomodar o skew
            var quadroSkew = new SKBitmap(largura, altura + fatorSkew);

            using (var canvas = new SKCanvas(quadroSkew))
            {
                canvas.Clear(SKColors.Transparent);

                // VB.NET usa 3 pontos de destino:
                // pt1 = (0, 0)              upper-left
                // pt2 = (largura, fatorSkew) upper-right deslocado
                // pt3 = (0, altura)         lower-left

                // Pontos de origem (da imagem original)
                float srcW = imagem.Width;
                float srcH = imagem.Height;

                // Pontos de destino (onde queremos mapear)
                float[] t = Transform2d(
                    srcW, srcH,
                    0, 0,                    // pt1: (0, 0)
                    largura, fatorSkew,      // pt2: (largura, fatorSkew)
                    0, altura,               // pt3: (0, altura)
                    srcW, altura             // pt4: mantém original para não quebrar Bancadas #1 e #2
                );

                var matrix = new SKMatrix
                {
                    ScaleX = t[0],
                    SkewX = t[3],
                    TransX = t[6],
                    SkewY = t[1],
                    ScaleY = t[4],
                    TransY = t[7],
                    Persp0 = t[2],
                    Persp1 = t[5],
                    Persp2 = t[8]
                };

                canvas.SetMatrix(matrix);

                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };

                canvas.DrawBitmap(imagem, 0, 0, paint);
            }

            return quadroSkew;
        }

        /// <summary>
        /// Aplica Skew com 4º ponto calculado corretamente (paralelogramo)
        /// Específico para LATERAL da Bancada #4 - não afeta Bancadas #1 e #2
        /// </summary>
        public SKBitmap SkewLateral(SKBitmap imagem, int acrescimo, int fatorSkew)
        {
            int largura = imagem.Width;
            int altura = imagem.Height + acrescimo;

            // Cria canvas com altura extra para acomodar o skew
            var quadroSkew = new SKBitmap(largura, altura + fatorSkew);

            using (var canvas = new SKCanvas(quadroSkew))
            {
                canvas.Clear(SKColors.Transparent);

                // VB.NET GDI+ com 3 pontos calcula 4º ponto automaticamente como paralelogramo:
                // pt4 = pt2 + (pt3 - pt1) = (largura, fatorSkew) + (0, altura) - (0, 0) = (largura, altura + fatorSkew)
                float srcW = imagem.Width;
                float srcH = imagem.Height;

                float[] t = Transform2d(
                    srcW, srcH,
                    0, 0,                         // pt1: (0, 0) - topo esquerdo
                    largura, fatorSkew,           // pt2: (largura, fatorSkew) - topo direito desce fatorSkew
                    0, altura,                    // pt3: (0, altura) - base esquerda
                    largura, altura + fatorSkew   // pt4: (largura, altura + fatorSkew) - base direita desce fatorSkew
                );

                var matrix = new SKMatrix
                {
                    ScaleX = t[0],
                    SkewX = t[3],
                    TransX = t[6],
                    SkewY = t[1],
                    ScaleY = t[4],
                    TransY = t[7],
                    Persp0 = t[2],
                    Persp1 = t[5],
                    Persp2 = t[8]
                };

                canvas.SetMatrix(matrix);

                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };

                canvas.DrawBitmap(imagem, 0, 0, paint);
            }

            return quadroSkew;
        }

        /// <summary>
        /// Aplica transformação skew invertida (inclina da direita para esquerda)
        /// Equivalente ao Skew2() do VB.NET - usado na parte FRENTE da Bancada #4
        /// </summary>
        public SKBitmap Skew2(SKBitmap imagem, int acrescimo, int fatorSkew)
        {
            int largura = imagem.Width;
            int altura = imagem.Height + acrescimo;

            // Cria canvas com altura extra para acomodar o skew
            var quadroSkew = new SKBitmap(largura, altura + fatorSkew);

            using (var canvas = new SKCanvas(quadroSkew))
            {
                canvas.Clear(SKColors.Transparent);

                // VB.NET Skew2 usa 3 pontos de destino (inclinação INVERTIDA):
                // pt1 = (0, fatorSkew)          upper-left DESLOCADO PARA BAIXO
                // pt2 = (largura, 0)            upper-right NO TOPO
                // pt3 = (0, altura + fatorSkew) lower-left

                // Pontos de origem (da imagem original)
                float srcW = imagem.Width;
                float srcH = imagem.Height;

                // Pontos de destino (onde queremos mapear)
                float[] t = Transform2d(
                    srcW, srcH,
                    0, fatorSkew,            // pt1: (0, fatorSkew) - topo esquerdo DESCE
                    largura, 0,              // pt2: (largura, 0) - topo direito no topo
                    0, altura + fatorSkew,   // pt3: (0, altura + fatorSkew) - base esquerda desce mais
                    largura, altura          // pt4: (largura, altura) - base direita mantém
                );

                var matrix = new SKMatrix
                {
                    ScaleX = t[0],
                    SkewX = t[3],
                    TransX = t[6],
                    SkewY = t[1],
                    ScaleY = t[4],
                    TransY = t[7],
                    Persp0 = t[2],
                    Persp1 = t[5],
                    Persp2 = t[8]
                };

                canvas.SetMatrix(matrix);

                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };

                canvas.DrawBitmap(imagem, 0, 0, paint);
            }

            return quadroSkew;
        }

        /// <summary>
        /// Ajusta HSL (Hue, Saturation, Lightness) de uma imagem
        /// Usado para pós-produção (ajuste de brilho, contraste, sombras)
        /// </summary>
        public SKBitmap AjustarHSL(SKBitmap imagem, int varBrilho, int varContraste, int varSombras, int varSaturacao)
        {
            var result = new SKBitmap(imagem.Width, imagem.Height, imagem.ColorType, imagem.AlphaType);

            double sat = 127 * varSaturacao * 3.0 / 100;
            double lum = 127 * varBrilho / 100;

            for (int y = 0; y < imagem.Height; y++)
            {
                for (int x = 0; x < imagem.Width; x++)
                {
                    var pixel = imagem.GetPixel(x, y);

                    // Converte RGB para HSL
                    double r = pixel.Red;
                    double g = pixel.Green;
                    double b = pixel.Blue;

                    double min = Math.Min(r, Math.Min(g, b));
                    double max = Math.Max(r, Math.Max(g, b));
                    double dif = max - min;
                    double sum = max + min;

                    double l = 0.5 * sum;
                    double h = 0.0;
                    double s = 0.0;

                    if (dif != 0)
                    {
                        if (l < 127.5)
                            s = 255.0 * dif / sum;
                        else
                            s = 255.0 * dif / (510.0 - sum);

                        double f1, f2;
                        if (max == r)
                        {
                            f1 = 0.0;
                            f2 = g - b;
                        }
                        else if (max == g)
                        {
                            f1 = 120.0;
                            f2 = b - r;
                        }
                        else
                        {
                            f1 = 240.0;
                            f2 = r - g;
                        }

                        h = f1 + 60.0 * f2 / dif;
                        if (h < 0.0) h += 360.0;
                        if (h >= 360.0) h -= 360.0;
                    }

                    // Aplica transformações
                    s = Math.Max(0, Math.Min(255, s + sat));
                    l = Math.Max(0, Math.Min(255, l + lum));

                    // Converte de volta para RGB
                    if (s == 0)
                    {
                        r = g = b = l;
                    }
                    else
                    {
                        double v2;
                        if (l < 127.5)
                            v2 = l / 255.0 * (255 + s);
                        else
                            v2 = l + s - s * l / 255.0;

                        double v1 = 2 * l - v2;
                        double v3 = v2 - v1;

                        r = GetRGBComponent(v1, v3, h + 120.0);
                        g = GetRGBComponent(v1, v3, h);
                        b = GetRGBComponent(v1, v3, h - 120.0);
                    }

                    byte newR = (byte)Math.Max(0, Math.Min(255, r));
                    byte newG = (byte)Math.Max(0, Math.Min(255, g));
                    byte newB = (byte)Math.Max(0, Math.Min(255, b));

                    result.SetPixel(x, y, new SKColor(newR, newG, newB, pixel.Alpha));
                }
            }

            return result;
        }

        private double GetRGBComponent(double v1, double v3, double h)
        {
            if (h >= 360.0) h -= 360.0;
            if (h < 0.0) h += 360.0;

            if (h < 60.0)
                return v1 + v3 * h / 60.0;
            else if (h < 180.0)
                return v1 + v3;
            else if (h < 240.0)
                return v1 + v3 * (4 - h / 60.0);
            else
                return v1;
        }

        /// <summary>
        /// Aplica distorção de perspectiva sem inclinação (VB.NET: Distortion sem inclinação)
        /// Redimensiona e aplica compressão vertical não-linear para efeito de perspectiva
        /// </summary>
        /// <param name="imagem">Bitmap de entrada</param>
        /// <param name="ladoMaior">Lado maior para cálculo do fator de distorção</param>
        /// <param name="ladoMenor">Lado menor para cálculo do fator de distorção</param>
        /// <param name="novaLargura">Largura final desejada</param>
        /// <param name="novaAltura">Altura final desejada</param>
        /// <returns>Bitmap com distorção aplicada</returns>
        public SKBitmap Distortion(SKBitmap imagem, int ladoMaior, int ladoMenor, int novaLargura, int novaAltura)
        {
            _logger.LogInformation($"Distortion ENTRADA: imagem={imagem?.Width ?? 0}x{imagem?.Height ?? 0}, ladoMaior={ladoMaior}, ladoMenor={ladoMenor}, novaLargura={novaLargura}, novaAltura={novaAltura}");

            // VALIDAÇÃO: Parâmetros
            if (imagem == null || imagem.Width <= 0 || imagem.Height <= 0)
            {
                _logger.LogError($"Distortion: Imagem de entrada inválida!");
                throw new ArgumentException($"Imagem de entrada inválida para Distortion");
            }

            if (ladoMaior <= 0 || ladoMenor <= 0 || novaLargura <= 0 || novaAltura <= 0)
            {
                _logger.LogError($"Distortion: Parâmetros inválidos!");
                throw new ArgumentException($"Parâmetros inválidos para Distortion");
            }

            // CORREÇÃO: VB.NET faz apenas RESIZE UNIFORME (sem amostragem não-linear)
            // Isso cria PARALELOGRAMO (sem convergência) em vez de TRAPÉZIO

            // Calcula altura final usando o fator
            float fator = (float)ladoMaior / ladoMenor;
            int alturaFinal = (int)(novaAltura / fator);

            _logger.LogInformation($"Distortion: fator={fator:F3}, alturaFinal={alturaFinal} (de {novaAltura})");

            // APENAS RESIZE uniforme (mantém linhas paralelas = PARALELOGRAMO)
            SKBitmap resultado;
            try
            {
                resultado = imagem.Resize(new SKImageInfo(novaLargura, alturaFinal), SKFilterQuality.High);
                if (resultado == null || resultado.Width <= 0 || resultado.Height <= 0)
                {
                    _logger.LogError($"Distortion: Resize retornou bitmap inválido!");
                    throw new InvalidOperationException("Resize falhou");
                }
                _logger.LogInformation($"Distortion SAÍDA: {resultado.Width}x{resultado.Height}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Distortion: Erro no resize: {ex.Message}");
                throw;
            }

            return resultado;
        }

        /// <summary>
        /// Rotaciona imagem com offset customizado (VB.NET: RotateImage2)
        /// Rotaciona em torno do centro mas desenha a imagem com offset especificado
        /// </summary>
        /// <param name="img">Bitmap de entrada</param>
        /// <param name="angle">Ângulo de rotação em graus</param>
        /// <param name="offsetX">Offset horizontal para desenho</param>
        /// <param name="offsetY">Offset vertical para desenho</param>
        /// <returns>Bitmap rotacionado</returns>
        public SKBitmap RotateImage2(SKBitmap img, float angle, int offsetX, int offsetY)
        {
            int larg = img.Width;
            int altu = img.Height;

            var retBMP = new SKBitmap(larg, altu);
            using (var canvas = new SKCanvas(retBMP))
            {
                canvas.Clear(SKColors.Transparent);

                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };

                // VB.NET: g.DrawImage(img, New PointF(offsetX, offsetY))
                // Offset integrado NA transformação de rotação

                // 1. Move para ponto de desenho + centro da imagem
                canvas.Translate(offsetX + img.Width / 2f, offsetY + img.Height / 2f);

                // 2. Rotaciona em torno deste ponto
                canvas.RotateDegrees(angle);

                // 3. Move de volta para desenhar centrado
                canvas.Translate(-img.Width / 2f, -img.Height / 2f);

                // 4. Desenha na origem do sistema transformado
                canvas.DrawBitmap(img, 0, 0, paint);
            }

            return retBMP;
        }

        /// <summary>
        /// Transformação para Bancada 5 - Frente Direita
        /// Canvas: 1500x1068
        /// Vértices fixos:
        ///   V1 (top-left):     (670, 598)
        ///   V2 (top-right):    (968, 577)
        ///   V3 (bottom-right): (975, 854)
        ///   V4 (bottom-left):  (309, 1037)
        /// </summary>
        public SKBitmap MapToCustomQuadrilateral_Bancada5_FrenteDireita(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Bancada5_FrenteDireita ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 670,  v1y: 598,    // top-left
                v2x: 968,  v2y: 577,    // top-right
                v4x: 309,  v4y: 1037,   // bottom-left
                v3x: 975,  v3y: 854     // bottom-right
            );
        }

        /// <summary>
        /// Transformação para Bancada 5 - Frente Esquerda
        /// Canvas: 1500x1068
        /// Vértices fixos:
        ///   V1 (top-left):     (309, 623)
        ///   V2 (top-right):    (670, 598)
        ///   V3 (bottom-right): (669, 939)
        ///   V4 (bottom-left):  (309, 1036)
        /// </summary>
        public SKBitmap MapToCustomQuadrilateral_Bancada5_FrenteEsquerda(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Bancada5_FrenteEsquerda ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 309,  v1y: 623,    // top-left
                v2x: 670,  v2y: 598,    // top-right
                v4x: 309,  v4y: 1036,   // bottom-left
                v3x: 669,  v3y: 939     // bottom-right
            );
        }

        /// <summary>
        /// Transformação para Bancada 5 - Lateral
        /// Canvas: 1500x1068
        /// Vértices fixos:
        ///   V1 (top-left):     (188, 601)
        ///   V2 (top-right):    (309, 623)
        ///   V3 (bottom-right): (309, 1036)
        ///   V4 (bottom-left):  (196, 922)
        /// </summary>
        public SKBitmap MapToCustomQuadrilateral_Bancada5_Lateral(SKBitmap input, int canvasWidth, int canvasHeight)
        {
            _logger.LogInformation("=== MapToCustomQuadrilateral_Bancada5_Lateral ===");
            _logger.LogInformation($"Input: {input.Width}x{input.Height}");
            _logger.LogInformation($"Canvas: {canvasWidth}x{canvasHeight}");

            return MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: 188,  v1y: 601,    // top-left
                v2x: 309,  v2y: 623,    // top-right
                v4x: 196,  v4y: 922,    // bottom-left
                v3x: 309,  v3y: 1036    // bottom-right
            );
        }
    }
}
