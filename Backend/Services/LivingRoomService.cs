using SkiaSharp;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço para geração de mockups de Living Room
    /// Tradução do VB.NET Sub Sala1() para C# usando SkiaSharp
    /// </summary>
    public class LivingRoomService
    {
        private readonly GraphicsTransformService _transformService;
        private readonly ILogger<LivingRoomService> _logger;
        private readonly ImageWatermarkService _watermark;

        public LivingRoomService(GraphicsTransformService transformService, ILogger<LivingRoomService> logger, ImageWatermarkService watermark)
        {
            _transformService = transformService;
            _logger = logger;
            _watermark = watermark;
        }

        /// <summary>
        /// Gera mockup Living Room #1 (Sub Sala1 do VB.NET)
        /// </summary>
        /// <param name="imagemCropada">Imagem cropada pelo usuário</param>
        /// <returns>Lista com 4 quadrantes gerados</returns>
        public List<SKBitmap> GerarLivingRoom1(SKBitmap imagemCropada)
        {
            try
            {
                // Parâmetros do VB.NET Sub Sala1()
                const int larguraDaMoldura = 1080;
                const int alturaDaMoldura = 1342;
                const int larguraDoQuadroSemSkew = 440;
                const int alturaDoQuadroSemSkew = 1010;
                const int coordPlotSkewX = 550;
                const int coordPlotSkewY = 22;
                const int ladoMaior = 1001;
                const int ladoMenor = 628;
                const int fatorInclinacao = 230;
                const int tamanhoDoQuadro = 1500;

                _logger.LogInformation("=== INICIANDO LIVING ROOM #1 ===");
                _logger.LogInformation("Imagem original: {W}x{H}", imagemCropada.Width, imagemCropada.Height);

                // PASSO 1: Redimensiona para largura fixa de 1500px
                float fatorDeAjuste = (float)imagemCropada.Width / tamanhoDoQuadro;
                int novaAltura = (int)(imagemCropada.Height / fatorDeAjuste);

                using var imagemRedimensionada = imagemCropada.Resize(
                    new SKImageInfo(tamanhoDoQuadro, novaAltura),
                    SKFilterQuality.High);

                _logger.LogInformation("Imagem redimensionada: {W}x{H}", tamanhoDoQuadro, novaAltura);

                // PASSO 2: Cria as 4 versões rotacionadas (BookMatch)
                using var bitmap90E = CriarBitmapRotacionado(imagemRedimensionada, SKEncodedOrigin.LeftBottom); // 90° sem flip
                using var bitmap90D = FlipHorizontal(bitmap90E); // 90° com flip
                using var bitmap270E = CriarBitmapRotacionado(imagemRedimensionada, SKEncodedOrigin.RightTop); // 270° sem flip
                using var bitmap270D = FlipHorizontal(bitmap270E); // 270° com flip

                _logger.LogInformation("BookMatch criado: 90E={W}x{H}, 90D={W}x{H}, 270E={W}x{H}, 270D={W}x{H}",
                    bitmap90E.Width, bitmap90E.Height,
                    bitmap90D.Width, bitmap90D.Height,
                    bitmap270E.Width, bitmap270E.Height,
                    bitmap270D.Width, bitmap270D.Height);

                // PASSO 3: Cria os 4 mosaicos (quadrantes) como no VB.NET
                var larguraMosaico = (novaAltura * 2) + 1; // +1 para linha divisória opcional
                var alturaMosaico = tamanhoDoQuadro;

                var quadrantes = new List<SKBitmap>();

                // Gera os 4 quadrantes
                for (int quadrante = 1; quadrante <= 4; quadrante++)
                {
                    _logger.LogInformation("--- Gerando quadrante {Q}/4 ---", quadrante);

                    // Cria mosaico (2 chapas lado a lado)
                    using var mosaico = new SKBitmap(larguraMosaico, alturaMosaico);
                    using var canvas = new SKCanvas(mosaico);
                    canvas.Clear(SKColors.White);

                    // Desenha as 2 chapas lado a lado conforme o quadrante
                    switch (quadrante)
                    {
                        case 1: // 90E + 90D
                            canvas.DrawBitmap(bitmap90E, 0, 0);
                            canvas.DrawBitmap(bitmap90D, novaAltura + 1, 0);
                            break;
                        case 2: // 90D + 90E (invertido)
                            canvas.DrawBitmap(bitmap90D, 0, 0);
                            canvas.DrawBitmap(bitmap90E, novaAltura + 1, 0);
                            break;
                        case 3: // 270E + 270D
                            canvas.DrawBitmap(bitmap270E, 0, 0);
                            canvas.DrawBitmap(bitmap270D, novaAltura + 1, 0);
                            break;
                        case 4: // 270D + 270E (invertido)
                            canvas.DrawBitmap(bitmap270D, 0, 0);
                            canvas.DrawBitmap(bitmap270E, novaAltura + 1, 0);
                            break;
                    }

                    _logger.LogInformation("Mosaico criado: {W}x{H}", mosaico.Width, mosaico.Height);

                    // PASSO 4: Aplica DistortionInclina (perspectiva vertical + skew)
                    using var quadranteDistorcido = _transformService.DistortionInclina(
                        imagem: mosaico,
                        ladoMaior: ladoMaior,
                        ladoMenor: ladoMenor,
                        novaLargura: larguraDoQuadroSemSkew,
                        novaAltura: alturaDoQuadroSemSkew,
                        inclinacao: fatorInclinacao
                    );

                    _logger.LogInformation("Quadrante distorcido: {W}x{H}", quadranteDistorcido.Width, quadranteDistorcido.Height);

                    // PASSO 5: Cria canvas vazio (1080x1342px)
                    var canvasBase = new SKBitmap(larguraDaMoldura, alturaDaMoldura);
                    using var canvasFinal = new SKCanvas(canvasBase);
                    canvasFinal.Clear(SKColors.Transparent);

                    _logger.LogInformation("Canvas vazio criado: {W}x{H}", larguraDaMoldura, alturaDaMoldura);

                    // PASSO 6: Plota transformação no canvas
                    canvasFinal.DrawBitmap(quadranteDistorcido, coordPlotSkewX, coordPlotSkewY);
                    _logger.LogInformation("Transformação plotada em ({X}, {Y})", coordPlotSkewX, coordPlotSkewY);

                    // PASSO 7: Carrega moldura Sala1.webp como overlay final
                    var caminhoSala = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Salas", "Sala1.webp");
                    if (File.Exists(caminhoSala))
                    {
                        using var sala = SKBitmap.Decode(caminhoSala);
                        _logger.LogInformation("Sala1.webp carregada: {W}x{H}", sala.Width, sala.Height);

                        // Desenha moldura POR CIMA em (0, 0)
                        canvasFinal.DrawBitmap(sala, 0, 0);
                        _logger.LogInformation("Sala1.webp desenhada como overlay final");
                    }
                    else
                    {
                        _logger.LogWarning("Sala1.webp não encontrada: {Path}. Canvas sem moldura.", caminhoSala);
                    }

                    // ✅ IMPERATIVO: Adiciona marca d'água (canto inferior direito)
                    _watermark.AddWatermark(canvasFinal, canvasBase.Width, canvasBase.Height);
                    _logger.LogInformation("Marca d'água adicionada ao quadrante {Q}", quadrante);

                    // Adiciona quadrante à lista (NÃO usa 'using' aqui pois será retornado)
                    quadrantes.Add(canvasBase);
                    _logger.LogInformation("Quadrante {Q} concluído!", quadrante);
                }

                _logger.LogInformation("=== LIVING ROOM #1 CONCLUÍDO: {Count} quadrantes ===", quadrantes.Count);
                return quadrantes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Living Room #1");
                throw;
            }
        }

        /// <summary>
        /// Gera mockup Living Room #2 (Sub Sala2 do VB.NET) - Liveroom #3
        /// </summary>
        /// <param name="imagemCropada">Imagem cropada pelo usuário</param>
        /// <returns>Lista com 4 quadrantes gerados</returns>
        public List<SKBitmap> GerarLivingRoom2(SKBitmap imagemCropada)
        {
            try
            {
                // Parâmetros do VB.NET Sub Sala2() - Liveroom #3
                const int larguraDaMoldura = 1080;
                const int alturaDaMoldura = 1334;
                const int larguraDoQuadroSemSkew = 316;
                const int alturaDoQuadroSemSkew = 979;
                const int coordPlotSkewX = 456;
                const int coordPlotSkewY = 2;
                const int ladoMaior = 979;
                const int ladoMenor = 830;
                const int fatorInclinacao = 161;
                const int tamanhoDoQuadro = 700; // Largura do tile

                _logger.LogInformation("=== INICIANDO LIVING ROOM #2 (Liveroom #3) ===");
                _logger.LogInformation("Imagem original: {W}x{H}", imagemCropada.Width, imagemCropada.Height);

                // PASSO 1: Redimensiona para largura fixa
                float fatorDeAjuste = (float)imagemCropada.Width / tamanhoDoQuadro;
                int novaAltura = (int)(imagemCropada.Height / fatorDeAjuste);

                using var imagemRedimensionada = imagemCropada.Resize(
                    new SKImageInfo(tamanhoDoQuadro, novaAltura),
                    SKFilterQuality.High);

                _logger.LogInformation("Imagem redimensionada: {W}x{H}", tamanhoDoQuadro, novaAltura);

                // PASSO 2: Cria as 4 versões do BookMatch (ORI, FV, FH, 180)
                using var bitmapORI = new SKBitmap(imagemRedimensionada.Width, imagemRedimensionada.Height);
                imagemRedimensionada.CopyTo(bitmapORI);

                using var bitmapFV = FlipVertical(imagemRedimensionada);
                using var bitmapFH = FlipHorizontal(imagemRedimensionada);
                using var bitmap180 = Rotate180(imagemRedimensionada);

                _logger.LogInformation("BookMatch criado: ORI={W}x{H}, FV={W}x{H}, FH={W}x{H}, 180={W}x{H}",
                    bitmapORI.Width, bitmapORI.Height,
                    bitmapFV.Width, bitmapFV.Height,
                    bitmapFH.Width, bitmapFH.Height,
                    bitmap180.Width, bitmap180.Height);

                // PASSO 3: Monta os 4 mosaicos 2x2 (quadrantes)
                var larguraMosaico = tamanhoDoQuadro * 2;
                var alturaMosaico = novaAltura * 2;

                var quadrantes = new List<SKBitmap>();

                // Gera os 4 quadrantes
                for (int quadrante = 1; quadrante <= 4; quadrante++)
                {
                    _logger.LogInformation("--- Gerando quadrante {Q}/4 ---", quadrante);

                    // Cria mosaico 2x2
                    using var mosaico = new SKBitmap(larguraMosaico, alturaMosaico);
                    using var canvasMosaico = new SKCanvas(mosaico);
                    canvasMosaico.Clear(SKColors.White);

                    // Desenha o mosaico 2x2 conforme o quadrante
                    switch (quadrante)
                    {
                        case 1: // Quadrante 1: ORI, FH / FV, 180
                            canvasMosaico.DrawBitmap(bitmapORI, 0, 0);
                            canvasMosaico.DrawBitmap(bitmapFH, tamanhoDoQuadro, 0);
                            canvasMosaico.DrawBitmap(bitmapFV, 0, novaAltura);
                            canvasMosaico.DrawBitmap(bitmap180, tamanhoDoQuadro, novaAltura);
                            break;
                        case 2: // Quadrante 2: FH, ORI / 180, FV
                            canvasMosaico.DrawBitmap(bitmapFH, 0, 0);
                            canvasMosaico.DrawBitmap(bitmapORI, tamanhoDoQuadro, 0);
                            canvasMosaico.DrawBitmap(bitmap180, 0, novaAltura);
                            canvasMosaico.DrawBitmap(bitmapFV, tamanhoDoQuadro, novaAltura);
                            break;
                        case 3: // Quadrante 3: FV, 180 / ORI, FH
                            canvasMosaico.DrawBitmap(bitmapFV, 0, 0);
                            canvasMosaico.DrawBitmap(bitmap180, tamanhoDoQuadro, 0);
                            canvasMosaico.DrawBitmap(bitmapORI, 0, novaAltura);
                            canvasMosaico.DrawBitmap(bitmapFH, tamanhoDoQuadro, novaAltura);
                            break;
                        case 4: // Quadrante 4: 180, FV / FH, ORI
                            canvasMosaico.DrawBitmap(bitmap180, 0, 0);
                            canvasMosaico.DrawBitmap(bitmapFV, tamanhoDoQuadro, 0);
                            canvasMosaico.DrawBitmap(bitmapFH, 0, novaAltura);
                            canvasMosaico.DrawBitmap(bitmapORI, tamanhoDoQuadro, novaAltura);
                            break;
                    }

                    _logger.LogInformation("Mosaico {Q} montado: {W}x{H}", quadrante, larguraMosaico, alturaMosaico);

                    // PASSO 4: Aplica DistortionInclina (compressão vertical trapézio + skew)
                    // Este método JÁ faz: resize para larguraDoQuadroSemSkew x alturaDoQuadroSemSkew + aplica transformação skew de 4 pontos
                    using var quadranteDistorcido = _transformService.DistortionInclina(
                        mosaico,
                        ladoMaior,
                        ladoMenor,
                        larguraDoQuadroSemSkew,
                        alturaDoQuadroSemSkew,
                        fatorInclinacao);

                    _logger.LogInformation("Quadrante {Q} distorcido: {W}x{H}", quadrante, quadranteDistorcido.Width, quadranteDistorcido.Height);

                    // PASSO 5: Cria canvas final (moldura 1080x1334)
                    var canvasFinal = new SKBitmap(larguraDaMoldura, alturaDaMoldura);
                    using var canvasCanvas = new SKCanvas(canvasFinal);
                    canvasCanvas.Clear(SKColors.Transparent);

                    // PASSO 6: Plota o quadro distorcido no canvas nas coordenadas especificadas
                    canvasCanvas.DrawBitmap(quadranteDistorcido, coordPlotSkewX, coordPlotSkewY);

                    _logger.LogInformation("Quadrante {Q} finalizado: {W}x{H}, plotado em ({X},{Y})",
                        quadrante, canvasFinal.Width, canvasFinal.Height, coordPlotSkewX, coordPlotSkewY);

                    // PASSO 7: Aplica overlay (camada final da sala)
                    var caminhoOverlay = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Salas", "Sala2.webp");
                    if (File.Exists(caminhoOverlay))
                    {
                        using var overlayBitmap = SKBitmap.Decode(caminhoOverlay);
                        if (overlayBitmap != null)
                        {
                            canvasCanvas.DrawBitmap(overlayBitmap, 0, 0);
                            _logger.LogInformation("Overlay aplicado no quadrante {Q}", quadrante);
                        }
                    }

                    // ✅ IMPERATIVO: Adiciona marca d'água (canto inferior direito)
                    _watermark.AddWatermark(canvasCanvas, canvasFinal.Width, canvasFinal.Height);
                    _logger.LogInformation("Marca d'água adicionada ao quadrante {Q}", quadrante);

                    quadrantes.Add(canvasFinal);
                }

                _logger.LogInformation("=== LIVING ROOM #2 CONCLUÍDO: {Count} quadrantes ===", quadrantes.Count);
                return quadrantes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Living Room #2");
                throw;
            }
        }

        /// <summary>
        /// Cria bitmap rotacionado conforme orientação EXIF
        /// </summary>
        private SKBitmap CriarBitmapRotacionado(SKBitmap source, SKEncodedOrigin orientation)
        {
            var rotated = new SKBitmap(source.Height, source.Width); // Inverte dimensões para rotação 90/270
            using var canvas = new SKCanvas(rotated);

            switch (orientation)
            {
                case SKEncodedOrigin.LeftBottom: // 90° anti-horário
                    canvas.Translate(0, source.Width);
                    canvas.RotateDegrees(-90);
                    break;
                case SKEncodedOrigin.RightTop: // 90° horário (270° anti-horário)
                    canvas.Translate(source.Height, 0);
                    canvas.RotateDegrees(90);
                    break;
            }

            canvas.DrawBitmap(source, 0, 0);
            return rotated;
        }

        /// <summary>
        /// Faz flip horizontal (espelha) de um bitmap
        /// </summary>
        private SKBitmap FlipHorizontal(SKBitmap source)
        {
            var flipped = new SKBitmap(source.Width, source.Height);
            using var canvas = new SKCanvas(flipped);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };

            // Aplica transformação de espelhamento horizontal
            canvas.Scale(-1, 1, source.Width / 2f, source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0, paint);

            return flipped;
        }

        /// <summary>
        /// Faz flip vertical de um bitmap
        /// </summary>
        private SKBitmap FlipVertical(SKBitmap source)
        {
            var flipped = new SKBitmap(source.Width, source.Height);
            using var canvas = new SKCanvas(flipped);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };

            // Aplica transformação de espelhamento vertical
            canvas.Scale(1, -1, source.Width / 2f, source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0, paint);

            return flipped;
        }

        /// <summary>
        /// Rotaciona 180 graus
        /// </summary>
        private SKBitmap Rotate180(SKBitmap source)
        {
            var rotated = new SKBitmap(source.Width, source.Height);
            using var canvas = new SKCanvas(rotated);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };

            // Rotaciona 180° ao redor do centro
            canvas.RotateDegrees(180, source.Width / 2f, source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0, paint);

            return rotated;
        }
    }
}
