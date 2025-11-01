using SkiaSharp;
using System;
using System.IO;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço para geração de mockups tipo Nicho
    /// Baseado no Sub Nicho1() do PicStone PageMaker VB.NET
    /// </summary>
    public class NichoService
    {
        private readonly ILogger<NichoService> _logger;
        private readonly GraphicsTransformService _transformService;
        private readonly string _resourcesPath;

        public NichoService(ILogger<NichoService> logger, GraphicsTransformService transformService)
        {
            _logger = logger;
            _transformService = transformService;
            _resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Nichos");
        }

        /// <summary>
        /// Gera mockup de Nicho tipo 1
        /// </summary>
        /// <param name="imagemOriginal">Imagem da chapa selecionada pelo usuário</param>
        /// <param name="fundoEscuro">True para fundo escuro (DarkGray), False para fundo claro (White)</param>
        /// <param name="incluirShampoo">Incluir objeto decorativo shampoo</param>
        /// <param name="incluirSabonete">Incluir objeto decorativo sabonete</param>
        /// <returns>Lista com 2 mockups (normal e rotacionado 180°)</returns>
        public List<SKBitmap> GerarNicho1(SKBitmap imagemOriginal, bool fundoEscuro = false,
                                          bool incluirShampoo = false, bool incluirSabonete = false)
        {
            var resultado = new List<SKBitmap>();

            // Processa 2 versões: normal e rotacionada 180°
            for (int contaProcesso = 1; contaProcesso <= 2; contaProcesso++)
            {
                SKBitmap imagemBookMatch;

                if (contaProcesso == 1)
                {
                    imagemBookMatch = imagemOriginal.Copy();
                }
                else
                {
                    // Rotaciona 180° para segunda versão
                    imagemBookMatch = RotateFlip180(imagemOriginal);
                }

                // Define retângulos para recorte das seções do nicho
                // Baseado nas proporções do código VB.NET
                var retangulos = new SKRectI[]
                {
                    // Fundo do nicho (85.71% largura x 80% altura)
                    new SKRectI(
                        (int)(imagemBookMatch.Width * 0.0714),
                        (int)(imagemBookMatch.Height * 0.1),
                        (int)(imagemBookMatch.Width * 0.9285),
                        (int)(imagemBookMatch.Height * 0.9)
                    ),
                    // Patamar inferior (85.71% largura x 10% altura)
                    new SKRectI(
                        (int)(imagemBookMatch.Width * 0.0714),
                        (int)(imagemBookMatch.Height * 0.8),
                        (int)(imagemBookMatch.Width * 0.9285),
                        (int)(imagemBookMatch.Height * 0.9)
                    ),
                    // Lateral (7.14% largura x 80% altura)
                    new SKRectI(
                        0,
                        0,
                        (int)(imagemBookMatch.Width * 0.0714),
                        (int)(imagemBookMatch.Height * 0.8)
                    )
                };

                // Recorta as 3 seções
                var recortes = new SKBitmap[3];
                for (int i = 0; i < 3; i++)
                {
                    recortes[i] = CropBitmap(imagemBookMatch, retangulos[i]);
                }

                // Redimensiona e prepara as peças do nicho
                var baseNicho = imagemBookMatch.Resize(new SKImageInfo(700, 500), SKFilterQuality.High);
                var fundoNicho = recortes[0].Resize(new SKImageInfo(550, 400), SKFilterQuality.High);
                var patamarInf = recortes[1].Resize(new SKImageInfo(600, 50), SKFilterQuality.High);
                var patamarSup = recortes[1].Resize(new SKImageInfo(600, 25), SKFilterQuality.High);
                var lateralDir = recortes[2].Resize(new SKImageInfo(25, 400), SKFilterQuality.High);
                var lateralEsq = recortes[2].Resize(new SKImageInfo(25, 400), SKFilterQuality.High);

                // Aplica transformações de perspectiva
                patamarInf = _transformService.RotateImage(patamarInf, 270);
                patamarInf = _transformService.DistortionInclina(patamarInf, 600, 550, 50, 600, 25);
                patamarInf = _transformService.RotateImage(patamarInf, 90);

                patamarSup = _transformService.RotateImage(patamarSup, 270);
                patamarSup = _transformService.DistortionInclina(patamarSup, 600, 550, 25, 600, 25);
                patamarSup = _transformService.RotateImage(patamarSup, 270);

                lateralEsq = _transformService.RotateImage(lateralEsq, 180);

                // Aplica ajustes de cor (pós-produção) para criar efeito 3D
                patamarSup = _transformService.AjustarHSL(patamarSup, -2, -5, 1, 0);
                lateralDir = _transformService.AjustarHSL(lateralDir, -2, -9, 2, 0);
                lateralEsq = _transformService.AjustarHSL(lateralEsq, 2, 1, 0, 0);
                patamarInf = _transformService.AjustarHSL(patamarInf, 1, 1, 0, 0);
                fundoNicho = _transformService.AjustarHSL(fundoNicho, -1, -1, 0, 0);

                // Monta o nicho completo na base
                var surface = SKSurface.Create(new SKImageInfo(700, 500));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                };

                // Desenha as peças em ordem
                canvas.DrawBitmap(fundoNicho, 75, 50, paint);
                canvas.DrawBitmap(lateralEsq, 50, 50, paint);
                canvas.DrawBitmap(lateralDir, 625, 50, paint);
                canvas.DrawBitmap(patamarInf, 50, 400, paint);
                canvas.DrawBitmap(patamarSup, 50, 50, paint);

                // Adiciona sombras
                var sombraNicho = CarregarRecurso("SombraNicho1.png");
                if (sombraNicho != null)
                {
                    canvas.DrawBitmap(sombraNicho, 0, 0, paint);
                }

                // Adiciona objetos decorativos opcionais
                if (incluirShampoo)
                {
                    var shampoo = CarregarRecurso("ShampooNicho1.png");
                    if (shampoo != null)
                    {
                        canvas.DrawBitmap(shampoo, 0, 0, paint);
                    }
                }

                if (incluirSabonete)
                {
                    var sabonete = CarregarRecurso("SaboneteNicho1.png");
                    if (sabonete != null)
                    {
                        canvas.DrawBitmap(sabonete, 0, 0, paint);
                    }
                }

                var nichoMontado = surface.Snapshot();

                // Cria canvas final 1000x1000 com fundo e sombra de borda
                var surfaceFinal = SKSurface.Create(new SKImageInfo(1000, 1000));
                var canvasFinal = surfaceFinal.Canvas;

                // Define cor de fundo
                if (fundoEscuro)
                    canvasFinal.Clear(SKColors.DarkGray);
                else
                    canvasFinal.Clear(SKColors.White);

                // Adiciona sombra da borda
                var sombraBorda = CarregarRecurso("SombraBordaNicho1.png");
                if (sombraBorda != null)
                {
                    canvasFinal.DrawBitmap(sombraBorda, 140, 240, paint);
                }

                // Desenha o nicho montado
                using var nichoData = nichoMontado.Encode(SKEncodedImageFormat.Png, 100);
                using var nichoStream = new MemoryStream(nichoData.ToArray());
                using var nichoBitmap = SKBitmap.Decode(nichoStream);
                canvasFinal.DrawBitmap(nichoBitmap, 150, 250, paint);

                var imagemFinal = surfaceFinal.Snapshot();
                using var finalData = imagemFinal.Encode(SKEncodedImageFormat.Png, 100);
                using var finalStream = new MemoryStream(finalData.ToArray());
                var resultadoBitmap = SKBitmap.Decode(finalStream);

                resultado.Add(resultadoBitmap);

                // Limpa recursos temporários
                imagemBookMatch.Dispose();
                foreach (var recorte in recortes)
                {
                    recorte?.Dispose();
                }
                fundoNicho.Dispose();
                patamarInf.Dispose();
                patamarSup.Dispose();
                lateralDir.Dispose();
                lateralEsq.Dispose();
                surface.Dispose();
                surfaceFinal.Dispose();
            }

            return resultado;
        }

        /// <summary>
        /// Recorta uma região específica de um bitmap
        /// </summary>
        private SKBitmap CropBitmap(SKBitmap source, SKRectI cropRect)
        {
            var cropped = new SKBitmap(cropRect.Width, cropRect.Height);
            using var canvas = new SKCanvas(cropped);
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High
            };

            canvas.DrawBitmap(source, cropRect, new SKRect(0, 0, cropRect.Width, cropRect.Height), paint);
            return cropped;
        }

        /// <summary>
        /// Rotaciona bitmap 180 graus
        /// </summary>
        private SKBitmap RotateFlip180(SKBitmap source)
        {
            var surface = SKSurface.Create(new SKImageInfo(source.Width, source.Height));
            var canvas = surface.Canvas;

            canvas.Translate(source.Width / 2f, source.Height / 2f);
            canvas.RotateDegrees(180);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);

            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High
            };

            canvas.DrawBitmap(source, 0, 0, paint);

            var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
        }

        /// <summary>
        /// Carrega recurso PNG da pasta MockupResources
        /// </summary>
        private SKBitmap? CarregarRecurso(string nomeArquivo)
        {
            try
            {
                string caminhoCompleto = Path.Combine(_resourcesPath, nomeArquivo);

                if (!File.Exists(caminhoCompleto))
                {
                    _logger.LogWarning($"Arquivo de recurso não encontrado: {caminhoCompleto}");
                    return null;
                }

                using var stream = File.OpenRead(caminhoCompleto);
                return SKBitmap.Decode(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar recurso: {nomeArquivo}");
                return null;
            }
        }
    }
}
