using SkiaSharp;
using PicStoneFotoAPI.Helpers;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço para geração de mockups de Floor (Piso)
    /// Tradução do VB.NET Sub Piso1() para C# usando SkiaSharp
    /// </summary>
    public class FloorService
    {
        private readonly GraphicsTransformService _transformService;
        private readonly ILogger<FloorService> _logger;
        private readonly ImageWatermarkService _watermark;
        private readonly ImageManipulationService _imageManipulation;

        public FloorService(
            GraphicsTransformService transformService,
            ILogger<FloorService> logger,
            ImageWatermarkService watermark,
            ImageManipulationService imageManipulation)
        {
            _transformService = transformService;
            _logger = logger;
            _watermark = watermark;
            _imageManipulation = imageManipulation;
        }

        /// <summary>
        /// Gera mockup Floor #1 (Sub Piso1 do VB.NET)
        /// </summary>
        /// <param name="imagemCropada">Imagem cropada pelo usuário</param>
        /// <returns>Lista com 4 versões geradas</returns>
        public List<SKBitmap> GerarFloor1(SKBitmap imagemCropada)
        {
            try
            {
                // Parâmetros do VB.NET Sub Piso1()
                const int tamanhoMaxQuadro = 1000;
                const int larguraMolduraVirtual = 1997;
                const int alturaMolduraVirtual = 1300;
                const int coordPlotX = -996;
                const int coordPlotY = 746;

                // Parâmetros DistortionInclina
                const int ladoMaior = 4390;
                const int ladoMenor = 1415;
                const int novaLarguraDistorcao = 878;
                const int novaAlturaDistorcao = 4390;
                const int fatorInclinacao = 1545;

                _logger.LogInformation("=== INICIANDO FLOOR #1 ===");
                _logger.LogInformation("Imagem original: {W}x{H}", imagemCropada.Width, imagemCropada.Height);

                // PASSO 1: Calcula tamanho do quadro (max 1000px)
                int tamanhoDoQuadro = imagemCropada.Width > tamanhoMaxQuadro ? tamanhoMaxQuadro : imagemCropada.Width;
                float fatorDeAjuste = (float)imagemCropada.Width / tamanhoDoQuadro;
                int novaAltura = (int)(imagemCropada.Height / fatorDeAjuste);

                _logger.LogInformation("Tamanho do quadro: {T}, Fator: {F}, Nova altura: {H}",
                    tamanhoDoQuadro, fatorDeAjuste, novaAltura);

                // Copia a imagem base para manipulação (será transformada a cada processo)
                var imagemBase = imagemCropada.Copy();

                var resultados = new List<SKBitmap>();

                // Gera os 4 processos (variações)
                for (int contaProcesso = 1; contaProcesso <= 4; contaProcesso++)
                {
                    _logger.LogInformation("--- Gerando processo {P}/4 ---", contaProcesso);

                    // PASSO 2: Aplica transformação na imagem base conforme o processo
                    // NOTA: As transformações são ACUMULATIVAS no VB.NET original
                    if (contaProcesso == 2)
                    {
                        // Rotate180FlipNone
                        imagemBase = _imageManipulation.Rotate180(imagemBase);
                        _logger.LogInformation("Processo 2: Aplicado Rotate180");
                    }
                    else if (contaProcesso == 3)
                    {
                        // Rotate180FlipX (sobre o resultado do processo 2)
                        // Rotate180 + FlipX = FlipY (espelho vertical)
                        imagemBase = _imageManipulation.FlipVertical(imagemBase);
                        _logger.LogInformation("Processo 3: Aplicado FlipVertical (Rotate180FlipX acumulado)");
                    }
                    else if (contaProcesso == 4)
                    {
                        // Rotate180FlipNone novamente (sobre o resultado do processo 3)
                        imagemBase = _imageManipulation.Rotate180(imagemBase);
                        _logger.LogInformation("Processo 4: Aplicado Rotate180 (acumulado)");
                    }

                    // PASSO 3: Redimensiona para o tamanho do quadro
                    using var imagemRedimensionada = imagemBase.Resize(
                        new SKImageInfo(tamanhoDoQuadro, novaAltura),
                        SKBitmapHelper.HighQuality);

                    _logger.LogInformation("Imagem redimensionada: {W}x{H}", tamanhoDoQuadro, novaAltura);

                    // PASSO 4: Cria as 4 versões para o mosaico
                    using var bitmapORI = new SKBitmap(imagemRedimensionada.Width, imagemRedimensionada.Height);
                    imagemRedimensionada.CopyTo(bitmapORI);

                    using var bitmapFLH = _imageManipulation.FlipHorizontal(imagemRedimensionada); // RotateNoneFlipX
                    using var bitmap180 = _imageManipulation.FlipVertical(imagemRedimensionada);   // RotateNoneFlipY
                    using var bitmapFLV = _imageManipulation.Rotate180(imagemRedimensionada);      // Rotate180FlipNone

                    _logger.LogInformation("BookMatch criado: ORI, FLH, 180, FLV");

                    // PASSO 5: Cria mosaico 4x4
                    int larguraMosaico = tamanhoDoQuadro * 4;
                    int alturaMosaico = novaAltura * 4;

                    using var mosaico = new SKBitmap(larguraMosaico, alturaMosaico);
                    using var canvasMosaico = new SKCanvas(mosaico);
                    canvasMosaico.Clear(SKColors.White);

                    _logger.LogInformation("Mosaico 4x4: {W}x{H}", larguraMosaico, alturaMosaico);

                    // Padrão do mosaico 4x4:
                    // Linha 0: ORI, FLH, ORI, FLH
                    // Linha 1: 180, FLV, 180, FLV
                    // Linha 2: ORI, FLH, ORI, FLH
                    // Linha 3: 180, FLV, 180, FLV

                    // Linha 0
                    canvasMosaico.DrawBitmap(bitmapORI, 0, 0);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro, 0);
                    canvasMosaico.DrawBitmap(bitmapORI, tamanhoDoQuadro * 2, 0);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro * 3, 0);

                    // Linha 1
                    canvasMosaico.DrawBitmap(bitmap180, 0, novaAltura);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro, novaAltura);
                    canvasMosaico.DrawBitmap(bitmap180, tamanhoDoQuadro * 2, novaAltura);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro * 3, novaAltura);

                    // Linha 2
                    canvasMosaico.DrawBitmap(bitmapORI, 0, novaAltura * 2);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro, novaAltura * 2);
                    canvasMosaico.DrawBitmap(bitmapORI, tamanhoDoQuadro * 2, novaAltura * 2);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro * 3, novaAltura * 2);

                    // Linha 3
                    canvasMosaico.DrawBitmap(bitmap180, 0, novaAltura * 3);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro, novaAltura * 3);
                    canvasMosaico.DrawBitmap(bitmap180, tamanhoDoQuadro * 2, novaAltura * 3);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro * 3, novaAltura * 3);

                    _logger.LogInformation("Mosaico 4x4 montado");

                    // PASSO 6: Aplica transformações de perspectiva
                    // Rotate90 -> DistortionInclina -> Rotate90
                    using var mosaicoRotado1 = RotateBitmap90Clockwise(mosaico);
                    _logger.LogInformation("Mosaico rotacionado 90° (1): {W}x{H}", mosaicoRotado1.Width, mosaicoRotado1.Height);

                    using var mosaicoDistorcido = _transformService.DistortionInclina(
                        mosaicoRotado1,
                        ladoMaior,
                        ladoMenor,
                        novaLarguraDistorcao,
                        novaAlturaDistorcao,
                        fatorInclinacao);

                    _logger.LogInformation("Mosaico distorcido: {W}x{H}", mosaicoDistorcido.Width, mosaicoDistorcido.Height);

                    using var mosaicoRotado2 = RotateBitmap90Clockwise(mosaicoDistorcido);
                    _logger.LogInformation("Mosaico rotacionado 90° (2): {W}x{H}", mosaicoRotado2.Width, mosaicoRotado2.Height);

                    // PASSO 7: Cria canvas final e plota o mosaico transformado
                    var canvasFinal = new SKBitmap(larguraMolduraVirtual, alturaMolduraVirtual);
                    using var canvas = new SKCanvas(canvasFinal);
                    canvas.Clear(SKColors.White);

                    // Plota na posição (-996, 746) - crop posicional
                    canvas.DrawBitmap(mosaicoRotado2, coordPlotX, coordPlotY);
                    _logger.LogInformation("Mosaico plotado em ({X}, {Y})", coordPlotX, coordPlotY);

                    // PASSO 8: Aplica overlay (moldura do piso)
                    var caminhoOverlay = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Pisos", "Piso1.webp");
                    if (File.Exists(caminhoOverlay))
                    {
                        using var overlayBitmap = SKBitmap.Decode(caminhoOverlay);
                        if (overlayBitmap != null)
                        {
                            canvas.DrawBitmap(overlayBitmap, 0, 0);
                            _logger.LogInformation("Overlay Piso1.webp aplicado");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Overlay não encontrado: {Path}", caminhoOverlay);
                    }

                    // PASSO 9: Adiciona marca d'água
                    _watermark.AddWatermark(canvas, canvasFinal.Width, canvasFinal.Height);
                    _logger.LogInformation("Marca d'água adicionada ao processo {P}", contaProcesso);

                    resultados.Add(canvasFinal);
                    _logger.LogInformation("Processo {P} concluído!", contaProcesso);
                }

                // Limpa a imagem base
                imagemBase.Dispose();

                _logger.LogInformation("=== FLOOR #1 CONCLUÍDO: {Count} versões ===", resultados.Count);
                return resultados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Floor #1");
                throw;
            }
        }

        /// <summary>
        /// Gera mockup Floor #2 - Mosaico 4x4 com perspectiva diferente
        /// Baseado no VB.NET Sub Piso2()
        /// </summary>
        public List<SKBitmap> GerarFloor2(SKBitmap imagemCropada)
        {
            _logger.LogInformation("=== INICIANDO FLOOR #2 ===");
            var resultados = new List<SKBitmap>();

            try
            {
                // Parâmetros do VB.NET - Floor #2 (diferentes do Floor #1)
                const int tamanhoMaxQuadro = 1000;
                const int larguraMolduraVirtual = 1980;
                const int alturaMolduraVirtual = 1200;
                const int coordPlotX = -1767;
                const int coordPlotY = 335;

                // Parâmetros DistortionInclina para Floor #2
                const int ladoMaior = 5900;
                const int ladoMenor = 1468;
                const int novaLarguraDistorcao = 1060;
                const int novaAlturaDistorcao = 5900;
                const int fatorInclinacao = 2350;

                // PASSO 1: Redimensiona mantendo proporção (max 1000px largura)
                int tamanhoDoQuadro = Math.Min(imagemCropada.Width, tamanhoMaxQuadro);
                float fatorAjuste = (float)imagemCropada.Width / tamanhoDoQuadro;
                int novaAltura = (int)(imagemCropada.Height / fatorAjuste);

                _logger.LogInformation("Redimensionando: {W}x{H} -> {NW}x{NH}",
                    imagemCropada.Width, imagemCropada.Height, tamanhoDoQuadro, novaAltura);

                using var imagemRedimensionada = imagemCropada.Resize(
                    new SKImageInfo(tamanhoDoQuadro, novaAltura),
                    SKBitmapHelper.HighQuality);

                // Cria cópia para acumular transformações
                var imagemBase = imagemRedimensionada.Copy();

                // Gera 4 versões (ContaProcesso = 1 a 4)
                for (int contaProcesso = 1; contaProcesso <= 4; contaProcesso++)
                {
                    _logger.LogInformation("--- Processando versão {P} ---", contaProcesso);

                    // Aplica transformação acumulativa na imagemBase
                    if (contaProcesso == 2)
                    {
                        // Rotate180FlipNone
                        imagemBase = _imageManipulation.Rotate180(imagemBase);
                        _logger.LogInformation("Processo 2: Aplicado Rotate180");
                    }
                    else if (contaProcesso == 3)
                    {
                        // Rotate180FlipX = FlipVertical
                        imagemBase = _imageManipulation.FlipVertical(imagemBase);
                        _logger.LogInformation("Processo 3: Aplicado FlipVertical");
                    }
                    else if (contaProcesso == 4)
                    {
                        // Rotate180FlipNone
                        imagemBase = _imageManipulation.Rotate180(imagemBase);
                        _logger.LogInformation("Processo 4: Aplicado Rotate180");
                    }

                    // Redimensiona para o tamanho do quadro
                    using var imagem = imagemBase.Resize(
                        new SKImageInfo(tamanhoDoQuadro, novaAltura),
                        SKBitmapHelper.HighQuality);

                    // PASSO 2: Cria as 4 versões do BookMatch
                    using var bitmapORI = imagem.Copy();
                    using var bitmapFLH = _imageManipulation.FlipHorizontal(imagem);
                    using var bitmap180 = _imageManipulation.FlipVertical(imagem);
                    using var bitmapFLV = _imageManipulation.Rotate180(imagem);

                    // PASSO 3: Monta o mosaico 4x4 (mesmo padrão do Floor #1)
                    int mosaicoLargura = tamanhoDoQuadro * 4;
                    int mosaicoAltura = novaAltura * 4;

                    using var mosaico = new SKBitmap(mosaicoLargura, mosaicoAltura);
                    using var canvasMosaico = new SKCanvas(mosaico);
                    canvasMosaico.Clear(SKColors.White);

                    // Linha 0: ORI, FLH, ORI, FLH
                    canvasMosaico.DrawBitmap(bitmapORI, 0, 0);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro, 0);
                    canvasMosaico.DrawBitmap(bitmapORI, tamanhoDoQuadro * 2, 0);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro * 3, 0);

                    // Linha 1: 180, FLV, 180, FLV
                    canvasMosaico.DrawBitmap(bitmap180, 0, novaAltura);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro, novaAltura);
                    canvasMosaico.DrawBitmap(bitmap180, tamanhoDoQuadro * 2, novaAltura);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro * 3, novaAltura);

                    // Linha 2: ORI, FLH, ORI, FLH
                    canvasMosaico.DrawBitmap(bitmapORI, 0, novaAltura * 2);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro, novaAltura * 2);
                    canvasMosaico.DrawBitmap(bitmapORI, tamanhoDoQuadro * 2, novaAltura * 2);
                    canvasMosaico.DrawBitmap(bitmapFLH, tamanhoDoQuadro * 3, novaAltura * 2);

                    // Linha 3: 180, FLV, 180, FLV
                    canvasMosaico.DrawBitmap(bitmap180, 0, novaAltura * 3);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro, novaAltura * 3);
                    canvasMosaico.DrawBitmap(bitmap180, tamanhoDoQuadro * 2, novaAltura * 3);
                    canvasMosaico.DrawBitmap(bitmapFLV, tamanhoDoQuadro * 3, novaAltura * 3);

                    _logger.LogInformation("Mosaico 4x4 criado: {W}x{H}", mosaico.Width, mosaico.Height);

                    // PASSO 4: Rotaciona 90° (Rotate90FlipNone)
                    using var mosaicoRotado1 = RotateBitmap90Clockwise(mosaico);
                    _logger.LogInformation("Mosaico rotacionado 90° (1): {W}x{H}", mosaicoRotado1.Width, mosaicoRotado1.Height);

                    // PASSO 5: Aplica DistortionInclina com parâmetros do Floor #2
                    using var mosaicoDistorcido = _transformService.DistortionInclina(
                        mosaicoRotado1, ladoMaior, ladoMenor, novaLarguraDistorcao, novaAlturaDistorcao, fatorInclinacao);
                    _logger.LogInformation("Distorção aplicada: {W}x{H}", mosaicoDistorcido.Width, mosaicoDistorcido.Height);

                    // PASSO 6: Rotaciona 90° novamente
                    using var mosaicoRotado2 = RotateBitmap90Clockwise(mosaicoDistorcido);
                    _logger.LogInformation("Mosaico rotacionado 90° (2): {W}x{H}", mosaicoRotado2.Width, mosaicoRotado2.Height);

                    // PASSO 7: Cria canvas final e plota o mosaico transformado
                    var canvasFinal = new SKBitmap(larguraMolduraVirtual, alturaMolduraVirtual);
                    using var canvas = new SKCanvas(canvasFinal);
                    canvas.Clear(SKColors.White);

                    // Plota na posição (-1767, 335)
                    canvas.DrawBitmap(mosaicoRotado2, coordPlotX, coordPlotY);
                    _logger.LogInformation("Mosaico plotado em ({X}, {Y})", coordPlotX, coordPlotY);

                    // PASSO 8: Aplica overlay (moldura do piso)
                    var caminhoOverlay = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Pisos", "Piso2.webp");
                    if (File.Exists(caminhoOverlay))
                    {
                        using var overlayBitmap = SKBitmap.Decode(caminhoOverlay);
                        if (overlayBitmap != null)
                        {
                            canvas.DrawBitmap(overlayBitmap, 0, 0);
                            _logger.LogInformation("Overlay Piso2.webp aplicado");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Overlay não encontrado: {Path}", caminhoOverlay);
                    }

                    // PASSO 9: Adiciona marca d'água
                    _watermark.AddWatermark(canvas, canvasFinal.Width, canvasFinal.Height);
                    _logger.LogInformation("Marca d'água adicionada ao processo {P}", contaProcesso);

                    resultados.Add(canvasFinal);
                    _logger.LogInformation("Processo {P} concluído!", contaProcesso);
                }

                // Limpa a imagem base
                imagemBase.Dispose();

                _logger.LogInformation("=== FLOOR #2 CONCLUÍDO: {Count} versões ===", resultados.Count);
                return resultados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Floor #2");
                throw;
            }
        }

        /// <summary>
        /// Gera mockup Floor #3 - Corredor com 6 filas em perspectiva
        /// Baseado no VB.NET Sub Piso3()
        /// </summary>
        public List<SKBitmap> GerarFloor3(SKBitmap imagemCropada)
        {
            _logger.LogInformation("=== INICIANDO FLOOR #3 ===");
            var resultados = new List<SKBitmap>();

            try
            {
                // Parâmetros do VB.NET - Floor #3
                const int tamanhoMaxQuadro = 1000;
                const int larguraMolduraVirtual = 1200;
                const int alturaMolduraVirtual = 1200;

                // PASSO 1: Redimensiona mantendo proporção (max 1000px largura)
                int tamanhoDoQuadro = Math.Min(imagemCropada.Width, tamanhoMaxQuadro);
                float fatorAjuste = (float)imagemCropada.Width / tamanhoDoQuadro;
                int novaAltura = (int)(imagemCropada.Height / fatorAjuste);

                _logger.LogInformation("Redimensionando: {W}x{H} -> {NW}x{NH}",
                    imagemCropada.Width, imagemCropada.Height, tamanhoDoQuadro, novaAltura);

                using var imagemRedimensionada = imagemCropada.Resize(
                    new SKImageInfo(tamanhoDoQuadro, novaAltura),
                    SKBitmapHelper.HighQuality);

                // Cria cópia para acumular transformações
                var imagemBase = imagemRedimensionada.Copy();

                // Gera 4 versões (ContaProcesso = 1 a 4)
                for (int contaProcesso = 1; contaProcesso <= 4; contaProcesso++)
                {
                    _logger.LogInformation("--- Processando versão {P} ---", contaProcesso);

                    // Aplica transformação acumulativa na imagemBase
                    if (contaProcesso == 2)
                    {
                        imagemBase = _imageManipulation.Rotate180(imagemBase);
                        _logger.LogInformation("Processo 2: Aplicado Rotate180");
                    }
                    else if (contaProcesso == 3)
                    {
                        imagemBase = _imageManipulation.FlipVertical(imagemBase);
                        _logger.LogInformation("Processo 3: Aplicado FlipVertical");
                    }
                    else if (contaProcesso == 4)
                    {
                        imagemBase = _imageManipulation.Rotate180(imagemBase);
                        _logger.LogInformation("Processo 4: Aplicado Rotate180");
                    }

                    // Redimensiona para o tamanho do quadro
                    using var imagem = imagemBase.Resize(
                        new SKImageInfo(tamanhoDoQuadro, novaAltura),
                        SKBitmapHelper.HighQuality);

                    // PASSO 2: Cria as 4 versões com rotações específicas do Floor #3
                    // bitmapORI: Rotate90FlipNone
                    // bitmapFLH: Rotate90FlipX
                    // bitmap180: Rotate270FlipX
                    // bitmapFLV: Rotate270FlipNone
                    using var bitmapORI = RotateBitmap90Clockwise(imagem);
                    using var bitmapFLH = RotateBitmap90ClockwiseFlipX(imagem);
                    using var bitmap180 = RotateBitmap270ClockwiseFlipX(imagem);
                    using var bitmapFLV = RotateBitmap270Clockwise(imagem);

                    _logger.LogInformation("BookMatch Floor3 criado: ORI(90), FLH(90X), 180(270X), FLV(270)");

                    // PASSO 3: Cria as 6 filas com espaçamento (rejunte)
                    const int espacamentoRejunte = 2;
                    int filaLargura = (novaAltura * 4) + (espacamentoRejunte * 3);
                    int filaAltura = tamanhoDoQuadro;

                    // Fila 1 e 3 e 5: ORI, FLH, ORI, FLH
                    using var fila1 = CriarFilaFloor3(bitmapORI, bitmapFLH, novaAltura, tamanhoDoQuadro, espacamentoRejunte);
                    using var fila3 = fila1.Copy();
                    using var fila5 = fila1.Copy();

                    // Fila 2 e 4 e 6: 180, FLV, 180, FLV
                    using var fila2 = CriarFilaFloor3(bitmap180, bitmapFLV, novaAltura, tamanhoDoQuadro, espacamentoRejunte);
                    using var fila4 = fila2.Copy();
                    using var fila6 = fila2.Copy();

                    _logger.LogInformation("6 filas criadas com rejunte: {W}x{H}, espaçamento: {E}px", filaLargura, filaAltura, espacamentoRejunte);

                    // PASSO 4: Aplica transformações em cada fila (DistortionInclina + Skew2)
                    // Fila1: DistortionInclina(2200, 1017, 350, 2200, 0) + Skew2(0, 562)
                    using var fila1Rot = RotateBitmap270Clockwise(fila1);
                    using var fila1Dist = _transformService.DistortionInclina(fila1Rot, 2200, 1017, 350, 2200, 0);
                    using var fila1Skew = _transformService.Skew2(fila1Dist, 0, 562);
                    using var fila1Final = RotateBitmap90ClockwiseFlipX(fila1Skew);

                    // Fila2: DistortionInclina(1017, 662, 104, 1017, 0) + Skew2(0, 168)
                    using var fila2Rot = RotateBitmap270Clockwise(fila2);
                    using var fila2Dist = _transformService.DistortionInclina(fila2Rot, 1017, 662, 104, 1017, 0);
                    using var fila2Skew = _transformService.Skew2(fila2Dist, 0, 168);
                    using var fila2Final = RotateBitmap90ClockwiseFlipX(fila2Skew);

                    // Fila3: DistortionInclina(662, 494, 50, 662, 0) + Skew2(0, 79)
                    using var fila3Rot = RotateBitmap270Clockwise(fila3);
                    using var fila3Dist = _transformService.DistortionInclina(fila3Rot, 662, 494, 50, 662, 0);
                    using var fila3Skew = _transformService.Skew2(fila3Dist, 0, 79);
                    using var fila3Final = RotateBitmap90ClockwiseFlipX(fila3Skew);

                    // Fila4: DistortionInclina(488, 390, 25, 488, 0) + Skew2(0, 47)
                    using var fila4Rot = RotateBitmap270Clockwise(fila4);
                    using var fila4Dist = _transformService.DistortionInclina(fila4Rot, 488, 390, 25, 488, 0);
                    using var fila4Skew = _transformService.Skew2(fila4Dist, 0, 47);
                    using var fila4Final = RotateBitmap90ClockwiseFlipX(fila4Skew);

                    // Fila5: DistortionInclina(387, 324, 20, 387, 0) + Skew2(0, 29)
                    using var fila5Rot = RotateBitmap270Clockwise(fila5);
                    using var fila5Dist = _transformService.DistortionInclina(fila5Rot, 387, 324, 20, 387, 0);
                    using var fila5Skew = _transformService.Skew2(fila5Dist, 0, 29);
                    using var fila5Final = RotateBitmap90ClockwiseFlipX(fila5Skew);

                    // Fila6: DistortionInclina(324, 279, 14, 324, 0) + Skew2(0, 20)
                    using var fila6Rot = RotateBitmap270Clockwise(fila6);
                    using var fila6Dist = _transformService.DistortionInclina(fila6Rot, 324, 279, 14, 324, 0);
                    using var fila6Skew = _transformService.Skew2(fila6Dist, 0, 20);
                    using var fila6Final = RotateBitmap90ClockwiseFlipX(fila6Skew);

                    _logger.LogInformation("Filas transformadas com perspectiva");

                    // PASSO 5: Cria canvas final e plota as filas nas posições
                    var canvasFinal = new SKBitmap(larguraMolduraVirtual, alturaMolduraVirtual);
                    using var canvas = new SKCanvas(canvasFinal);
                    canvas.Clear(SKColors.White);

                    // Posições do VB.NET:
                    // Fila1: (-475, 850)
                    // Fila2: (87, 747)
                    // Fila3: (256, 698)
                    // Fila4: (338, 674)
                    // Fila5: (386, 655)
                    // Fila6: (416, 641)
                    canvas.DrawBitmap(fila1Final, -475, 850);
                    canvas.DrawBitmap(fila2Final, 87, 747);
                    canvas.DrawBitmap(fila3Final, 256, 698);
                    canvas.DrawBitmap(fila4Final, 338, 674);
                    canvas.DrawBitmap(fila5Final, 386, 655);
                    canvas.DrawBitmap(fila6Final, 416, 641);

                    _logger.LogInformation("Filas plotadas no canvas");

                    // PASSO 6: Aplica overlay
                    var caminhoOverlay = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Pisos", "Piso3.webp");
                    if (File.Exists(caminhoOverlay))
                    {
                        using var overlayBitmap = SKBitmap.Decode(caminhoOverlay);
                        if (overlayBitmap != null)
                        {
                            canvas.DrawBitmap(overlayBitmap, 0, 0);
                            _logger.LogInformation("Overlay Piso3.webp aplicado");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Overlay não encontrado: {Path}", caminhoOverlay);
                    }

                    // PASSO 7: Adiciona marca d'água
                    _watermark.AddWatermark(canvas, canvasFinal.Width, canvasFinal.Height);
                    _logger.LogInformation("Marca d'água adicionada ao processo {P}", contaProcesso);

                    resultados.Add(canvasFinal);
                    _logger.LogInformation("Processo {P} concluído!", contaProcesso);
                }

                // Limpa a imagem base
                imagemBase.Dispose();

                _logger.LogInformation("=== FLOOR #3 CONCLUÍDO: {Count} versões ===", resultados.Count);
                return resultados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Floor #3");
                throw;
            }
        }

        /// <summary>
        /// Cria uma fila para Floor #3 com padrão: img1, img2, img1, img2
        /// Inclui espaçamento (rejunte) entre os quadros
        /// </summary>
        private SKBitmap CriarFilaFloor3(SKBitmap img1, SKBitmap img2, int novaAltura, int tamanhoDoQuadro, int espacamento = 2)
        {
            // Largura total: 4 quadros + 3 espaços entre eles
            int filaLargura = (novaAltura * 4) + (espacamento * 3);
            int filaAltura = tamanhoDoQuadro;

            var fila = new SKBitmap(filaLargura, filaAltura);
            using var canvas = new SKCanvas(fila);

            // Fundo cinza claro para o rejunte
            canvas.Clear(new SKColor(180, 180, 180));

            // Posiciona os quadros com espaçamento
            int pos0 = 0;
            int pos1 = novaAltura + espacamento;
            int pos2 = (novaAltura * 2) + (espacamento * 2);
            int pos3 = (novaAltura * 3) + (espacamento * 3);

            canvas.DrawBitmap(img1, pos0, 0);
            canvas.DrawBitmap(img2, pos1, 0);
            canvas.DrawBitmap(img1, pos2, 0);
            canvas.DrawBitmap(img2, pos3, 0);

            return fila;
        }

        /// <summary>
        /// Rotaciona bitmap 90° no sentido horário (Rotate90FlipNone)
        /// </summary>
        private SKBitmap RotateBitmap90Clockwise(SKBitmap source)
        {
            // Após rotação 90°, largura e altura são invertidas
            var rotated = new SKBitmap(source.Height, source.Width);
            using var canvas = new SKCanvas(rotated);

            // Rotação 90° horário
            canvas.Translate(source.Height, 0);
            canvas.RotateDegrees(90);
            canvas.DrawBitmap(source, 0, 0);

            return rotated;
        }

        /// <summary>
        /// Rotaciona bitmap 90° horário + FlipX (Rotate90FlipX)
        /// </summary>
        private SKBitmap RotateBitmap90ClockwiseFlipX(SKBitmap source)
        {
            var rotated = RotateBitmap90Clockwise(source);
            var flipped = _imageManipulation.FlipHorizontal(rotated);
            rotated.Dispose();
            return flipped;
        }

        /// <summary>
        /// Rotaciona bitmap 270° horário (Rotate270FlipNone) = 90° anti-horário
        /// </summary>
        private SKBitmap RotateBitmap270Clockwise(SKBitmap source)
        {
            var rotated = new SKBitmap(source.Height, source.Width);
            using var canvas = new SKCanvas(rotated);

            // Rotação 270° horário = 90° anti-horário
            canvas.Translate(0, source.Width);
            canvas.RotateDegrees(-90);
            canvas.DrawBitmap(source, 0, 0);

            return rotated;
        }

        /// <summary>
        /// Rotaciona bitmap 270° horário + FlipX (Rotate270FlipX)
        /// </summary>
        private SKBitmap RotateBitmap270ClockwiseFlipX(SKBitmap source)
        {
            var rotated = RotateBitmap270Clockwise(source);
            var flipped = _imageManipulation.FlipHorizontal(rotated);
            rotated.Dispose();
            return flipped;
        }
    }
}
