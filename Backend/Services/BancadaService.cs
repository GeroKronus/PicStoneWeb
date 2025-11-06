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
        private readonly string _logoPath;

        public BancadaService(ILogger<BancadaService> logger, GraphicsTransformService transformService)
        {
            _logger = logger;
            _transformService = transformService;
            _resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Bancadas");
            _logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "watermark.png");
        }

        /// <summary>
        /// Adiciona marca d'água (logo) no canto inferior direito
        /// </summary>
        private void AdicionarMarcaDagua(SKCanvas canvas, int canvasWidth, int canvasHeight)
        {
            if (!File.Exists(_logoPath))
            {
                _logger.LogWarning("Logo não encontrada em: {LogoPath}", _logoPath);
                return;
            }

            try
            {
                using var streamLogo = File.OpenRead(_logoPath);
                using var logo = SKBitmap.Decode(streamLogo);

                if (logo == null)
                {
                    _logger.LogWarning("Não foi possível decodificar a logo");
                    return;
                }

                // Usa tamanho original da logo (49x50 pixels)
                int logoWidth = logo.Width;
                int logoHeight = logo.Height;

                // Posição: canto inferior direito com margem de 5px
                int posX = canvasWidth - logoWidth - 5;
                int posY = canvasHeight - logoHeight - 5;

                // Desenha a logo sem redimensionar
                canvas.DrawBitmap(logo, posX, posY);

                _logger.LogInformation("Marca d'água adicionada no ambiente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar marca d'água");
            }
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
                    // Ajuste partePe: Y de 777 → 756 → 763 → 761 (-16px total)
                    // Ajuste partePe: X de -161 → -168 → -158 → -151 → -158 → -165 (-7px esquerda)
                    canvas.DrawBitmap(parteBancada, -131, 472, paint);
                    canvas.DrawBitmap(partePe, -165, 761, paint);
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

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
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

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
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
            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);

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
            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);

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
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
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
            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
        }

        /// <summary>
        /// Rotaciona 90° no sentido horário (VB.NET: Rotate90FlipNone)
        /// </summary>
        private SKBitmap RotateFlip90(SKBitmap source)
        {
            return RotateImage90(source, 90);
        }

        /// <summary>
        /// Rotaciona 270° no sentido horário (VB.NET: Rotate270FlipNone)
        /// </summary>
        private SKBitmap RotateFlip270(SKBitmap source)
        {
            return RotateImage90(source, 270);
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
            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);

            using var mStream = new MemoryStream(data.ToArray());
            return SKBitmap.Decode(mStream);
        }

        /// <summary>
        /// Rotaciona 90° e aplica flip horizontal (VB.NET: Rotate90FlipX)
        /// </summary>
        private SKBitmap RotateFlip90FlipX(SKBitmap source)
        {
            // VB.NET RotateFlipType.Rotate90FlipX: primeiro flip horizontal, depois rotação 90° horário
            // Resultado: largura e altura trocam
            var surface = SKSurface.Create(new SKImageInfo(source.Height, source.Width));
            var canvas = surface.Canvas;

            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            // ORDEM CORRETA (reversa no SkiaSharp):
            // 1. Translate para posicionar corretamente após rotação
            canvas.Translate(source.Height, 0);

            // 2. Rotaciona 90° horário
            canvas.RotateDegrees(90);

            // 3. Flip horizontal (Scale -1 em X)
            canvas.Scale(-1, 1);
            canvas.Translate(-source.Width, 0);

            // Desenha a imagem source no canvas transformado
            canvas.DrawBitmap(source, 0, 0, paint);

            var image = surface.Snapshot();
            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);

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

        /// <summary>
        /// Gera mockup de Bancada tipo 3 (Countertop #3)
        /// IMPLEMENTAÇÃO COMPLETA: 3 componentes (bmp7, bmp9, FaixaRotacionada) com 22 transformações
        /// Replica exatamente o VB.NET original (Form1.vb linhas 7290-7717)
        /// </summary>
        public List<SKBitmap> GerarBancada3(SKBitmap imagemOriginal, bool flip = false)
        {
            _logger.LogWarning($"========== INICIANDO GerarBancada3 COMPLETO - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
            var resultado = new List<SKBitmap>();

            // Bancada 3 - gera 2 versões (normal e 180°)
            for (int contaProcesso = 1; contaProcesso <= 2; contaProcesso++)
            {
                // DEBUG MODE: Substituir textura por verde sólido para testes
                bool DEBUG_GREEN_MODE = false; // Definir como true para ativar modo verde

                // ROTACIONA A IMAGEM ORIGINAL ANTES DE PROCESSAR (se contaProcesso == 2)
                SKBitmap imagemParaProcessar;
                if (contaProcesso == 1)
                {
                    imagemParaProcessar = imagemOriginal.Copy();
                    _logger.LogInformation($"contaProcesso={contaProcesso}: Usando imagem ORIGINAL");
                }
                else
                {
                    imagemParaProcessar = RotateFlip180(imagemOriginal);
                    _logger.LogInformation($"contaProcesso={contaProcesso}: Usando imagem ROTACIONADA 180°");
                }

                SKBitmap imagemBookMatch;
                if (DEBUG_GREEN_MODE)
                {
                    // Criar superfície verde sólida 2000x1200
                    imagemBookMatch = new SKBitmap(2000, 1200);
                    using (var canvas = new SKCanvas(imagemBookMatch))
                    {
                        canvas.Clear(new SKColor(0, 200, 100, 180)); // Verde semi-transparente
                    }
                    _logger.LogWarning("🟢 DEBUG MODE: Usando superfície VERDE sólida");
                }
                else
                {
                    // imagemParaProcessar já foi rotacionada (ou não) conforme contaProcesso
                    imagemBookMatch = imagemParaProcessar.Copy();
                }
                _logger.LogInformation($"BookMatch entrada: {imagemBookMatch.Width}x{imagemBookMatch.Height}");

                // ============ DIVISÃO DA FAIXA: 95% PRINCIPAL + 5% FAIXA LATERAL ============
                // A faixa deve ter o COMPRIMENTO TOTAL (Height) e 5% da LARGURA (Width)
                // Dividimos na ALTURA (Height): os primeiros 95% são a parte principal, os últimos 5% são a faixa
                int alturaPrincipal = (int)(imagemBookMatch.Height * 0.95);
                int alturaFaixa = imagemBookMatch.Height - alturaPrincipal;

                _logger.LogInformation($"Divisão da faixa: 95% principal={alturaPrincipal}px altura, 5% faixa={alturaFaixa}px altura");
                _logger.LogInformation($"Faixa terá: largura={imagemBookMatch.Width}px (100% do comprimento), altura={alturaFaixa}px (5% da largura)");

                var rectPrincipal = new SKRectI(0, 0, imagemBookMatch.Width, alturaPrincipal);
                var rectFaixaLateral = new SKRectI(0, alturaPrincipal, imagemBookMatch.Width, imagemBookMatch.Height);

                var imagemPrincipal = CropBitmap(imagemBookMatch, rectPrincipal);
                var imagemFaixaLateral = CropBitmap(imagemBookMatch, rectFaixaLateral);

                _logger.LogInformation($"✓ Parte principal (95%): {imagemPrincipal.Width}x{imagemPrincipal.Height}");
                _logger.LogInformation($"✓ Faixa lateral (5%): {imagemFaixaLateral.Width}x{imagemFaixaLateral.Height}");

                // ============ DIVISÃO VERTICAL DA PARTE PRINCIPAL: 2/3 E 1/3 ============
                int doisTercos = (int)(imagemPrincipal.Width / 1.5);
                int umTerco = imagemPrincipal.Width - doisTercos;

                _logger.LogInformation($"Divisão vertical (principal): 2/3={doisTercos}px, 1/3={umTerco}px");

                var rectDoisTercos = new SKRectI(0, 0, doisTercos, imagemPrincipal.Height);
                var rectUmTerco = new SKRectI(doisTercos - 10, 0, doisTercos - 10 + umTerco + 10, imagemPrincipal.Height);

                var imagemDoisTercos = CropBitmap(imagemPrincipal, rectDoisTercos);
                var imagemUmTerco = CropBitmap(imagemPrincipal, rectUmTerco);

                _logger.LogInformation($"✓ Principal 2/3: {imagemDoisTercos.Width}x{imagemDoisTercos.Height}");
                _logger.LogInformation($"✓ Principal 1/3: {imagemUmTerco.Width}x{imagemUmTerco.Height}");

                // ============ DIVISÃO HORIZONTAL DA FAIXA LATERAL: 2/3 E 1/3 ============
                // A faixa agora é horizontal (comprimento total x 5% largura)
                // Dividimos na LARGURA (Width): 2/3 esquerda + 1/3 direita
                int faixaDoisTercos = (int)(imagemFaixaLateral.Width / 1.5);
                int faixaUmTerco = imagemFaixaLateral.Width - faixaDoisTercos;

                _logger.LogInformation($"Divisão horizontal (faixa): 2/3={faixaDoisTercos}px largura, 1/3={faixaUmTerco}px largura");

                // Dividimos na LARGURA (Width)
                var rectFaixaSuperior = new SKRectI(0, 0, faixaDoisTercos, imagemFaixaLateral.Height);
                var rectFaixaInferior = new SKRectI(faixaDoisTercos, 0, imagemFaixaLateral.Width, imagemFaixaLateral.Height);

                var imagemFaixaSuperior = CropBitmap(imagemFaixaLateral, rectFaixaSuperior);
                var imagemFaixaInferior = CropBitmap(imagemFaixaLateral, rectFaixaInferior);

                _logger.LogInformation($"✓ Faixa superior (2/3): {imagemFaixaSuperior.Width}x{imagemFaixaSuperior.Height}");
                _logger.LogInformation($"✓ Faixa inferior (1/3): {imagemFaixaInferior.Width}x{imagemFaixaInferior.Height}");

                // ============ PARTE 1: bmp7 (COMPONENTE PRINCIPAL) - 12 TRANSFORMAÇÕES ============
                _logger.LogInformation("=== Iniciando PARTE 1: bmp7 (12 transformações) ===");

                // Transformação 1: Rotate90
                // ============ NOVA LÓGICA: MapToCustomQuadrilateral ============
                // SUBSTITUINDO 12 transformações complexas por 1 transformação direta

                _logger.LogInformation($"imagemDoisTercos (2/3 largura): {imagemDoisTercos.Width}x{imagemDoisTercos.Height} (paisagem)");

                // TRANSFORMAÇÃO PRINCIPAL: MapToCustomQuadrilateral
                // Aplica transformação de perspectiva direta nos 2/3 da largura
                _logger.LogInformation("Aplicando MapToCustomQuadrilateral...");
                var bmp7 = _transformService.MapToCustomQuadrilateral(
                    input: imagemDoisTercos,
                    canvasWidth: 2000,
                    canvasHeight: 1863  // Canvas final da Bancada 3
                );
                _logger.LogInformation($"MapToCustomQuadrilateral -> bmp7: {bmp7.Width}x{bmp7.Height}");

                // ============ PARTE 3: FAIXA LATERAL SUPERIOR - NOVA TRANSFORMAÇÃO ============
                _logger.LogInformation("=== Iniciando PARTE 3: Faixa Lateral Superior (MapToCustomQuadrilateral_FaixaSuperior) ===");
                _logger.LogInformation($"imagemFaixaSuperior (3/4 largura faixa 5%): {imagemFaixaSuperior.Width}x{imagemFaixaSuperior.Height}");

                // SEM ROTAÇÃO - a faixa já está na orientação correta (horizontal com comprimento total)
                _logger.LogInformation("Faixa já está na orientação correta, sem necessidade de rotação");

                // TRANSFORMAÇÃO DA FAIXA SUPERIOR: MapToCustomQuadrilateral_FaixaSuperior
                _logger.LogInformation("Aplicando MapToCustomQuadrilateral_FaixaSuperior...");
                var faixaSuperiorTransformada = _transformService.MapToCustomQuadrilateral_FaixaSuperior(
                    input: imagemFaixaSuperior,
                    canvasWidth: 2000,
                    canvasHeight: 1863  // Canvas final da Bancada 3
                );
                _logger.LogInformation($"MapToCustomQuadrilateral_FaixaSuperior -> {faixaSuperiorTransformada.Width}x{faixaSuperiorTransformada.Height}");

                // ============ PARTE 2: bmp9 (PÉ/LATERAL) - NOVA TRANSFORMAÇÃO DIRETA ============
                _logger.LogInformation("=== Iniciando PARTE 2: bmp9 (MapToCustomQuadrilateral_Pe) ===");
                _logger.LogInformation($"imagemUmTerco (1/3 largura): {imagemUmTerco.Width}x{imagemUmTerco.Height}");

                // ROTAÇÃO PARA CONTINUIDADE DOS VEIOS
                // O pé é uma continuação vertical do topo, então precisa rotacionar 90° (sentido horário)
                // para manter os veios do mármore em continuidade
                var imagemUmTercoRotacionada = RotateFlip90(imagemUmTerco);
                _logger.LogInformation($"Rotacionado 90° para continuidade dos veios: {imagemUmTercoRotacionada.Width}x{imagemUmTercoRotacionada.Height}");

                // TRANSFORMAÇÃO PRINCIPAL: MapToCustomQuadrilateral_Pe
                // Substitui as 5 transformações antigas (Resize → Rotate90FlipX → Distortion → FlipX → Skew2)
                // por uma única transformação de perspectiva direta
                _logger.LogInformation("Aplicando MapToCustomQuadrilateral_Pe...");
                var bmp9 = _transformService.MapToCustomQuadrilateral_Pe(
                    input: imagemUmTercoRotacionada,
                    canvasWidth: 2000,
                    canvasHeight: 1863  // Canvas final da Bancada 3
                );
                _logger.LogInformation($"MapToCustomQuadrilateral_Pe -> bmp9: {bmp9.Width}x{bmp9.Height}");

                // ============ PARTE 4: FAIXA LATERAL INFERIOR - NOVA TRANSFORMAÇÃO ============
                _logger.LogInformation("=== Iniciando PARTE 4: Faixa Lateral Inferior (MapToCustomQuadrilateral_FaixaInferior) ===");
                _logger.LogInformation($"imagemFaixaInferior (1/4 largura faixa 5%): {imagemFaixaInferior.Width}x{imagemFaixaInferior.Height}");

                // ROTAÇÃO -90° (270°) PARA CONTINUIDADE DOS VEIOS
                var imagemFaixaInferiorRotacionada = RotateFlip270(imagemFaixaInferior);
                _logger.LogInformation($"Rotacionado -90° para continuidade dos veios: {imagemFaixaInferiorRotacionada.Width}x{imagemFaixaInferiorRotacionada.Height}");

                // TRANSFORMAÇÃO DA FAIXA INFERIOR: MapToCustomQuadrilateral_FaixaInferior
                _logger.LogInformation("Aplicando MapToCustomQuadrilateral_FaixaInferior...");
                var faixaInferiorTransformada = _transformService.MapToCustomQuadrilateral_FaixaInferior(
                    input: imagemFaixaInferiorRotacionada,
                    canvasWidth: 2000,
                    canvasHeight: 1863  // Canvas final da Bancada 3
                );
                _logger.LogInformation($"MapToCustomQuadrilateral_FaixaInferior -> {faixaInferiorTransformada.Width}x{faixaInferiorTransformada.Height}");

                // ============ MONTAGEM FINAL: Canvas 2000x1863 ============
                _logger.LogInformation("=== Montagem final: MÁRMORE primeiro, MOLDURA por cima (overlay) ===");

                var mosaicoEmBranco = new SKBitmap(2000, 1863);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };

                    // ORDEM CORRETA:
                    // 1. Mármore transformado (bmp7 - topo) - FUNDO
                    canvas.DrawBitmap(bmp7, 0, 0, paint);
                    _logger.LogInformation("1. bmp7 (topo transformado) desenhado at (0, 0)");

                    // 2. Mármore transformado (bmp9 - pé/lateral) - FUNDO
                    canvas.DrawBitmap(bmp9, 0, 0, paint);
                    _logger.LogInformation("2. bmp9 (pé transformado) desenhado at (0, 0)");

                    // 3. Faixa lateral superior transformada - FUNDO
                    canvas.DrawBitmap(faixaSuperiorTransformada, 0, 0, paint);
                    _logger.LogInformation("3. faixaSuperiorTransformada desenhada at (0, 0)");

                    // 4. Faixa lateral inferior transformada - FUNDO
                    canvas.DrawBitmap(faixaInferiorTransformada, 0, 0, paint);
                    _logger.LogInformation("4. faixaInferiorTransformada desenhada at (0, 0)");

                    // 5. Moldura vazada - OVERLAY (por cima do mármore)
                    var moldura = CarregarRecurso("bancada3.png");
                    if (moldura != null)
                    {
                        canvas.DrawBitmap(moldura, 0, 0, paint);
                        _logger.LogInformation("5. Moldura vazada sobreposta (overlay)");
                    }

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
                }

                if (flip) mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                resultado.Add(mosaicoEmBranco);

                // Dispose de todos os bitmaps intermediários
                imagemBookMatch.Dispose();
                imagemPrincipal.Dispose();
                imagemFaixaLateral.Dispose();
                imagemDoisTercos.Dispose();
                imagemUmTerco.Dispose();
                imagemUmTercoRotacionada.Dispose();
                imagemFaixaSuperior.Dispose();
                imagemFaixaInferior.Dispose();
                imagemFaixaInferiorRotacionada.Dispose();
                // bmp2, faixa1, faixa1Pronta, canvas1500, faixaRotacionada removidos (lógica antiga)
                bmp7.Dispose();
                bmp9.Dispose();
                faixaSuperiorTransformada.Dispose();
                faixaInferiorTransformada.Dispose();
            }

            _logger.LogWarning("========== GerarBancada3 COMPLETO FINALIZADO ==========");
            return resultado;
        }

        /// <summary>
        /// Gera mockup de Bancada tipo 4 (Countertop #4)
        /// Divide em frente e lateral
        /// </summary>
        public List<SKBitmap> GerarBancada4(SKBitmap imagemOriginal, bool flip = false)
        {
            _logger.LogWarning($"========== INICIANDO GerarBancada4 - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
            var resultado = new List<SKBitmap>();

            for (int contaProcesso = 1; contaProcesso <= 2; contaProcesso++)
            {
                SKBitmap imagemBookMatch = contaProcesso == 1 ? imagemOriginal.Copy() : RotateFlip180(imagemOriginal);

                // Divide em frente (1600px) e lateral (500px) - Total 2100px
                int larguraFrente = (int)(imagemBookMatch.Width * 0.76); // ~76% para frente
                int larguraLateral = imagemBookMatch.Width - larguraFrente;

                var rectFrente = new SKRectI(larguraLateral, 0, imagemBookMatch.Width, imagemBookMatch.Height);
                var rectLateral = new SKRectI(0, 0, larguraLateral, imagemBookMatch.Height);

                var imagemFrente = CropBitmap(imagemBookMatch, rectFrente);
                var imagemLateral = CropBitmap(imagemBookMatch, rectLateral);

                // Aplica transformações FRENTE (VB.NET: Rotate180 → Distortion → Skew2 → Rotate180)
                // Mantém dimensões originais do crop - DistortionInclina faz o resize internamente
                var frente = RotateFlip180(imagemFrente);  // Rotate180FlipNone
                frente = _transformService.DistortionInclina(frente, 390, 280, 776, 398, 0);
                frente = _transformService.Skew2(frente, 0, 220);  // Skew2 (inclinação invertida)
                frente = RotateFlip180(frente);  // Rotate180FlipNone novamente

                // Aplica transformações LATERAL (VB.NET: Distortion → Skew)
                // Mantém dimensões originais do crop - DistortionInclina faz o resize internamente
                var lateral = _transformService.DistortionInclina(imagemLateral, 390, 280, 182, 399, 0);
                lateral = _transformService.SkewLateral(lateral, 0, 90);  // Skew com 4º ponto corrigido

                // Monta mosaico 1523x1238
                var mosaicoEmBranco = new SKBitmap(1523, 1238);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };

                    // GEOMETRIA: Alinhar no ponto de interseção X=391
                    // Skew lateral=90, skew frente=220
                    // lateralY = frenteY + skewFrente - skewLateral = 545 + 220 - 90 = 675
                    canvas.DrawBitmap(lateral, 209, 675, paint);  // Ajustado de 676 para 675
                    canvas.DrawBitmap(frente, 391, 545, paint);
                }

                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    var moldura = CarregarRecurso("bancada4.png");
                    if (moldura != null) canvas.DrawBitmap(moldura, 0, 0, paint);

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
                }

                if (flip) mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                resultado.Add(mosaicoEmBranco);

                imagemBookMatch.Dispose();
                imagemFrente.Dispose();
                imagemLateral.Dispose();
                frente.Dispose();
                lateral.Dispose();
            }

            return resultado;
        }

        /// <summary>
        /// Gera mockup de Bancada tipo 5 (Countertop #5)
        /// NOVA ESTRATÉGIA: Usa 3 transformações MapToCustomQuadrilateral
        /// - 1/3 esquerdo transformado (0-33%)
        /// - 2/3 direitos transformados direto (33-100%)
        /// - 2/3 direitos flipados + transformados (33-100%)
        /// Gera 4 variações (normal, 180°, FlipX, 180° + FlipX)
        /// </summary>
        public List<SKBitmap> GerarBancada5(SKBitmap imagemOriginal, bool flip = false)
        {
            _logger.LogWarning($"========== INICIANDO GerarBancada5 NOVA LÓGICA - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
            var resultado = new List<SKBitmap>();

            // Bancada5 gera 4 variações (normal, 180°, FlipX, 180° + FlipX)
            for (int contaProcesso = 1; contaProcesso <= 4; contaProcesso++)
            {
                SKBitmap imagemBookMatch;
                if (contaProcesso == 1)
                    imagemBookMatch = imagemOriginal.Copy();
                else if (contaProcesso == 2)
                    imagemBookMatch = RotateFlip180(imagemOriginal);
                else if (contaProcesso == 3)
                    imagemBookMatch = FlipHorizontal(imagemOriginal);
                else
                    imagemBookMatch = RotateFlip180(FlipHorizontal(imagemOriginal));

                _logger.LogInformation($"Processando variação {contaProcesso} - BookMatch: {imagemBookMatch.Width}x{imagemBookMatch.Height}");

                // Divide em 1/3 esquerdo e 2/3 direitos
                int umTerco = imagemBookMatch.Width / 3;
                int doisTercosLargura = imagemBookMatch.Width - umTerco;

                // Recorta 1/3 esquerdo (0% a 33%)
                var rectUmTerco = new SKRectI(0, 0, umTerco, imagemBookMatch.Height);
                var imagemUmTerco = CropBitmap(imagemBookMatch, rectUmTerco);
                _logger.LogInformation($"1/3 esquerdo: {imagemUmTerco.Width}x{imagemUmTerco.Height}");

                // Recorta 2/3 direitos (33% a 100%)
                var rectDoisTercos = new SKRectI(umTerco, 0, imagemBookMatch.Width, imagemBookMatch.Height);
                var imagemDoisTercos = CropBitmap(imagemBookMatch, rectDoisTercos);
                _logger.LogInformation($"2/3 direitos: {imagemDoisTercos.Width}x{imagemDoisTercos.Height}");

                // TRANSFORMAÇÃO 1: 1/3 esquerdo
                var umTercoTransformado = MapToCustomQuadrilateral_Bancada3_TEST(
                    input: imagemUmTerco,
                    canvasWidth: 1500,
                    canvasHeight: 1068,
                    v1x: 188, v1y: 601,    // topLeft
                    v2x: 309, v2y: 623,    // topRight
                    v4x: 196, v4y: 922,    // bottomLeft
                    v3x: 309, v3y: 1036    // bottomRight
                );
                _logger.LogInformation($"1/3 transformado: {umTercoTransformado.Width}x{umTercoTransformado.Height}");

                // TRANSFORMAÇÃO 2: 2/3 direitos direto
                var doisTercosTransformado = MapToCustomQuadrilateral_Bancada3_TEST(
                    input: imagemDoisTercos,
                    canvasWidth: 1500,
                    canvasHeight: 1068,
                    v1x: 309, v1y: 623,    // topLeft
                    v2x: 670, v2y: 598,    // topRight
                    v4x: 309, v4y: 1036,   // bottomLeft
                    v3x: 670, v3y: 939     // bottomRight
                );
                _logger.LogInformation($"2/3 direto transformado: {doisTercosTransformado.Width}x{doisTercosTransformado.Height}");

                // TRANSFORMAÇÃO 3: 2/3 direitos flipados
                var imagemDoisTercosFlipped = FlipHorizontal(imagemDoisTercos);
                var doisTercosFlippedTransformado = MapToCustomQuadrilateral_Bancada3_TEST(
                    input: imagemDoisTercosFlipped,
                    canvasWidth: 1500,
                    canvasHeight: 1068,
                    v1x: 670, v1y: 598,    // topLeft
                    v2x: 968, v2y: 577,    // topRight
                    v4x: 670, v4y: 937,    // bottomLeft
                    v3x: 975, v3y: 854     // bottomRight
                );
                _logger.LogInformation($"2/3 flipado transformado: {doisTercosFlippedTransformado.Width}x{doisTercosFlippedTransformado.Height}");

                // Monta o canvas 1500x1068 desenhando as 3 peças
                var mosaicoEmBranco = new SKBitmap(1500, 1068);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };

                    // Desenha as 3 peças transformadas (já estão no canvas 1500x1068)
                    // As peças já possuem as coordenadas corretas dentro do canvas, então desenhamos em (0,0)
                    canvas.DrawBitmap(umTercoTransformado, 0, 0, paint);
                    canvas.DrawBitmap(doisTercosTransformado, 0, 0, paint);
                    canvas.DrawBitmap(doisTercosFlippedTransformado, 0, 0, paint);

                    _logger.LogInformation("3 peças desenhadas no canvas 1500x1068");
                }

                // Adiciona a moldura por cima
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    var moldura = CarregarRecurso("bancada5.png");
                    if (moldura != null)
                    {
                        canvas.DrawBitmap(moldura, 0, 0, paint);
                        _logger.LogInformation("Moldura bancada5.png aplicada");
                    }
                    else
                    {
                        _logger.LogWarning("Moldura bancada5.png NÃO encontrada!");
                    }

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
                }

                if (flip) mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                resultado.Add(mosaicoEmBranco);

                // Limpa recursos
                imagemBookMatch.Dispose();
                imagemUmTerco.Dispose();
                imagemDoisTercos.Dispose();
                imagemDoisTercosFlipped.Dispose();
                umTercoTransformado.Dispose();
                doisTercosTransformado.Dispose();
                doisTercosFlippedTransformado.Dispose();
            }

            _logger.LogWarning($"========== GerarBancada5 CONCLUÍDO - {resultado.Count} mockups gerados ==========");
            return resultado;
        }

        /// <summary>
        /// Gera mockup de Bancada tipo 6 (Countertop #6)
        /// Sistema de faixas com divisão 3/4
        /// </summary>
        public List<SKBitmap> GerarBancada6(SKBitmap imagemOriginal, bool flip = false)
        {
            _logger.LogWarning($"========== INICIANDO GerarBancada6 - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
            var resultado = new List<SKBitmap>();

            for (int contaProcesso = 1; contaProcesso <= 2; contaProcesso++)
            {
                SKBitmap imagemBookMatch = contaProcesso == 1 ? imagemOriginal.Copy() : RotateFlip180(imagemOriginal);

                // Divide em 3/4 (bancada) e 1/4 (pé)
                int tresQuartos = (int)(imagemBookMatch.Width / 1.3333);
                var rectBancada = new SKRectI(0, 0, tresQuartos, imagemBookMatch.Height);
                var imagemBancada = CropBitmap(imagemBookMatch, rectBancada);

                // Usa 90% da altura para bancada, 10% para faixa
                int alturaBancada = (int)(imagemBancada.Height * 0.9);
                var rect90 = new SKRectI(0, 0, imagemBancada.Width, alturaBancada);
                var bmp90 = CropBitmap(imagemBancada, rect90);

                // Redimensiona e aplica transformação
                var bmp = bmp90.Resize(new SKImageInfo(290, 686), SKFilterQuality.High);
                bmp = _transformService.DistortionInclina(bmp, 290, 114, 686, 290, 176);

                // Monta mosaico 2500x1632
                var mosaicoEmBranco = new SKBitmap(2500, 1632);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    canvas.DrawBitmap(bmp, -269, 825, paint);
                }

                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    var moldura = CarregarRecurso("bancada6.png");
                    if (moldura != null) canvas.DrawBitmap(moldura, 0, 0, paint);

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
                }

                if (flip) mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                resultado.Add(mosaicoEmBranco);

                imagemBookMatch.Dispose();
                imagemBancada.Dispose();
                bmp90.Dispose();
                bmp.Dispose();
            }

            return resultado;
        }

        /// <summary>
        /// Gera mockup de Bancada tipo 7 (Countertop #7)
        /// Com rotação de -21.5 graus e faixa decorativa
        /// </summary>
        public List<SKBitmap> GerarBancada7(SKBitmap imagemOriginal, bool flip = false)
        {
            _logger.LogWarning($"========== INICIANDO GerarBancada7 - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
            var resultado = new List<SKBitmap>();

            for (int contaProcesso = 1; contaProcesso <= 2; contaProcesso++)
            {
                SKBitmap imagemBookMatch = contaProcesso == 1 ? imagemOriginal.Copy() : RotateFlip180(imagemOriginal);

                // Divide em 2/3 e 1/3
                int doisTercos = (int)(imagemBookMatch.Width / 1.5);
                var rectDoisTercos = new SKRectI(0, 0, doisTercos, imagemBookMatch.Height);
                var imagemDoisTercos = CropBitmap(imagemBookMatch, rectDoisTercos);

                // Redimensiona para grande canvas 4000x4000
                var bmp = imagemDoisTercos.Resize(new SKImageInfo(2864, 993), SKFilterQuality.High);
                bmp = _transformService.DistortionInclina(bmp, 2864, 1000, 993, 2864, 1864);

                // Rotaciona -21.5 graus
                var bancadaRotacionada = _transformService.RotateImage(bmp, -21.5f);

                // Monta mosaico 2500x1667
                var mosaicoEmBranco = new SKBitmap(2500, 1667);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    canvas.DrawBitmap(bancadaRotacionada, -211, 279, paint);
                }

                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    var moldura = CarregarRecurso("bancada7.png");
                    if (moldura != null) canvas.DrawBitmap(moldura, 0, 0, paint);

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
                }

                if (flip) mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                resultado.Add(mosaicoEmBranco);

                imagemBookMatch.Dispose();
                imagemDoisTercos.Dispose();
                bmp.Dispose();
                bancadaRotacionada.Dispose();
            }

            return resultado;
        }

        /// <summary>
        /// Gera mockup de Bancada tipo 8 (Countertop #8)
        /// Com rotação de -57.2 graus e faixa vertical
        /// </summary>
        public List<SKBitmap> GerarBancada8(SKBitmap imagemOriginal, bool flip = false)
        {
            _logger.LogWarning($"========== INICIANDO GerarBancada8 - Imagem {imagemOriginal.Width}x{imagemOriginal.Height}, flip={flip} ==========");
            var resultado = new List<SKBitmap>();

            for (int contaProcesso = 1; contaProcesso <= 2; contaProcesso++)
            {
                SKBitmap imagemBookMatch = contaProcesso == 1 ? imagemOriginal.Copy() : RotateFlip180(imagemOriginal);

                // Divide em 2/3 e 1/3
                int doisTercos = (int)(imagemBookMatch.Width / 1.5);
                var rectDoisTercos = new SKRectI(0, 0, doisTercos, imagemBookMatch.Height);
                var imagemDoisTercos = CropBitmap(imagemBookMatch, rectDoisTercos);

                // Redimensiona
                var bmp = imagemDoisTercos.Resize(new SKImageInfo(1816, 1206), SKFilterQuality.High);
                bmp = _transformService.DistortionInclina(bmp, 1816, 1077, 1206, 1816, 0);

                // Aplica Skew com valor 950
                bmp = _transformService.SkewSimples(bmp, 0, 950);

                // Rotaciona -57.2 graus
                var bancadaRotacionada = _transformService.RotateImage(bmp, -57.2f);

                // Monta mosaico 2500x1554
                var mosaicoEmBranco = new SKBitmap(2500, 1554);
                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    canvas.DrawBitmap(bancadaRotacionada, -89, -432, paint);
                }

                using (var canvas = new SKCanvas(mosaicoEmBranco))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                    var moldura = CarregarRecurso("bancada8.png");
                    if (moldura != null) canvas.DrawBitmap(moldura, 0, 0, paint);

                    // Adiciona marca d'água
                    AdicionarMarcaDagua(canvas, mosaicoEmBranco.Width, mosaicoEmBranco.Height);
                }

                if (flip) mosaicoEmBranco = FlipHorizontal(mosaicoEmBranco);
                resultado.Add(mosaicoEmBranco);

                imagemBookMatch.Dispose();
                imagemDoisTercos.Dispose();
                bmp.Dispose();
                bancadaRotacionada.Dispose();
            }

            return resultado;
        }

        /// <summary>
        /// Método de TESTE para Bancada 5 - Nova estratégia
        /// Aplica MapToVertices com coordenadas customizadas
        /// </summary>
        public SKBitmap MapToCustomQuadrilateral_Bancada3_TEST(SKBitmap input, int canvasWidth, int canvasHeight,
                                                                 int v1x, int v1y, int v2x, int v2y,
                                                                 int v4x, int v4y, int v3x, int v3y)
        {
            return _transformService.MapToVertices(
                input: input,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                v1x: v1x, v1y: v1y,
                v2x: v2x, v2y: v2y,
                v4x: v4x, v4y: v4y,
                v3x: v3x, v3y: v3y
            );
        }
    }
}
