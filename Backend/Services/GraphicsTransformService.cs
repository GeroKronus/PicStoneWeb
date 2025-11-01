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

            var surface = SKSurface.Create(new SKImageInfo(w, h));
            var canvas = surface.Canvas;

            // Calcula matriz de transformação 2D para perspectiva
            float[] t = Transform2d(w, h, 0, distanciaTopo, w, 0, 0, h2 + distanciaTopo, w, h);

            // Usa SKMatrix para transformação 2D (mais simples que SKMatrix44)
            var matrix = new SKMatrix(
                t[0], t[1], t[2],
                t[3], t[4], t[5],
                t[6], t[7], t[8]
            );

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

            return new float[] { t[0], t[3], 0, t[6], t[1], t[4], 0, t[7], 0, 0, 1, 0, t[2], t[5], 0, t[8] };
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
        /// </summary>
        public SKBitmap RotateImage(SKBitmap img, float angle)
        {
            var radians = (float)(angle * Math.PI / 180);

            // Calcula novo tamanho após rotação
            double cos = Math.Abs(Math.Cos(radians));
            double sin = Math.Abs(Math.Sin(radians));
            int newWidth = (int)(img.Width * cos + img.Height * sin);
            int newHeight = (int)(img.Width * sin + img.Height * cos);

            var surface = SKSurface.Create(new SKImageInfo(newWidth, newHeight));
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.Transparent);
            canvas.Translate(newWidth / 2f, newHeight / 2f);
            canvas.RotateDegrees(angle);
            canvas.Translate(-img.Width / 2f, -img.Height / 2f);

            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.DrawBitmap(img, 0, 0, paint);

            var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
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
    }
}
