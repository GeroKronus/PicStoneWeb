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
            // Redimensiona para o tamanho desejado
            var bmpImage = imagem.Resize(new SKImageInfo(novaLargura, novaAltura), SKFilterQuality.High);

            var bmp2 = new SKBitmap(bmpImage.Width, bmpImage.Height);
            int largura = bmpImage.Width;
            int altura = bmpImage.Height;

            // Cálculo do fator de distorção (perspectiva)
            decimal fatorDeDistortion = (decimal)ladoMaior / ladoMenor;
            decimal primeiroY = altura / fatorDeDistortion;
            decimal primeiroDiv = altura / primeiroY;

            int loopEixoY = (int)primeiroY;
            decimal pixelVertical = altura / primeiroY;
            decimal posicaoDosPixels = 0;

            for (int horizontal = 0; horizontal < largura; horizontal++)
            {
                posicaoDosPixels = 0;

                for (int vertical = 0; vertical < loopEixoY; vertical++)
                {
                    int pixelY = (int)posicaoDosPixels;
                    if (pixelY >= altura) pixelY = altura - 1;
                    if (pixelY < 0) pixelY = 0;

                    var pixel = bmpImage.GetPixel(horizontal, pixelY);
                    bmp2.SetPixel(horizontal, vertical, pixel);

                    posicaoDosPixels += pixelVertical;
                    if (posicaoDosPixels >= altura) posicaoDosPixels = altura - 1;
                }
            }

            bmpImage.Dispose();
            return bmp2;
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

                // Rotaciona em torno do centro da imagem
                canvas.Translate(img.Width / 2f, img.Height / 2f);
                canvas.RotateDegrees(angle);
                canvas.Translate(-img.Width / 2f, -img.Height / 2f);

                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };

                // VB.NET: g.DrawImage(img, New PointF(Ponto1, Ponto2))
                canvas.DrawBitmap(img, offsetX, offsetY, paint);
            }

            return retBMP;
        }
    }
}
