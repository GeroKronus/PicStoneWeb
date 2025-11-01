using SkiaSharp;
using System;
using System.IO;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço para geração de mockups tipo Bancada (Countertop)
    /// Baseado no Sub Bancada1() do PicStone PageMaker VB.NET
    /// </summary>
    public class BancadaService
    {
        private readonly ILogger<BancadaService> _logger;
        private readonly GraphicsTransformService _transformService;
        private readonly string _resourcesPath;

        public BancadaService(ILogger<BancadaService> logger, GraphicsTransformService transformService)
        {
            _logger = logger;
            _transformService = transformService;
            _resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Bancadas");
        }

        /// <summary>
        /// Gera mockup de Bancada tipo 1 (Countertop #1)
        /// </summary>
        /// <param name="imagemOriginal">Imagem da chapa selecionada pelo usuário</param>
        /// <param name="flip">True para flipar horizontalmente</param>
        /// <returns>Lista com 2 mockups (normal e rotacionado 180°)</returns>
        public List<SKBitmap> GerarBancada1(SKBitmap imagemOriginal, bool flip = false)
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

                _logger.LogInformation($"Bancada1 - Processo {contaProcesso}: Imagem {imagemBookMatch.Width}x{imagemBookMatch.Height}");

                // Divide em 2/3 (topo) e 1/3 (lateral)
                int doisTercos = (int)(imagemBookMatch.Width / 1.5); // 2/3 da largura
                int umTerco = imagemBookMatch.Width - doisTercos;

                var rectDoisTercos = new SKRectI(0, 0, doisTercos, imagemBookMatch.Height);
                var rectUmTerco = new SKRectI(doisTercos, 0, imagemBookMatch.Width, imagemBookMatch.Height);

                var imagemDoisTercos = CropBitmap(imagemBookMatch, rectDoisTercos);
                var imagemUmTerco = CropBitmap(imagemBookMatch, rectUmTerco);

                _logger.LogInformation($"DoisTercos: {imagemDoisTercos.Width}x{imagemDoisTercos.Height}");
                _logger.LogInformation($"UmTerco: {imagemUmTerco.Width}x{imagemUmTerco.Height}");

                // Redimensiona a parte lateral (1/3)
                var bitmapORI2 = imagemUmTerco.Resize(new SKImageInfo(460, 1150), SKFilterQuality.High);

                // Aplica distorção no topo (2/3)
                var bmp2 = _transformService.DistortionInclina(imagemDoisTercos, 1180, 450, 450, 1180, 700);

                // Cria canvas para parte da bancada
                var parteBancada = new SKBitmap(1400, 1400);
                using (var canvas = new SKCanvas(parteBancada))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(bmp2, 50, 50);
                }
                parteBancada = _transformService.RotateImage(parteBancada, 83.3f);

                // Cria canvas para parte do pé
                var partePe = new SKBitmap(1400, 1400);
                using (var canvas = new SKCanvas(partePe))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(bitmapORI2, 100, 100);
                }
                partePe = _transformService.Skew(partePe, 0, 200);
                partePe = _transformService.RotateImage(partePe, 83.25f);

                // Monta o mosaico 1191x1051
                int larguraMolduraVirtual = 1191;
                int alturaMolduraVirtual = 1051;

                var mosaicoEmBranco = new SKBitmap(larguraMolduraVirtual, alturaMolduraVirtual);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);

                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

                    // Desenha parte bancada e parte pé
                    canvas.DrawBitmap(parteBancada, -161, 474, paint);
                    canvas.DrawBitmap(partePe, -161, 777, paint);

                    // Adiciona moldura bancada1.png
                    var moldura = CarregarRecurso("bancada1.png");
                    if (moldura != null)
                    {
                        canvas.DrawBitmap(moldura, 0, 0, paint);
                        _logger.LogInformation("Moldura bancada1.png aplicada");
                    }
                    else
                    {
                        _logger.LogWarning("Moldura bancada1.png NÃO foi carregada!");
                    }
                }

                // Flip horizontal se solicitado
                if (flip)
                {
                    mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                }

                resultado.Add(mosaicoEmBranco);

                // Limpa recursos temporários
                imagemBookMatch.Dispose();
                imagemDoisTercos.Dispose();
                imagemUmTerco.Dispose();
                bitmapORI2.Dispose();
                bmp2.Dispose();
                parteBancada.Dispose();
                partePe.Dispose();
            }

            return resultado;
        }

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

        private SKBitmap FlipHorizontal(SKBitmap source)
        {
            var surface = SKSurface.Create(new SKImageInfo(source.Width, source.Height));
            var canvas = surface.Canvas;

            canvas.Scale(-1, 1, source.Width / 2f, 0);

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
