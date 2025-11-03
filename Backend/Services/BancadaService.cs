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
            _logger.LogWarning($"========== INICIANDO GerarBancada1 - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
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

                // DEBUG: Salvar partes cropadas
                SalvarDebug(imagemDoisTercos, $"DEBUG_Bancada1_P{contaProcesso}_01_DoisTercos.png");
                SalvarDebug(imagemUmTerco, $"DEBUG_Bancada1_P{contaProcesso}_02_UmTerco.png");

                // Redimensiona a parte lateral (1/3)
                var bitmapORI2 = imagemUmTerco.Resize(new SKImageInfo(460, 1150), SKFilterQuality.High);
                SalvarDebug(bitmapORI2, $"DEBUG_Bancada1_P{contaProcesso}_03_LateralResized.png");

                // Aplica distorção no topo (2/3)
                // Parâmetros VB.NET: DistortionInclina(ImagemDoisTercos, 1180, 450, 450, 1180, 700)
                // Parâmetros: imagem, ladoMaior, ladoMenor, novaLargura, novaAltura, inclinacao
                var bmp2 = _transformService.DistortionInclina(imagemDoisTercos, 1180, 450, 450, 1180, 700);
                SalvarDebug(bmp2, $"DEBUG_Bancada1_P{contaProcesso}_04_TopoDistorcido.png");

                // Cria canvas para parte da bancada
                var parteBancada = new SKBitmap(1400, 1400);
                using (var canvas = new SKCanvas(parteBancada))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(bmp2, 50, 50);
                }
                SalvarDebug(parteBancada, $"DEBUG_Bancada1_P{contaProcesso}_05_ParteBancadaAntes.png");
                parteBancada = _transformService.RotateImage(parteBancada, 83.3f);
                SalvarDebug(parteBancada, $"DEBUG_Bancada1_P{contaProcesso}_06_ParteBancadaRotacionada.png");

                // Cria canvas para parte do pé
                var partePe = new SKBitmap(1400, 1400);
                using (var canvas = new SKCanvas(partePe))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(bitmapORI2, 100, 100);
                }
                SalvarDebug(partePe, $"DEBUG_Bancada1_P{contaProcesso}_07_PartePeAntes.png");
                // VB.NET: partePe = Skew(partePe, 0, 200)
                partePe = _transformService.SkewSimples(partePe, 0, 200);
                SalvarDebug(partePe, $"DEBUG_Bancada1_P{contaProcesso}_08_PartePeSkew.png");
                partePe = _transformService.RotateImage(partePe, 83.25f);
                SalvarDebug(partePe, $"DEBUG_Bancada1_P{contaProcesso}_09_PartePeRotacionada.png");

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
                    // VB.NET original: (-161, 474) e (-161, 777)
                    // Ajuste parteBancada: Y de 474 → 500 → 507 → 497 → 487 → 467 → 472 (-2px total)
                    // Ajuste parteBancada: X de -161 → -141 → -121 → -131 (+30px direita total)
                    // Ajuste partePe: Y de 777 → 756 → 763 (-14px)
                    // Ajuste partePe: X de -161 → -168 → -158 → -151 (+17px direita total)
                    canvas.DrawBitmap(parteBancada, -131, 472, paint);
                    canvas.DrawBitmap(partePe, -151, 763, paint);
                }

                SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada1_P{contaProcesso}_10_MosaicoAntesMoldura.png");

                // Adiciona moldura bancada1.png
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

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

                SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada1_P{contaProcesso}_11_MosaicoComMoldura.png");

                // Flip horizontal se solicitado
                if (flip)
                {
                    mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                    SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada1_P{contaProcesso}_12_Final_Flipped.png");
                }
                else
                {
                    SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada1_P{contaProcesso}_12_Final.png");
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

        /// <summary>
        /// Gera mockup de Bancada tipo 2 (Countertop #2)
        /// Design horizontal expansivo com 3 elementos compostos
        /// </summary>
        /// <param name="imagemOriginal">Imagem da chapa selecionada pelo usuário</param>
        /// <param name="flip">True para flipar horizontalmente</param>
        /// <returns>Lista com 2 mockups (normal e rotacionado 180°)</returns>
        public List<SKBitmap> GerarBancada2(SKBitmap imagemOriginal, bool flip = false)
        {
            _logger.LogWarning($"========== INICIANDO GerarBancada2 - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
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

                _logger.LogInformation($"Bancada2 - Processo {contaProcesso}: Imagem {imagemBookMatch.Width}x{imagemBookMatch.Height}");

                // Divide em 2/3 (topo) e 1/3 (lateral/pé)
                int doisTercos = (int)(imagemBookMatch.Width / 1.5); // 2/3 da largura
                int umTerco = imagemBookMatch.Width - doisTercos;

                var rectDoisTercos = new SKRectI(0, 0, doisTercos, imagemBookMatch.Height);
                var rectUmTerco = new SKRectI(doisTercos, 0, imagemBookMatch.Width, imagemBookMatch.Height);

                var imagemDoisTercos = CropBitmap(imagemBookMatch, rectDoisTercos);
                var imagemUmTerco = CropBitmap(imagemBookMatch, rectUmTerco);

                _logger.LogInformation($"DoisTercos: {imagemDoisTercos.Width}x{imagemDoisTercos.Height}");
                _logger.LogInformation($"UmTerco: {imagemUmTerco.Width}x{imagemUmTerco.Height}");

                SalvarDebug(imagemDoisTercos, $"DEBUG_Bancada2_P{contaProcesso}_01_DoisTercos.png");
                SalvarDebug(imagemUmTerco, $"DEBUG_Bancada2_P{contaProcesso}_02_UmTerco.png");

                // Redimensiona ambas as partes para 524x1520
                var bitmapORI = imagemDoisTercos.Resize(new SKImageInfo(524, 1520), SKFilterQuality.High);
                var bitmapORI2 = imagemUmTerco.Resize(new SKImageInfo(524, 1520), SKFilterQuality.High);
                var bitmapORI3 = imagemUmTerco.Resize(new SKImageInfo(524, 1520), SKFilterQuality.High);

                // Rotaciona bitmapORI3 270° (equivalente a Rotate270FlipNone)
                bitmapORI3 = RotateImage90(bitmapORI3, 270);

                SalvarDebug(bitmapORI, $"DEBUG_Bancada2_P{contaProcesso}_03_BitmapORI_524x1520.png");
                SalvarDebug(bitmapORI2, $"DEBUG_Bancada2_P{contaProcesso}_04_BitmapORI2_524x1520.png");
                SalvarDebug(bitmapORI3, $"DEBUG_Bancada2_P{contaProcesso}_05_BitmapORI3_Rotated270.png");

                // Transformação 1: Flip horizontal + Rotação 180° (Rotate180FlipX)
                imagemDoisTercos = RotateFlip180FlipX(imagemDoisTercos);
                SalvarDebug(imagemDoisTercos, $"DEBUG_Bancada2_P{contaProcesso}_06_Rotate180FlipX.png");

                // Transformação 2: DistortionInclina
                // VB.NET: DistortionInclina(ImagemDoisTercos, 1520, 650, 513, 1520, 430)
                imagemDoisTercos = _transformService.DistortionInclina(imagemDoisTercos, 1520, 650, 513, 1520, 430);
                SalvarDebug(imagemDoisTercos, $"DEBUG_Bancada2_P{contaProcesso}_07_DistortionInclina.png");

                // Transformação 3: Rotação 90°
                imagemDoisTercos = RotateImage90(imagemDoisTercos, 90);
                SalvarDebug(imagemDoisTercos, $"DEBUG_Bancada2_P{contaProcesso}_08_Rotate90.png");

                // Cálculo do ponto de inclinação (skew)
                int larguraDoQuadroComSkew = 524;
                int alturaDoQuadroComSkew = 1520;
                int ladoMaior = 1520;
                int fatorInclinacao = 450;

                float pontoInclinacaoEsquerdaInicial = alturaDoQuadroComSkew / ((float)ladoMaior / fatorInclinacao);
                _logger.LogInformation($"PontoInclinacaoEsquerdaInicial: {pontoInclinacaoEsquerdaInicial}");

                // Criar Quadro1 com SKEW (paralelogramo) - parte principal
                var quadroSkew = new SKBitmap(larguraDoQuadroComSkew, alturaDoQuadroComSkew);
                using (var canvas = new SKCanvas(quadroSkew))
                {
                    canvas.Clear(SKColors.Transparent);

                    // VB.NET usa DrawImage com 3 pontos para criar paralelogramo
                    // pt1 = (0, PontoInclinacaoEsquerdaInicial)  upper-left
                    // pt2 = (largura, 0)                          upper-right
                    // pt3 = (0, altura + PontoInclinacaoEsquerdaInicial) lower-left

                    float srcW = bitmapORI.Width;
                    float srcH = bitmapORI.Height;

                    float[] t = Transform2dFor3Points(
                        srcW, srcH,
                        0, pontoInclinacaoEsquerdaInicial,                                    // pt1
                        larguraDoQuadroComSkew, 0,                                            // pt2
                        0, alturaDoQuadroComSkew + pontoInclinacaoEsquerdaInicial            // pt3
                    );

                    var matrix = new SKMatrix
                    {
                        ScaleX = t[0], SkewX = t[3], TransX = t[6],
                        SkewY = t[1], ScaleY = t[4], TransY = t[7],
                        Persp0 = t[2], Persp1 = t[5], Persp2 = t[8]
                    };

                    canvas.SetMatrix(matrix);

                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

                    // Desenha bmp2 (que está vazio no VB.NET, então usa bitmapORI)
                    canvas.DrawBitmap(bitmapORI, 0, 0, paint);
                }
                SalvarDebug(quadroSkew, $"DEBUG_Bancada2_P{contaProcesso}_09_QuadroSkew_Paralelogramo.png");

                // Criar Quadro2 SEM SKEW (retângulo) - parte pé
                var quadroSkew2 = new SKBitmap(larguraDoQuadroComSkew, alturaDoQuadroComSkew);
                using (var canvas = new SKCanvas(quadroSkew2))
                {
                    canvas.Clear(SKColors.Transparent);

                    // VB.NET usa DrawImage com 3 pontos formando retângulo
                    // pt4 = (0, 0), pt5 = (largura, 0), pt6 = (0, altura)
                    // Isso é apenas uma cópia direta sem transformação

                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

                    canvas.DrawBitmap(bitmapORI2, 0, 0, paint);
                }
                SalvarDebug(quadroSkew2, $"DEBUG_Bancada2_P{contaProcesso}_10_QuadroSkew2_Retangulo.png");

                // Redimensionar Quadro3 (parte lateral) para 796x366
                var quadroSkew3 = bitmapORI3.Resize(new SKImageInfo(796, 366), SKFilterQuality.High);
                SalvarDebug(quadroSkew3, $"DEBUG_Bancada2_P{contaProcesso}_11_QuadroSkew3_796x366.png");

                // Criar canvas intermediários 1550x1550
                var emBrancoQuad1 = new SKBitmap(1550, 1550);
                var emBrancoQuad2 = new SKBitmap(1550, 1550);

                // Plotar QuadroSkew no canvas intermediário 1
                using (var canvas = new SKCanvas(emBrancoQuad1))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

                    // VB.NET: DrawImage(Imgbit, 556, 28) onde Imgbit é QuadroSkew redimensionado
                    var imgbit = quadroSkew.Resize(new SKImageInfo(524, 1520), SKFilterQuality.High);
                    canvas.DrawBitmap(imgbit, 556, 28, paint);
                }
                SalvarDebug(emBrancoQuad1, $"DEBUG_Bancada2_P{contaProcesso}_12_EmBrancoQuad1_Antes90.png");

                // Plotar QuadroSkew2 no canvas intermediário 2
                using (var canvas = new SKCanvas(emBrancoQuad2))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

                    canvas.DrawBitmap(quadroSkew2, 556, 28, paint);
                }
                SalvarDebug(emBrancoQuad2, $"DEBUG_Bancada2_P{contaProcesso}_13_EmBrancoQuad2_Antes90.png");

                // Rotacionar ambos os canvas 90°
                emBrancoQuad1 = _transformService.RotateImage(emBrancoQuad1, 90);
                emBrancoQuad2 = _transformService.RotateImage(emBrancoQuad2, 90);

                SalvarDebug(emBrancoQuad1, $"DEBUG_Bancada2_P{contaProcesso}_14_EmBrancoQuad1_Depois90.png");
                SalvarDebug(emBrancoQuad2, $"DEBUG_Bancada2_P{contaProcesso}_15_EmBrancoQuad2_Depois90.png");

                // Ajustes finais antes da montagem
                // VB.NET linha 6872: QuadroSkew3.RotateFlip(RotateFlipType.Rotate180FlipNone)
                quadroSkew3 = RotateFlip180(quadroSkew3);
                SalvarDebug(quadroSkew3, $"DEBUG_Bancada2_P{contaProcesso}_16_QuadroSkew3_Rotate180.png");

                // VB.NET linha 6873: ImagemDoisTercos.RotateFlip(RotateFlipType.RotateNoneFlipX)
                imagemDoisTercos = FlipHorizontal(imagemDoisTercos);
                SalvarDebug(imagemDoisTercos, $"DEBUG_Bancada2_P{contaProcesso}_17_ImagemDoisTercos_FlipX.png");

                // Monta o mosaico final 1680x1261
                int larguraMolduraVirtual = 1680;
                int alturaMolduraVirtual = 1261;

                var mosaicoEmBranco = new SKBitmap(larguraMolduraVirtual, alturaMolduraVirtual);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);

                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

                    // Composição em 3 camadas
                    // VB.NET linhas 6875-6877:
                    // point = (83, 574)
                    // point2 = (53, 565)
                    // point3 = (419, 125)
                    canvas.DrawBitmap(imagemDoisTercos, 83, 574, paint);     // Imagem principal transformada
                    canvas.DrawBitmap(emBrancoQuad2, 53, 565, paint);        // Parte pé da bancada
                    canvas.DrawBitmap(quadroSkew3, 419, 125, paint);         // Parte lateral
                }

                SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada2_P{contaProcesso}_18_MosaicoAntesMoldura.png");

                // Adiciona moldura bancada2.png
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    using var paint = new SKPaint
                    {
                        FilterQuality = SKFilterQuality.High,
                        IsAntialias = true
                    };

                    var moldura = CarregarRecurso("bancada2.png");
                    if (moldura != null)
                    {
                        canvas.DrawBitmap(moldura, 0, 0, paint);
                        _logger.LogInformation("Moldura bancada2.png aplicada");
                    }
                    else
                    {
                        _logger.LogWarning("Moldura bancada2.png NÃO foi carregada!");
                    }
                }

                SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada2_P{contaProcesso}_19_MosaicoComMoldura.png");

                // Flip horizontal se solicitado
                if (flip)
                {
                    mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                    SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada2_P{contaProcesso}_20_Final_Flipped.png");
                }
                else
                {
                    SalvarDebug(mosaicoEmBranco, $"DEBUG_Bancada2_P{contaProcesso}_20_Final.png");
                }

                resultado.Add(mosaicoEmBranco);

                // Limpa recursos temporários
                imagemBookMatch.Dispose();
                imagemDoisTercos.Dispose();
                imagemUmTerco.Dispose();
                bitmapORI.Dispose();
                bitmapORI2.Dispose();
                bitmapORI3.Dispose();
                quadroSkew.Dispose();
                quadroSkew2.Dispose();
                quadroSkew3.Dispose();
                emBrancoQuad1.Dispose();
                emBrancoQuad2.Dispose();
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

        private void SalvarDebug(SKBitmap bitmap, string nomeArquivo)
        {
            try
            {
                // Salvar em pasta acessível via web (wwwroot/debug)
                string pastaDebug = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "debug");
                Directory.CreateDirectory(pastaDebug);

                string caminhoCompleto = Path.Combine(pastaDebug, nomeArquivo);

                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(caminhoCompleto);
                data.SaveTo(stream);

                _logger.LogWarning($"🔍 DEBUG SALVO: http://mobile.picstone.com.br/debug/{nomeArquivo} ({bitmap.Width}x{bitmap.Height})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao salvar debug: {nomeArquivo}");
            }
        }

        /// <summary>
        /// Rotaciona imagem em ângulos ortogonais (90, 180, 270)
        /// Replica VB.NET RotateFlipType.Rotate90FlipNone, etc.
        /// </summary>
        private SKBitmap RotateImage90(SKBitmap source, int angle)
        {
            // Normaliza ângulo para 0-360
            angle = angle % 360;
            if (angle < 0) angle += 360;

            // Para rotações ortogonais, precisamos ajustar o tamanho do canvas
            int width = source.Width;
            int height = source.Height;

            // Para 90° e 270°, as dimensões são invertidas
            int newWidth = (angle == 90 || angle == 270) ? height : width;
            int newHeight = (angle == 90 || angle == 270) ? width : height;

            var surface = SKSurface.Create(new SKImageInfo(newWidth, newHeight));
            var canvas = surface.Canvas;

            canvas.Translate(newWidth / 2f, newHeight / 2f);
            canvas.RotateDegrees(angle);
            canvas.Translate(-width / 2f, -height / 2f);

            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.DrawBitmap(source, 0, 0, paint);

            var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
        }

        /// <summary>
        /// Rotaciona 180° e faz flip horizontal
        /// Replica VB.NET RotateFlipType.Rotate180FlipX
        /// </summary>
        private SKBitmap RotateFlip180FlipX(SKBitmap source)
        {
            var surface = SKSurface.Create(new SKImageInfo(source.Width, source.Height));
            var canvas = surface.Canvas;

            // Primeiro rotaciona 180°
            canvas.Translate(source.Width / 2f, source.Height / 2f);
            canvas.RotateDegrees(180);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);

            // Depois aplica flip horizontal
            canvas.Translate(source.Width / 2f, 0);
            canvas.Scale(-1, 1);
            canvas.Translate(-source.Width / 2f, 0);

            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.DrawBitmap(source, 0, 0, paint);

            var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Png, 100);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
        }

        /// <summary>
        /// Cria transformação afim usando 3 pontos de destino
        /// Similar ao DrawImage com 3 pontos do VB.NET
        /// </summary>
        private float[] Transform2dFor3Points(float srcW, float srcH,
                                                float x1, float y1,
                                                float x2, float y2,
                                                float x3, float y3)
        {
            // VB.NET DrawImage com 3 pontos mapeia:
            // (0, 0) → (x1, y1)         upper-left
            // (srcW, 0) → (x2, y2)      upper-right
            // (0, srcH) → (x3, y3)      lower-left
            // O quarto ponto é calculado automaticamente

            // Calcula o quarto ponto
            float x4 = x2 + (x3 - x1);
            float y4 = y2 + (y3 - y1);

            // Usa a função existente Transform2d do GraphicsTransformService
            // Mas aqui precisamos fazer manualmente porque é privada

            // Matriz de transformação afim
            // [ a  b  tx ]   [ x ]   [ x' ]
            // [ c  d  ty ] * [ y ] = [ y' ]
            // [ 0  0  1  ]   [ 1 ]   [ 1  ]

            // Sistema de equações:
            // x1 = a*0 + b*0 + tx  → tx = x1
            // y1 = c*0 + d*0 + ty  → ty = y1
            // x2 = a*srcW + b*0 + tx  → a = (x2 - tx) / srcW
            // y2 = c*srcW + d*0 + ty  → c = (y2 - ty) / srcW
            // x3 = a*0 + b*srcH + tx  → b = (x3 - tx) / srcH
            // y3 = c*0 + d*srcH + ty  → d = (y3 - ty) / srcH

            float tx = x1;
            float ty = y1;
            float a = (x2 - tx) / srcW;
            float c = (y2 - ty) / srcW;
            float b = (x3 - tx) / srcH;
            float d = (y3 - ty) / srcH;

            // Retorna matriz 3x3 em formato row-major como Transform2d
            // [m11, m12, m13, m21, m22, m23, m31, m32, m33]
            return new float[] { a, b, 0, c, d, 0, tx, ty, 1 };
        }
    }
}
