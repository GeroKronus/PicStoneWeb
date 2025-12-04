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
    }
}
