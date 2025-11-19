using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Collections.Generic;
using PicStoneFotoAPI.Services;
using System.Drawing;
using System.Linq;
using PicStoneFotoAPI.Models;
using PicStoneFotoAPI.Helpers;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Serviço responsável pela geração de mockups de escadas com diferentes estilos
    /// </summary>
    public class StairsService
    {
        private readonly ILogger<StairsService> _logger;
        private readonly GraphicsTransformService _graphicsTransformService;
        private readonly ImageWatermarkService _watermark;

        public StairsService(ILogger<StairsService> logger, GraphicsTransformService graphicsTransformService, ImageWatermarkService watermark)
        {
            _logger = logger;
            _graphicsTransformService = graphicsTransformService;
            _watermark = watermark;
        }

        /// <summary>
        /// Gera mockup de Escada #1 - OTIMIZADO (50% de todos os parâmetros)
        /// 4 Degraus (150px) + 6 Espelhos (50px)
        /// </summary>
        public SKBitmap GerarStairs1(SKBitmap imagemOriginal, bool rotacionado = false)
        {
            _logger.LogInformation("=== GERANDO ESCADA #1 (VERSÃO OTIMIZADA 50%) ===");

            // PASSO 1: Redimensionar para largura 1400px (50% = 4x menos pixels)
            const int LARGURA_ALVO = 1400;
            decimal fatorDeAjuste = (decimal)imagemOriginal.Width / LARGURA_ALVO;
            int novaAltura = (int)(imagemOriginal.Height / fatorDeAjuste);
            var imagemRedimensionada = imagemOriginal.Resize(
                new SKImageInfo(LARGURA_ALVO, novaAltura),
                SKBitmapHelper.HighQuality);
            _logger.LogInformation("Imagem redimensionada: {Width}x{Height}", LARGURA_ALVO, novaAltura);

            // PASSO 1.5: Rotacionar 180° se necessário
            if (rotacionado)
            {
                var temp = new SKBitmap(imagemRedimensionada.Width, imagemRedimensionada.Height);
                using var canvas = new SKCanvas(temp);
                canvas.Translate(imagemRedimensionada.Width / 2f, imagemRedimensionada.Height / 2f);
                canvas.RotateDegrees(180);
                canvas.Translate(-imagemRedimensionada.Width / 2f, -imagemRedimensionada.Height / 2f);
                canvas.DrawBitmap(imagemRedimensionada, 0, 0);
                imagemRedimensionada.Dispose();
                imagemRedimensionada = temp;
                _logger.LogInformation("Imagem rotacionada 180°");
            }

            // PASSO 2: Extrair 4 DEGRAUS (150px cada - 50% do original)
            _logger.LogInformation("Extraindo 4 degraus...");
            var degrau1 = ExtrairPorcao(imagemRedimensionada, 1250, 0, 150, imagemRedimensionada.Height);
            var degrau2 = ExtrairPorcao(imagemRedimensionada, 1050, 0, 150, imagemRedimensionada.Height);
            var degrau3 = ExtrairPorcao(imagemRedimensionada, 850, 0, 150, imagemRedimensionada.Height);
            var degrau4 = ExtrairPorcao(imagemRedimensionada, 650, 0, 150, imagemRedimensionada.Height);

            // PASSO 3: Extrair 6 ESPELHOS (50px cada - 50% do original)
            _logger.LogInformation("Extraindo 6 espelhos...");
            var espelho1 = ExtrairPorcao(imagemRedimensionada, 1200, 0, 50, imagemRedimensionada.Height);
            var espelho2 = ExtrairPorcao(imagemRedimensionada, 1000, 0, 50, imagemRedimensionada.Height);
            var espelho3 = ExtrairPorcao(imagemRedimensionada, 800, 0, 50, imagemRedimensionada.Height);
            var espelho4 = ExtrairPorcao(imagemRedimensionada, 600, 0, 50, imagemRedimensionada.Height);
            var espelho5 = ExtrairPorcao(imagemRedimensionada, 400, 0, 50, imagemRedimensionada.Height);
            var espelho6 = ExtrairPorcao(imagemRedimensionada, 200, 0, 50, imagemRedimensionada.Height);

            // PASSO 4: Transformar DEGRAUS (Distortion + Skew2 + Rotate90) - Parâmetros 50%
            _logger.LogInformation("Transformando degraus...");
            degrau1 = _graphicsTransformService.Distortion(degrau1, 1091, 800, 210, 1093);
            degrau1 = _graphicsTransformService.Skew2(degrau1, 0, 150);
            degrau1 = RotacionarImagem(degrau1, 90);

            degrau2 = _graphicsTransformService.Distortion(degrau2, 813, 655, 94, 812);
            degrau2 = _graphicsTransformService.Skew2(degrau2, 0, 90);
            degrau2 = RotacionarImagem(degrau2, 90);

            degrau3 = _graphicsTransformService.Distortion(degrau3, 649, 540, 43, 649);
            degrau3 = _graphicsTransformService.Skew2(degrau3, 0, 63);
            degrau3 = RotacionarImagem(degrau3, 90);

            degrau4 = _graphicsTransformService.Distortion(degrau4, 542, 470, 18, 548);
            degrau4 = _graphicsTransformService.Skew2(degrau4, 0, 35);
            degrau4 = RotacionarImagem(degrau4, 90);

            // PASSO 5: Transformar ESPELHOS (apenas Resize + Rotate90) - Dimensões 50%
            _logger.LogInformation("Transformando espelhos...");
            espelho1 = espelho1.Resize(new SKImageInfo(69, 810), SKBitmapHelper.HighQuality);
            espelho1 = RotacionarImagem(espelho1, 90);

            espelho2 = espelho2.Resize(new SKImageInfo(59, 653), SKBitmapHelper.HighQuality);
            espelho2 = RotacionarImagem(espelho2, 90);

            espelho3 = espelho3.Resize(new SKImageInfo(50, 546), SKBitmapHelper.HighQuality);
            espelho3 = RotacionarImagem(espelho3, 90);

            espelho4 = espelho4.Resize(new SKImageInfo(44, 465), SKBitmapHelper.HighQuality);
            espelho4 = RotacionarImagem(espelho4, 90);

            espelho5 = espelho5.Resize(new SKImageInfo(38, 404), SKBitmapHelper.HighQuality);
            espelho5 = RotacionarImagem(espelho5, 90);

            espelho6 = espelho6.Resize(new SKImageInfo(35, 359), SKBitmapHelper.HighQuality);
            espelho6 = RotacionarImagem(espelho6, 90);

            // PASSO 6: Compor no canvas 1000x1450 (50% do original)
            _logger.LogInformation("Montando canvas 1000x1450...");
            const int CANVAS_WIDTH = 1000;
            const int CANVAS_HEIGHT = 1450;
            var canvasFinal = new SKBitmap(CANVAS_WIDTH, CANVAS_HEIGHT);
            using (var canvas = new SKCanvas(canvasFinal))
            {
                canvas.Clear(SKColors.Transparent);

                // Coordenadas 50% (divididas por 2)
                canvas.DrawBitmap(degrau1, -179, 1242);
                canvas.DrawBitmap(espelho1, 111, 1108);
                canvas.DrawBitmap(degrau2, 20, 1014);
                canvas.DrawBitmap(espelho2, 185, 906);
                canvas.DrawBitmap(degrau3, 125, 864);
                canvas.DrawBitmap(espelho3, 238, 773);
                canvas.DrawBitmap(degrau4, 201, 755);
                canvas.DrawBitmap(espelho4, 275, 675);
                canvas.DrawBitmap(espelho5, 304, 607);
                canvas.DrawBitmap(espelho6, 326, 560);
            }

            // PASSO 7: Compor com overlay escada1_menor.webp
            _logger.LogInformation("Aplicando overlay escada1_menor.webp...");
            var overlayPath = Path.Combine("MockupResources", "Escadas", "escada1_menor.webp");
            SKBitmap resultado;
            using (var overlayStream = File.OpenRead(overlayPath))
            using (var overlay = SKBitmap.Decode(overlayStream))
            {
                using (var canvas = new SKCanvas(canvasFinal))
                {
                    canvas.DrawBitmap(overlay, 0, 0);
                }
                resultado = canvasFinal;
            }

            // Liberar memória
            imagemRedimensionada.Dispose();
            degrau1.Dispose();
            degrau2.Dispose();
            degrau3.Dispose();
            degrau4.Dispose();
            espelho1.Dispose();
            espelho2.Dispose();
            espelho3.Dispose();
            espelho4.Dispose();
            espelho5.Dispose();
            espelho6.Dispose();

            // Adicionar marca d'água
            using (var canvas = new SKCanvas(resultado))
            {
                _watermark.AddWatermark(canvas, resultado.Width, resultado.Height);
            }

            _logger.LogInformation("Escada #1 gerada com sucesso!");
            return resultado;
        }

        /// <summary>
        /// Extrai uma porção retangular da imagem
        /// </summary>
        private SKBitmap ExtrairPorcao(SKBitmap imagem, int x, int y, int largura, int altura)
        {
            var rect = new SKRectI(x, y, x + largura, y + altura);
            var porcao = new SKBitmap(largura, altura);
            using (var canvas = new SKCanvas(porcao))
            {
                canvas.DrawBitmap(imagem, rect, new SKRect(0, 0, largura, altura));
            }
            return porcao;
        }

        /// <summary>
        /// Rotaciona imagem 90 graus
        /// </summary>
        private SKBitmap RotacionarImagem(SKBitmap imagem, int graus)
        {
            var rotacionado = new SKBitmap(imagem.Height, imagem.Width);
            using (var canvas = new SKCanvas(rotacionado))
            {
                canvas.Translate(rotacionado.Width / 2f, rotacionado.Height / 2f);
                canvas.RotateDegrees(graus);
                canvas.Translate(-imagem.Width / 2f, -imagem.Height / 2f);
                canvas.DrawBitmap(imagem, 0, 0);
            }
            return rotacionado;
        }

        /// <summary>
        /// Transforma degrau aplicando skew para criar efeito trapézio (base menor em cima)
        /// </summary>
        private SKBitmap TransformarDegrau(SKBitmap degrau)
        {
            // PASSO 1: Rotação 90° (horária)
            var rotacionado90 = new SKBitmap(degrau.Height, degrau.Width);
            using (var canvas = new SKCanvas(rotacionado90))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotacionado90.Width / 2f, rotacionado90.Height / 2f);
                canvas.RotateDegrees(90);
                canvas.DrawBitmap(degrau, -degrau.Width / 2f, -degrau.Height / 2f);
            }

            // PASSO 2: Aplicar Skew (inclinação) com GraphicsTransformService
            // Fator 4.0 = 400/100 do VB.NET
            var skewed = _graphicsTransformService.Skew(rotacionado90, 4.0f, 0);
            rotacionado90.Dispose();

            // PASSO 3: Rotação 270° (anti-horária) para voltar orientação
            var rotacionado270 = new SKBitmap(skewed.Height, skewed.Width);
            using (var canvas = new SKCanvas(rotacionado270))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotacionado270.Width / 2f, rotacionado270.Height / 2f);
                canvas.RotateDegrees(270);
                canvas.DrawBitmap(skewed, -skewed.Width / 2f, -skewed.Height / 2f);
            }
            skewed.Dispose();

            // ✅ NOVA CORREÇÃO: Rotação 180° adicional apenas para os degraus
            // Inverte os degraus em relação aos espelhos
            var rotacionadoFinal = new SKBitmap(rotacionado270.Width, rotacionado270.Height);
            using (var canvas = new SKCanvas(rotacionadoFinal))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotacionado270.Width / 2f, rotacionado270.Height / 2f);
                canvas.RotateDegrees(180);
                canvas.Translate(-rotacionado270.Width / 2f, -rotacionado270.Height / 2f);
                canvas.DrawBitmap(rotacionado270, 0, 0);
            }
            rotacionado270.Dispose();

            return rotacionadoFinal;
        }

        /// <summary>
        /// Transforma espelho aplicando skew inverso para criar efeito trapézio (base maior em cima)
        /// </summary>
        private SKBitmap TransformarEspelho(SKBitmap espelho)
        {
            // PASSO 1: Rotação 90° (horária)
            var rotacionado90 = new SKBitmap(espelho.Height, espelho.Width);
            using (var canvas = new SKCanvas(rotacionado90))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotacionado90.Width / 2f, rotacionado90.Height / 2f);
                canvas.RotateDegrees(90);
                canvas.DrawBitmap(espelho, -espelho.Width / 2f, -espelho.Height / 2f);
            }

            // PASSO 2: Aplicar Skew invertido
            // Fator 1.0 = 100/100 do VB.NET
            var skewed = _graphicsTransformService.Skew(rotacionado90, 1.0f, 0);
            rotacionado90.Dispose();

            // PASSO 3: Rotação 270° (anti-horária)
            var rotacionado270 = new SKBitmap(skewed.Height, skewed.Width);
            using (var canvas = new SKCanvas(rotacionado270))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotacionado270.Width / 2f, rotacionado270.Height / 2f);
                canvas.RotateDegrees(270);
                canvas.DrawBitmap(skewed, -skewed.Width / 2f, -skewed.Height / 2f);
            }
            skewed.Dispose();

            return rotacionado270;
        }

        /// <summary>
        /// Extrai uma fatia vertical da imagem
        /// </summary>
        private SKBitmap ExtrairFatiaVertical(SKBitmap imagem, int x, int largura)
        {
            // Garantir que não ultrapasse os limites da imagem
            if (x + largura > imagem.Width)
            {
                largura = imagem.Width - x;
            }

            var rect = new SKRectI(x, 0, x + largura, imagem.Height);
            var fatia = new SKBitmap(largura, imagem.Height);

            using var canvas = new SKCanvas(fatia);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(imagem, rect, new SKRect(0, 0, largura, imagem.Height));

            return fatia;
        }

        // MÉTODO GENÉRICO COMENTADO - GerarStairs2/3/4 usam implementação específica
        // Descomentar quando TransformacaoConfig for criado em Models
        /*
        /// <summary>
        /// Método genérico para gerar escadas com base em configuração
        /// </summary>
        public SKBitmap GerarEscada(SKBitmap imagemOriginal, StairsConfig config, bool rotacionado = false)
        {
            _logger.LogInformation($"=== INICIANDO {config.Nome} (MÉTODO GENÉRICO) ===");
            _logger.LogInformation("Imagem original: {Width}x{Height}, Rotacionado: {Rot}",
                imagemOriginal.Width, imagemOriginal.Height, rotacionado);

            // PASSO 1: Redimensionar para largura padrão mantendo proporção
            float fatorDeAjuste = (float)imagemOriginal.Width / config.LarguraPadrao;
            int novaAltura = (int)(imagemOriginal.Height / fatorDeAjuste);

            var imagemRedimensionada = imagemOriginal.Resize(
                new SKImageInfo(config.LarguraPadrao, novaAltura),
                SKBitmapHelper.HighQuality);

            // PASSO 2: Rotacionar 180° se necessário
            if (rotacionado)
            {
                var temp = RotacionarImagem(imagemRedimensionada, 180);
                imagemRedimensionada.Dispose();
                imagemRedimensionada = temp;
            }

            // PASSO 3: Extrair e transformar degraus
            var degraus = new List<SKBitmap>();
            foreach (var degrauConfig in config.Degraus)
            {
                var degrau = ExtrairFatiaVertical(imagemRedimensionada, degrauConfig.PosicaoX, degrauConfig.Largura);
                var transformado = TransformarElementoGenerico(degrau, degrauConfig.Transformacoes);
                degraus.Add(transformado);
                degrau.Dispose();
            }

            // PASSO 4: Extrair e transformar espelhos
            var espelhos = new List<SKBitmap>();
            foreach (var espelhoConfig in config.Espelhos)
            {
                var espelho = ExtrairFatiaVertical(imagemRedimensionada, espelhoConfig.PosicaoX, espelhoConfig.Largura);
                var transformado = TransformarElementoGenerico(espelho, espelhoConfig.Transformacoes);
                espelhos.Add(transformado);
                espelho.Dispose();
            }

            imagemRedimensionada.Dispose();

            // PASSO 5: Montar mosaico
            var canvasTemp = new SKBitmap(config.LarguraMosaico, config.AlturaMosaico);
            using (var graphics = new SKCanvas(canvasTemp))
            {
                graphics.Clear(SKColors.Transparent);

                // Posicionar degraus
                for (int i = 0; i < degraus.Count && i < config.PosicoesDegraus.Count; i++)
                {
                    var pos = config.PosicoesDegraus[i];
                    graphics.DrawBitmap(degraus[i], pos.X, pos.Y);
                }

                // Posicionar espelhos
                for (int i = 0; i < espelhos.Count && i < config.PosicoesEspelhos.Count; i++)
                {
                    var pos = config.PosicoesEspelhos[i];
                    graphics.DrawBitmap(espelhos[i], pos.X, pos.Y);
                }
            }

            // Liberar memória
            foreach (var d in degraus) d.Dispose();
            foreach (var e in espelhos) e.Dispose();

            // PASSO 6: Aplicar rotação final se configurada
            SKBitmap canvasRotacionado = canvasTemp;
            if (config.RotacaoFinal != 0)
            {
                canvasRotacionado = RotacionarImagem(canvasTemp, config.RotacaoFinal);
                canvasTemp.Dispose();
            }

            // PASSO 7: Redimensionar para tamanho final
            var resultado = canvasRotacionado.Resize(
                new SKImageInfo(config.LarguraFinal, config.AlturaFinal),
                SKBitmapHelper.HighQuality);
            canvasRotacionado.Dispose();

            // PASSO 8: Adicionar overlay se disponível
            AplicarOverlay(resultado, config.CaminhoOverlay);

            _logger.LogInformation($"=== {config.Nome} CONCLUÍDO ===");
            return resultado;
        }
        */

        // MÉTODO GENÉRICO COMENTADO - GerarStairs2/3/4 usam implementação específica
        // Descomentar quando TransformacaoConfig for criado em Models
        /*
        /// <summary>
        /// Transforma elemento aplicando lista de transformações
        /// </summary>
        private SKBitmap TransformarElementoGenerico(SKBitmap elemento, List<TransformacaoConfig> transformacoes)
        {
            var atual = elemento;

            foreach (var trans in transformacoes)
            {
                SKBitmap novo = null;

                switch (trans.Tipo)
                {
                    case TipoTransformacao.Rotate:
                        novo = RotacionarImagem(atual, trans.Parametro1);
                        break;
                    case TipoTransformacao.Skew:
                        novo = _graphicsTransformService.Skew(atual, trans.Parametro1 / 100f, (int)trans.Parametro2);
                        break;
                    case TipoTransformacao.Resize:
                        novo = atual.Resize(
                            new SKImageInfo((int)trans.Parametro1, (int)trans.Parametro2),
                            SKBitmapHelper.HighQuality);
                        break;
                    case TipoTransformacao.RotateImage3:
                        novo = RotateImage3(atual, trans.Parametro1, trans.Parametro2, trans.Parametro3);
                        break;
                }

                if (novo != null && novo != atual)
                {
                    if (atual != elemento) atual.Dispose();
                    atual = novo;
                }
            }

            return atual;
        }
        */

        /// <summary>
        /// Rotaciona imagem com centro
        /// </summary>
        private SKBitmap RotacionarImagem(SKBitmap imagem, float angulo)
        {
            var rotacionada = new SKBitmap(imagem.Width, imagem.Height);
            using var canvas = new SKCanvas(rotacionada);
            canvas.Clear(SKColors.Transparent);
            canvas.Translate(imagem.Width / 2f, imagem.Height / 2f);
            canvas.RotateDegrees(angulo);
            canvas.Translate(-imagem.Width / 2f, -imagem.Height / 2f);
            canvas.DrawBitmap(imagem, 0, 0);
            return rotacionada;
        }

        /// <summary>
        /// Rotaciona imagem com centro e adiciona offset X/Y após rotação
        /// </summary>
        private SKBitmap RotacionarImagemComOffset(SKBitmap imagem, float angulo, int offsetX, int offsetY)
        {
            var rotacionada = new SKBitmap(imagem.Width, imagem.Height);
            using var canvas = new SKCanvas(rotacionada);
            canvas.Clear(SKColors.Transparent);
            canvas.Translate(imagem.Width / 2f, imagem.Height / 2f);
            canvas.RotateDegrees(angulo);
            canvas.Translate(-imagem.Width / 2f, -imagem.Height / 2f);
            // Adiciona offset após rotação
            canvas.DrawBitmap(imagem, offsetX, offsetY);
            return rotacionada;
        }

        /// <summary>
        /// Função especial de rotação com escala (baseada no VB.NET RotateImage3)
        /// </summary>
        private SKBitmap RotateImage3(SKBitmap imagem, float angulo, float escalaLargura, float escalaAltura)
        {
            int novaLargura = (int)(imagem.Width * escalaLargura);
            int novaAltura = (int)(imagem.Height * escalaAltura);

            var resultado = new SKBitmap(novaLargura, novaAltura);
            using var canvas = new SKCanvas(resultado);
            canvas.Clear(SKColors.Transparent);

            // Aplicar transformações
            canvas.Translate(imagem.Width / 2f, imagem.Height / 2f);
            canvas.RotateDegrees(angulo);
            canvas.Translate(-imagem.Width / 2f, -imagem.Height / 2f);

            // Desenhar com offset específico (VB.NET: retBMP.Width - 272)
            float offsetX = resultado.Width - 272;
            canvas.DrawBitmap(imagem, offsetX, 0);

            return resultado;
        }

        /// <summary>
        /// Aplica overlay sobre a imagem final
        /// </summary>
        private void AplicarOverlay(SKBitmap imagem, string caminhoOverlay)
        {
            if (string.IsNullOrEmpty(caminhoOverlay) || !File.Exists(caminhoOverlay))
                return;

            using var overlayImage = SKBitmap.Decode(caminhoOverlay);
            using var graphics = new SKCanvas(imagem);

            if (overlayImage.Width != imagem.Width || overlayImage.Height != imagem.Height)
            {
                using var overlayResized = overlayImage.Resize(
                    new SKImageInfo(imagem.Width, imagem.Height),
                    SKBitmapHelper.HighQuality);
                graphics.DrawBitmap(overlayResized, 0, 0);
            }
            else
            {
                graphics.DrawBitmap(overlayImage, 0, 0);
            }
        }

        /// <summary>
        /// Gera mockup de Escada #2 - IMPLEMENTAÇÃO EXATA DO VB.NET
        /// </summary>
        public SKBitmap GerarStairs2(SKBitmap imagemOriginal, bool rotacionado = false)
        {
            _logger.LogInformation("=== GERANDO STAIRS #2 (CÓDIGO REFATORADO - SEM DEBUG) ===");

            // PASSO 1: Redimensionar para 1400x600 (50% do tamanho original VB.NET)
            const int LARGURA = 1400;
            const int ALTURA = 600;
            var imagemRedimensionada = imagemOriginal.Resize(new SKImageInfo(LARGURA, ALTURA), SKBitmapHelper.HighQuality);
            _logger.LogInformation("Imagem redimensionada: {Width}x{Height}", LARGURA, ALTURA);

            // PASSO 1.5: Rotacionar 180° se necessário
            if (rotacionado)
            {
                var temp = new SKBitmap(imagemRedimensionada.Width, imagemRedimensionada.Height);
                using var canvas = new SKCanvas(temp);
                canvas.Translate(imagemRedimensionada.Width / 2f, imagemRedimensionada.Height / 2f);
                canvas.RotateDegrees(180);
                canvas.Translate(-imagemRedimensionada.Width / 2f, -imagemRedimensionada.Height / 2f);
                canvas.DrawBitmap(imagemRedimensionada, 0, 0);
                imagemRedimensionada.Dispose();
                imagemRedimensionada = temp;
                _logger.LogInformation("Imagem rotacionada 180°");
            }

            // DIVISÃO PROPORCIONAL: Mantém proporção entre degrau e espelho
            const int LARGURA_DEGRAU = 153;   // Todos os degraus com mesmo tamanho
            const int LARGURA_ESPELHO = 96;    // Todos os espelhos com mesmo tamanho

            var coordenadasDegraus = new[] {
                (x: LARGURA - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
                (x: LARGURA - 1*LARGURA_DEGRAU - 1*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
                (x: LARGURA - 2*LARGURA_DEGRAU - 2*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
                (x: LARGURA - 3*LARGURA_DEGRAU - 3*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
                (x: LARGURA - 4*LARGURA_DEGRAU - 4*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),
                (x: LARGURA - 5*LARGURA_DEGRAU - 5*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU)
            };

            var coordenadasEspelhos = new[] {
                (x: LARGURA - 1*LARGURA_DEGRAU - 1*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
                (x: LARGURA - 2*LARGURA_DEGRAU - 2*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
                (x: LARGURA - 3*LARGURA_DEGRAU - 3*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
                (x: LARGURA - 4*LARGURA_DEGRAU - 4*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),
                (x: LARGURA - 5*LARGURA_DEGRAU - 5*LARGURA_ESPELHO, largura: LARGURA_ESPELHO)
            };

            _logger.LogInformation("🔹 Gerando 6 DEGRAUS (em memória)...");

            // PASSO 2: Gerar todos os 6 DEGRAUS (manter em memória)
            var degrausTransformados = new SKBitmap[6];
            for (int i = 0; i < 6; i++)
            {
                var (x, largura) = coordenadasDegraus[i];
                var degrauNum = i + 1;

                var degrauRect = new SKRectI(x, 0, x + largura, ALTURA);
                var degrauOriginal = new SKBitmap(largura, ALTURA);
                using (var canvas = new SKCanvas(degrauOriginal))
                {
                    canvas.DrawBitmap(imagemRedimensionada, degrauRect, new SKRect(0, 0, largura, ALTURA));
                }

                degrausTransformados[i] = TransformarDegrauEscada2(degrauOriginal);
                _logger.LogInformation($"   ✅ DEGRAU{degrauNum}: {degrausTransformados[i].Width}x{degrausTransformados[i].Height}");
                degrauOriginal.Dispose();
            }

            _logger.LogInformation("🔹 Gerando 5 ESPELHOS (em memória)...");

            // PASSO 3: Gerar todos os 5 ESPELHOS (manter em memória)
            var espelhosTransformados = new SKBitmap[5];
            for (int i = 0; i < 5; i++)
            {
                var (x, largura) = coordenadasEspelhos[i];
                var espelhoNum = i + 1;

                var espelhoRect = new SKRectI(x, 0, x + largura, ALTURA);
                var espelhoOriginal = new SKBitmap(largura, ALTURA);
                using (var canvas = new SKCanvas(espelhoOriginal))
                {
                    canvas.DrawBitmap(imagemRedimensionada, espelhoRect, new SKRect(0, 0, largura, ALTURA));
                }

                espelhosTransformados[i] = TransformarEspelhoEscada2(espelhoOriginal);
                _logger.LogInformation($"   ✅ ESPELHO{espelhoNum}: {espelhosTransformados[i].Width}x{espelhosTransformados[i].Height}");
                espelhoOriginal.Dispose();
            }

            _logger.LogInformation("🖼️ Montando canvas 2100x2100...");

            // PASSO 4: COMPOSIÇÃO FINAL NO CANVAS 2100x2100
            const int CANVAS_WIDTH = 2100;
            const int CANVAS_HEIGHT = 2100;
            var canvasFinal = new SKBitmap(CANVAS_WIDTH, CANVAS_HEIGHT);

            using (var canvas = new SKCanvas(canvasFinal))
            {
                canvas.Clear(SKColors.Transparent);

                // Usar peças dos arrays de memória
                canvas.DrawBitmap(degrausTransformados[0], 1587, 334);
                canvas.DrawBitmap(espelhosTransformados[0], 1491, 484);
                canvas.DrawBitmap(degrausTransformados[1], 1338, 484);
                canvas.DrawBitmap(espelhosTransformados[1], 1242, 634);
                canvas.DrawBitmap(degrausTransformados[2], 1089, 634);
                canvas.DrawBitmap(espelhosTransformados[2], 993, 784);
                canvas.DrawBitmap(degrausTransformados[3], 840, 784);
                canvas.DrawBitmap(espelhosTransformados[3], 744, 934);
                canvas.DrawBitmap(degrausTransformados[4], 591, 934);
                canvas.DrawBitmap(espelhosTransformados[4], 495, 1084);
                canvas.DrawBitmap(degrausTransformados[5], 342, 1084);
            }

            // PASSO 5: ROTACIONAR 60 GRAUS
            _logger.LogInformation("🔄 Rotação 60°...");
            var canvasRotacionado = RotacionarImagemComOffset(canvasFinal, 60, 90, 0);

            // PASSO 6: COMPRESSÃO HORIZONTAL
            _logger.LogInformation("📐 Compressão 2100x2100 → 1500x2100...");
            var canvasComprimido = canvasRotacionado.Resize(new SKImageInfo(1500, 2100), SKBitmapHelper.HighQuality);

            // PASSO 7: APLICAR 2 CAMADAS
            _logger.LogInformation("🎨 Efeito 2 camadas...");
            var canvasDuasCamadas = new SKBitmap(1500, 2100);
            using (var canvas = new SKCanvas(canvasDuasCamadas))
            {
                canvas.Clear(SKColors.Transparent);
                const int OFFSET_GLOBAL_X = -40;
                canvas.DrawBitmap(canvasComprimido, OFFSET_GLOBAL_X, 0);
                canvas.DrawBitmap(canvasComprimido, OFFSET_GLOBAL_X + 20, -20);
            }

            // PASSO 8: CROP E OFFSETS FINAIS
            _logger.LogInformation("✂️ Crop final...");
            const int CROP_TOP = 460;
            const int CROP_BOTTOM = 360;
            const int OFFSET_FINAL_X = -15;
            const int OFFSET_FINAL_Y = 103;
            const int CANVAS_FINAL_WIDTH = 1500;
            const int CANVAS_FINAL_HEIGHT = 1280;

            var canvasFinalComOffsets = new SKBitmap(CANVAS_FINAL_WIDTH, CANVAS_FINAL_HEIGHT);
            using (var canvas = new SKCanvas(canvasFinalComOffsets))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(canvasDuasCamadas, OFFSET_FINAL_X, OFFSET_FINAL_Y - CROP_TOP);
            }

            // PASSO 9: COMPOR COM OVERLAY
            _logger.LogInformation("🎨 Overlay escada2.webp...");
            var overlayPath = Path.Combine("MockupResources", "Escadas", "escada2.webp");
            SKBitmap canvasComOverlay;

            using (var overlayStream = File.OpenRead(overlayPath))
            using (var overlay = SKBitmap.Decode(overlayStream))
            {
                canvasComOverlay = new SKBitmap(overlay.Width, overlay.Height);
                using (var canvas = new SKCanvas(canvasComOverlay))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(canvasFinalComOffsets, 0, 0);
                    canvas.DrawBitmap(overlay, 0, 0);
                }
            }

            // Liberar memória
            imagemRedimensionada.Dispose();
            canvasFinal.Dispose();
            canvasRotacionado.Dispose();
            canvasComprimido.Dispose();
            canvasDuasCamadas.Dispose();
            canvasFinalComOffsets.Dispose();

            foreach (var degrau in degrausTransformados) { degrau?.Dispose(); }
            foreach (var espelho in espelhosTransformados) { espelho?.Dispose(); }

            // Adicionar marca d'água
            using (var canvas = new SKCanvas(canvasComOverlay))
            {
                _watermark.AddWatermark(canvas, canvasComOverlay.Width, canvasComOverlay.Height);
            }

            _logger.LogInformation("✅ STAIRS #2 CONCLUÍDO (sem imagens de debug)");
            return canvasComOverlay;
        }

        /// <summary>
        /// Transformar degrau para Escada #2 (VB.NET: Rotate90 -> Skew(400) -> Rotate270)
        /// </summary>
        private SKBitmap TransformarDegrauEscada2(SKBitmap degrau, int fatorSkew)
        {
            // PASSO 1: Rotação 90° (horária)
            var rotacionado90 = new SKBitmap(degrau.Height, degrau.Width);
            using (var canvas = new SKCanvas(rotacionado90))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotacionado90.Width / 2f, rotacionado90.Height / 2f);
                canvas.RotateDegrees(90);
                canvas.DrawBitmap(degrau, -degrau.Width / 2f, -degrau.Height / 2f);
            }

            // PASSO 2: Aplicar Skew(400)
            var skewed = _graphicsTransformService.Skew(rotacionado90, fatorSkew / 100f, 0);
            rotacionado90.Dispose();

            // PASSO 3: Rotação 270° (anti-horária)
            var rotacionado270 = new SKBitmap(skewed.Height, skewed.Width);
            using (var canvas = new SKCanvas(rotacionado270))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotacionado270.Width / 2f, rotacionado270.Height / 2f);
                canvas.RotateDegrees(270);
                canvas.DrawBitmap(skewed, -skewed.Width / 2f, -skewed.Height / 2f);
            }
            skewed.Dispose();

            return rotacionado270;
        }

        /// <summary>
        /// Transformar espelho com tamanho correto (120 x AltDegrau)
        /// </summary>
        private SKBitmap TransformarEspelhoEscada2ComTamanho(SKBitmap espelho, int fatorSkew, int altDegrau)
        {
            // PASSO 1: Redimensionar para 120 x AltDegrau (não 275x3000!)
            var espelhoRedimensionado = espelho.Resize(
                new SKImageInfo(120, altDegrau),
                SKBitmapHelper.HighQuality);

            // PASSO 2: Aplicar Skew(100)
            var skewed = _graphicsTransformService.Skew(espelhoRedimensionado, fatorSkew / 100f, 0);
            espelhoRedimensionado.Dispose();

            // PASSO 3: RotateImage3(18.45, 7, 1.5) com offset correto
            var rotacionado = RotateImage3ComOffset(skewed, 18.45f, 7f, 1.5f);
            skewed.Dispose();

            return rotacionado;
        }

        /// <summary>
        /// RotateImage3 - REPLICAÇÃO EXATA DO VB.NET (Form1.vb:11469)
        /// VB.NET: Cria bitmap GRANDE, desenha imagem ORIGINAL com transformações
        /// NÃO ESCALA a imagem - apenas cria canvas grande!
        /// </summary>
        private SKBitmap RotateImage3ComOffset(SKBitmap imagem, float angulo, float escalaLargura, float escalaAltura)
        {
            // VB.NET: Larg = CInt(Larg * Largura)  <- dimensões do CANVAS, não da imagem!
            int larguraCanvas = (int)(imagem.Width * escalaLargura);
            int alturaCanvas = (int)(imagem.Height * escalaAltura);

            // VB.NET: Dim retBMP As New Bitmap(Larg, Altu)
            var resultado = new SKBitmap(larguraCanvas, alturaCanvas);
            using var canvas = new SKCanvas(resultado);
            canvas.Clear(SKColors.Transparent);

            // VB.NET: g.TranslateTransform(img.Width \ 2, img.Height \ 2)  <- usa imagem ORIGINAL!
            canvas.Translate(imagem.Width / 2f, imagem.Height / 2f);

            // VB.NET: g.RotateTransform(angle)
            canvas.RotateDegrees(angulo);

            // VB.NET: g.TranslateTransform(-img.Width \ 2, -img.Height \ 2)
            canvas.Translate(-imagem.Width / 2f, -imagem.Height / 2f);

            // VB.NET: g.DrawImage(img, New PointF(retBMP.Width - 272, 0))
            // Desenha a imagem ORIGINAL (não escalada) no canvas grande
            float offsetX = larguraCanvas - 272;
            canvas.DrawBitmap(imagem, offsetX, 0);

            return resultado;
        }

        /// <summary>
        /// RotateImage com offset (-30, -30) para rotação final do mosaico
        /// </summary>
        private SKBitmap RotateImageWithOffset(SKBitmap imagem, float angulo)
        {
            var resultado = new SKBitmap(imagem.Width, imagem.Height);
            using var canvas = new SKCanvas(resultado);
            canvas.Clear(SKColors.Transparent);

            canvas.Translate(imagem.Width / 2f, imagem.Height / 2f);
            canvas.RotateDegrees(angulo);
            canvas.Translate(-imagem.Width / 2f, -imagem.Height / 2f);

            // ⚠️ OFFSET DO VB.NET: (-30, -30)
            canvas.DrawBitmap(imagem, -30, -30);

            return resultado;
        }

        // MÉTODOS COMENTADOS TEMPORARIAMENTE - Dependem de GerarEscada genérico
        // Descomentar quando GerarEscada e TransformacaoConfig estiverem prontos
        /*
        /// <summary>
        /// Gera mockup de Escada #3
        /// </summary>
        public SKBitmap GerarStairs3(SKBitmap imagemOriginal, bool rotacionado = false)
        {
            var config = StairsConfigFactory.ObterConfiguracao("stairs3");
            return GerarEscada(imagemOriginal, config, rotacionado);
        }

        /// <summary>
        /// Gera mockup de Escada #4
        /// </summary>
        public SKBitmap GerarStairs4(SKBitmap imagemOriginal, bool rotacionado = false)
        {
            var config = StairsConfigFactory.ObterConfiguracao("stairs4");
            return GerarEscada(imagemOriginal, config, rotacionado);
        }

        /// <summary>
        /// Gera mockup de Escada #5
        /// </summary>
        public SKBitmap GerarStairs5(SKBitmap imagemOriginal, bool rotacionado = false)
        {
            var config = StairsConfigFactory.ObterConfiguracao("stairs5");
            return GerarEscada(imagemOriginal, config, rotacionado);
        }
        */

        // ===== MÉTODOS DE DEBUG =====

        /// <summary>
        /// Salva imagem para debug
        /// </summary>
        private void SalvarImagemDebug(SKBitmap imagem, string caminho)
        {
            try
            {
                using var data = imagem.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(caminho);
                data.SaveTo(stream);
                _logger.LogInformation($"Debug: Imagem salva em {caminho}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar imagem de debug");
            }
        }
        /// <summary>
        /// MÉTODO DE TESTE: Processa apenas degrau1 e espelho1 para verificar inclinação e conexão dos vértices
        /// </summary>
        public SKBitmap TestarDegrau1Espelho1(SKBitmap imagemOriginal)
        {
            _logger.LogInformation("=== GERANDO TODAS AS 11 PORÇÕES (6 DEGRAUS + 5 ESPELHOS) ===");

            // PASSO 1: Redimensionar para 1400x600 (50% do tamanho original VB.NET)
            const int LARGURA = 1400;
            const int ALTURA = 600;
            var imagemRedimensionada = imagemOriginal.Resize(new SKImageInfo(LARGURA, ALTURA), SKBitmapHelper.HighQuality);
            _logger.LogInformation("Imagem redimensionada: {Width}x{Height}", LARGURA, ALTURA);

            // DIVISÃO PROPORCIONAL: Mantém proporção entre degrau e espelho
            // Proporção original (50% reduzida): degrau=135px, espelho=85px → relação 1.588:1
            // Cálculo: 6D + 5E = 1400px, com D/E = 135/85
            // Resultado: Degrau = 153px, Espelho = 96px (todos iguais dentro de cada tipo)
            // Total: 6×153 + 5×96 = 918 + 480 = 1398px (~1400px)
            // Sequência da DIREITA para ESQUERDA: Degrau1, Espelho1, Degrau2, Espelho2, etc.
            const int LARGURA_DEGRAU = 153;   // Todos os degraus com mesmo tamanho
            const int LARGURA_ESPELHO = 96;    // Todos os espelhos com mesmo tamanho

            var coordenadasDegraus = new[] {
                (x: LARGURA - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),                                                                  // Degrau1 (posição 1 da direita)
                (x: LARGURA - 1*LARGURA_DEGRAU - 1*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),                         // Degrau2 (depois de D1+E1)
                (x: LARGURA - 2*LARGURA_DEGRAU - 2*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),                         // Degrau3 (depois de D1+E1+D2+E2)
                (x: LARGURA - 3*LARGURA_DEGRAU - 3*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),                         // Degrau4 (depois de D1+E1+D2+E2+D3+E3)
                (x: LARGURA - 4*LARGURA_DEGRAU - 4*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU),                         // Degrau5 (depois de D1+E1+D2+E2+D3+E3+D4+E4)
                (x: LARGURA - 5*LARGURA_DEGRAU - 5*LARGURA_ESPELHO - 1*LARGURA_DEGRAU, largura: LARGURA_DEGRAU)                          // Degrau6 (início)
            };

            var coordenadasEspelhos = new[] {
                (x: LARGURA - 1*LARGURA_DEGRAU - 1*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),                                           // Espelho1 (depois de D1)
                (x: LARGURA - 2*LARGURA_DEGRAU - 2*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),                                           // Espelho2 (depois de D1+E1+D2)
                (x: LARGURA - 3*LARGURA_DEGRAU - 3*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),                                           // Espelho3 (depois de D1+E1+D2+E2+D3)
                (x: LARGURA - 4*LARGURA_DEGRAU - 4*LARGURA_ESPELHO, largura: LARGURA_ESPELHO),                                           // Espelho4 (depois de D1+E1+D2+E2+D3+E3+D4)
                (x: LARGURA - 5*LARGURA_DEGRAU - 5*LARGURA_ESPELHO, largura: LARGURA_ESPELHO)                                            // Espelho5 (depois de D1+E1+D2+E2+D3+E3+D4+E4+D5)
            };

            _logger.LogInformation("🔹 Gerando 6 DEGRAUS com coordenadas do VB.NET...");

            // PASSO 2: Gerar todos os 6 DEGRAUS (manter em memória)
            var degrausTransformados = new SKBitmap[6];
            for (int i = 0; i < 6; i++)
            {
                var (x, largura) = coordenadasDegraus[i];
                var degrauNum = i + 1;

                // Extrair degrau da imagem redimensionada
                var degrauRect = new SKRectI(x, 0, x + largura, ALTURA);
                var degrauOriginal = new SKBitmap(largura, ALTURA);
                using (var canvas = new SKCanvas(degrauOriginal))
                {
                    canvas.DrawBitmap(imagemRedimensionada, degrauRect, new SKRect(0, 0, largura, ALTURA));
                }

                // Transformar em paralogramo ASCENDENTE e armazenar
                degrausTransformados[i] = TransformarDegrauEscada2(degrauOriginal);

                _logger.LogInformation($"   ✅ DEGRAU{degrauNum} processado: X={x} Largura={largura} → {degrausTransformados[i].Width}x{degrausTransformados[i].Height}");

                // Liberar apenas o original
                degrauOriginal.Dispose();
            }

            _logger.LogInformation("🔹 Gerando 5 ESPELHOS com coordenadas do VB.NET...");

            // PASSO 3: Gerar todos os 5 ESPELHOS (manter em memória)
            var espelhosTransformados = new SKBitmap[5];
            for (int i = 0; i < 5; i++)
            {
                var (x, largura) = coordenadasEspelhos[i];
                var espelhoNum = i + 1;

                // Extrair espelho da imagem redimensionada
                var espelhoRect = new SKRectI(x, 0, x + largura, ALTURA);
                var espelhoOriginal = new SKBitmap(largura, ALTURA);
                using (var canvas = new SKCanvas(espelhoOriginal))
                {
                    canvas.DrawBitmap(imagemRedimensionada, espelhoRect, new SKRect(0, 0, largura, ALTURA));
                }

                // Transformar em paralogramo DESCENDENTE e armazenar
                espelhosTransformados[i] = TransformarEspelhoEscada2(espelhoOriginal);

                _logger.LogInformation($"   ✅ ESPELHO{espelhoNum} processado: X={x} Largura={largura} → {espelhosTransformados[i].Width}x{espelhosTransformados[i].Height}");

                // Liberar apenas o original
                espelhoOriginal.Dispose();
            }

            _logger.LogInformation("🎯 TODAS AS 11 PORÇÕES GERADAS COM SUCESSO!");
            _logger.LogInformation("🖼️ Montando canvas final 2100x2100...");

            // PASSO 3: COMPOSIÇÃO FINAL NO CANVAS 2100x2100
            const int CANVAS_WIDTH = 2100;
            const int CANVAS_HEIGHT = 2100;
            var canvasFinal = new SKBitmap(CANVAS_WIDTH, CANVAS_HEIGHT);

            using (var canvas = new SKCanvas(canvasFinal))
            {
                canvas.Clear(SKColors.Transparent);

                // Usar peças transformadas dos arrays de memória
                // Coordenadas: (X, Y) no canvas 2100x2100

                // DEGRAU 1 - (1587, 334)
                canvas.DrawBitmap(degrausTransformados[0], 1587, 334);

                // ESPELHO 1 - (1491, 484)
                canvas.DrawBitmap(espelhosTransformados[0], 1491, 484);

                // DEGRAU 2 - (1338, 484)
                canvas.DrawBitmap(degrausTransformados[1], 1338, 484);

                // ESPELHO 2 - (1242, 634)
                canvas.DrawBitmap(espelhosTransformados[1], 1242, 634);

                // DEGRAU 3 - (1089, 634)
                canvas.DrawBitmap(degrausTransformados[2], 1089, 634);

                // ESPELHO 3 - (993, 784)
                canvas.DrawBitmap(espelhosTransformados[2], 993, 784);

                // DEGRAU 4 - (840, 784)
                canvas.DrawBitmap(degrausTransformados[3], 840, 784);

                // ESPELHO 4 - (744, 934)
                canvas.DrawBitmap(espelhosTransformados[3], 744, 934);

                // DEGRAU 5 - (591, 934)
                canvas.DrawBitmap(degrausTransformados[4], 591, 934);

                // ESPELHO 5 - (495, 1084)
                canvas.DrawBitmap(espelhosTransformados[4], 495, 1084);

                // DEGRAU 6 - (342, 1084)
                canvas.DrawBitmap(degrausTransformados[5], 342, 1084);
            }

            // DEBUG: Salvar canvas final como "EscadaMontada.png" (COMENTADO)
            // var escadaMontadaPath = Path.Combine("wwwroot", "debug", "EscadaMontada.png");
            // using (var stream = File.OpenWrite(escadaMontadaPath))
            // {
            //     canvasFinal.Encode(stream, SKEncodedImageFormat.Png, 100);
            //     stream.Flush();
            // }
            // _logger.LogInformation($"✅ ESCADA MONTADA SALVA: {escadaMontadaPath}");
            // _logger.LogInformation($"   Canvas: {CANVAS_WIDTH}x{CANVAS_HEIGHT}");

            // PASSO 4: ROTACIONAR 60 GRAUS com offset de 90px para direita
            _logger.LogInformation("🔄 Aplicando rotação de 60 graus com offset +90px...");
            var canvasRotacionado = RotacionarImagemComOffset(canvasFinal, 60, 90, 0);

            // DEBUG: Salvar canvas rotacionado como "EscadaMontadaRotacionada.png" (COMENTADO)
            // var escadaRotacionadaPath = Path.Combine("wwwroot", "debug", "EscadaMontadaRotacionada.png");
            // using (var stream = File.OpenWrite(escadaRotacionadaPath))
            // {
            //     canvasRotacionado.Encode(stream, SKEncodedImageFormat.Png, 100);
            //     stream.Flush();
            // }
            // _logger.LogInformation($"✅ ESCADA ROTACIONADA SALVA: {escadaRotacionadaPath}");

            _logger.LogInformation($"   Canvas rotacionado: {canvasRotacionado.Width}x{canvasRotacionado.Height}");

            // PASSO 5: COMPRESSÃO HORIZONTAL (2100x2100 → 1500x2100)
            _logger.LogInformation("📐 Aplicando compressão horizontal (2100x2100 → 1500x2100)...");
            var canvasComprimido = canvasRotacionado.Resize(
                new SKImageInfo(1500, 2100),
                SKBitmapHelper.HighQuality);

            // DEBUG: Salvar canvas comprimido como "EscadaRedimensionada.png" (COMENTADO)
            // var escadaRedimensionadaPath = Path.Combine("wwwroot", "debug", "EscadaRedimensionada.png");
            // using (var stream = File.OpenWrite(escadaRedimensionadaPath))
            // {
            //     canvasComprimido.Encode(stream, SKEncodedImageFormat.Png, 100);
            //     stream.Flush();
            // }
            // _logger.LogInformation($"✅ ESCADA REDIMENSIONADA SALVA: {escadaRedimensionadaPath}");
            // _logger.LogInformation($"   Canvas redimensionado: {canvasComprimido.Width}x{canvasComprimido.Height}");

            // PASSO 6: APLICAR 2 CAMADAS (camada superior deslocada +20px direita, -20px cima)
            // + deslocamento global de -25px no eixo X
            _logger.LogInformation("🎨 Aplicando efeito de 2 camadas com deslocamento...");
            var canvasDuasCamadas = new SKBitmap(1500, 2100);
            using (var canvas = new SKCanvas(canvasDuasCamadas))
            {
                canvas.Clear(SKColors.Transparent);

                // Deslocamento global: -40px no eixo X
                const int OFFSET_GLOBAL_X = -40;

                // Camada 1 (fundo) - posição com offset global (-25, 0)
                canvas.DrawBitmap(canvasComprimido, OFFSET_GLOBAL_X, 0);

                // Camada 2 (topo) - deslocada (+20px direita, -20px cima) + offset global
                canvas.DrawBitmap(canvasComprimido, OFFSET_GLOBAL_X + 20, -20);
            }

            // DEBUG: Salvar canvas com 2 camadas como "EscadaDuasCamadas.png" (COMENTADO)
            // var escadaDuasCamadasPath = Path.Combine("wwwroot", "debug", "EscadaDuasCamadas.png");
            // using (var stream = File.OpenWrite(escadaDuasCamadasPath))
            // {
            //     canvasDuasCamadas.Encode(stream, SKEncodedImageFormat.Png, 100);
            //     stream.Flush();
            // }
            // _logger.LogInformation($"✅ ESCADA DUAS CAMADAS SALVA: {escadaDuasCamadasPath}");
            // _logger.LogInformation($"   Canvas final: {canvasDuasCamadas.Width}x{canvasDuasCamadas.Height}");

            // PASSO 7: CORTAR 460px do topo e 360px de baixo + APLICAR OFFSETS FINAIS
            _logger.LogInformation("✂️ Aplicando crop: -460px topo, -360px baixo + offsets finais: X=-15, Y=+103");
            const int CROP_TOP = 460;
            const int CROP_BOTTOM = 360;
            const int OFFSET_FINAL_X = -15;
            const int OFFSET_FINAL_Y = 103;

            int alturaAposCrop = canvasDuasCamadas.Height - CROP_TOP - CROP_BOTTOM; // 2100 - 460 - 360 = 1280

            // Canvas final FIXO em 1500x1280 (mesmo tamanho do overlay)
            const int CANVAS_FINAL_WIDTH = 1500;
            const int CANVAS_FINAL_HEIGHT = 1280;

            var canvasFinalComOffsets = new SKBitmap(CANVAS_FINAL_WIDTH, CANVAS_FINAL_HEIGHT);
            using (var canvas = new SKCanvas(canvasFinalComOffsets))
            {
                canvas.Clear(SKColors.Transparent);

                // Desenha a imagem com crop E offsets aplicados simultaneamente
                // Y = OFFSET_FINAL_Y - CROP_TOP (103 - 460 = -357)
                canvas.DrawBitmap(canvasDuasCamadas, OFFSET_FINAL_X, OFFSET_FINAL_Y - CROP_TOP);
            }

            // DEBUG: Salvar como "EscadaFinal.png" já com offsets aplicados (COMENTADO)
            // var escadaFinalPath = Path.Combine("wwwroot", "debug", "EscadaFinal.png");
            // using (var stream = File.OpenWrite(escadaFinalPath))
            // {
            //     canvasFinalComOffsets.Encode(stream, SKEncodedImageFormat.Png, 100);
            //     stream.Flush();
            // }
            // _logger.LogInformation($"✅ ESCADA FINAL SALVA (com offsets aplicados): {escadaFinalPath}");
            // _logger.LogInformation($"   Canvas final: {canvasFinalComOffsets.Width}x{canvasFinalComOffsets.Height}");

            // PASSO 8: COMPOR COM OVERLAY (escada2.webp)
            _logger.LogInformation("🎨 Carregando e compondo overlay escada2.webp...");
            var overlayPath = Path.Combine("MockupResources", "Escadas", "escada2.webp");
            SKBitmap canvasComOverlay;

            using (var overlayStream = File.OpenRead(overlayPath))
            using (var overlay = SKBitmap.Decode(overlayStream))
            {
                // Criar canvas com as mesmas dimensões do overlay (1500x1280)
                canvasComOverlay = new SKBitmap(overlay.Width, overlay.Height);
                using (var canvas = new SKCanvas(canvasComOverlay))
                {
                    canvas.Clear(SKColors.Transparent);

                    // Desenha a EscadaFinal em (0,0)
                    canvas.DrawBitmap(canvasFinalComOffsets, 0, 0);

                    // Sobrepõe o overlay em (0,0)
                    canvas.DrawBitmap(overlay, 0, 0);
                }
            }

            // Salvar resultado final com overlay
            var resultadoFinalPath = Path.Combine("wwwroot", "debug", "EscadaComOverlay.png");
            using (var stream = File.OpenWrite(resultadoFinalPath))
            {
                canvasComOverlay.Encode(stream, SKEncodedImageFormat.Png, 100);
                stream.Flush();
            }

            _logger.LogInformation($"✅ RESULTADO FINAL COM OVERLAY SALVO: {resultadoFinalPath}");
            _logger.LogInformation($"   Dimensões: {canvasComOverlay.Width}x{canvasComOverlay.Height}");

            // DEBUG: OPCIONAL: Gerar "EscadaPlotada.png" para visualização (COMENTADO)
            // var canvasPlotado = new SKBitmap(1600, 1500);
            // using (var canvas = new SKCanvas(canvasPlotado))
            // {
            //     canvas.Clear(SKColors.White);
            //     canvas.DrawBitmap(canvasFinalComOffsets, 50, 50);
            // }
            // var escadaPlotadaPath = Path.Combine("wwwroot", "debug", "EscadaPlotada.png");
            // using (var stream = File.OpenWrite(escadaPlotadaPath))
            // {
            //     canvasPlotado.Encode(stream, SKEncodedImageFormat.Png, 100);
            //     stream.Flush();
            // }
            // _logger.LogInformation($"✅ ESCADA PLOTADA SALVA (visualização): {escadaPlotadaPath}");

            // Liberar memória
            imagemRedimensionada.Dispose();
            canvasFinal.Dispose();
            canvasRotacionado.Dispose();
            canvasComprimido.Dispose();
            canvasDuasCamadas.Dispose();
            canvasFinalComOffsets.Dispose();

            // Liberar arrays de peças transformadas
            foreach (var degrau in degrausTransformados)
            {
                degrau?.Dispose();
            }
            foreach (var espelho in espelhosTransformados)
            {
                espelho?.Dispose();
            }

            return canvasComOverlay;
        }

        /// <summary>
        /// Transforma degrau em paralogramo ASCENDENTE (Escada2)
        /// Paralelogramo VERTICAL com lado DIREITO deslocado PARA CIMA
        /// Usando ComputeMatrix da Microsoft/Xamarin
        /// </summary>
        private SKBitmap TransformarDegrauEscada2(SKBitmap original)
        {
            // Original: 135x600 (vertical) - REDUZIDO 50%
            // Criar paralelogramo VERTICAL: lado DIREITO sobe 200px
            int w = original.Width;   // 135
            int h = original.Height;  // 600
            int deslocamento = 200;   // Lado direito sobe 200px (50% de 400px)
            int canvasWidth = w;
            int canvasHeight = h + deslocamento;  // Altura aumenta para acomodar deslocamento

            _logger.LogInformation($"Degrau original: {w}x{h}, canvas inicial: {canvasWidth}x{canvasHeight}");

            // Define os 4 vértices do paralelogramo VERTICAL
            // Lado ESQUERDO: mantém posição original (base em Y)
            // Lado DIREITO: desloca 200px PARA CIMA (Y - 200)
            var transformado = _graphicsTransformService.TransformPerspective(
                input: original,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                topLeft: new SKPoint(0, deslocamento),      // Topo esquerdo (Y = 200)
                topRight: new SKPoint(w, 0),                // Topo direito SOBE 200px (Y = 0)
                bottomLeft: new SKPoint(0, canvasHeight),   // Base esquerda (Y = 800)
                bottomRight: new SKPoint(w, h)              // Base direita SOBE 200px (Y = 600)
            );

            // Fazer crop para remover áreas transparentes e ter canvas exato
            var bounds = GetImageBounds(transformado);
            var resultado = CropBitmap(transformado, bounds);
            transformado.Dispose();

            _logger.LogInformation($"Degrau transformado e cortado: {resultado.Width}x{resultado.Height}");

            return resultado;
        }

        /// <summary>
        /// Transforma espelho em paralogramo DESCENDENTE (Escada2)
        /// Paralelogramo VERTICAL com lado DIREITO deslocado PARA BAIXO
        /// Usando ComputeMatrix da Microsoft/Xamarin
        /// </summary>
        private SKBitmap TransformarEspelhoEscada2(SKBitmap original)
        {
            // Criar paralelogramo VERTICAL: lado DIREITO desce 50px
            // Original: 85x600 - REDUZIDO 50%
            int w = original.Width;   // 85
            int h = original.Height;  // 600
            int deslocamento = 50;    // Lado direito desce 50px (50% de 100px)
            int canvasWidth = w;
            int canvasHeight = h + deslocamento;  // Altura aumenta para acomodar deslocamento

            _logger.LogInformation($"Espelho original: {w}x{h}, canvas: {canvasWidth}x{canvasHeight}");

            // Define os 4 vértices do paralelogramo VERTICAL
            // Lado ESQUERDO: mantém posição original (topo em Y=0)
            // Lado DIREITO: desloca 50px PARA BAIXO (Y + 50)
            var resultado = _graphicsTransformService.TransformPerspective(
                input: original,
                canvasWidth: canvasWidth,
                canvasHeight: canvasHeight,
                topLeft: new SKPoint(0, 0),                 // Topo esquerdo (Y = 0)
                topRight: new SKPoint(w, deslocamento),     // Topo direito DESCE 50px (Y = 50)
                bottomLeft: new SKPoint(0, h),              // Base esquerda (Y = 600)
                bottomRight: new SKPoint(w, canvasHeight)   // Base direita DESCE 50px (Y = 650)
            );

            _logger.LogInformation($"Espelho transformado: {resultado.Width}x{resultado.Height}");

            return resultado;
        }

        /// <summary>
        /// Calcula o retângulo delimitador (bounding box) da área não-transparente de uma imagem
        /// </summary>
        private SKRectI GetImageBounds(SKBitmap bitmap)
        {
            int minX = bitmap.Width;
            int minY = bitmap.Height;
            int maxX = 0;
            int maxY = 0;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.Alpha > 0) // Pixel não é totalmente transparente
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            return new SKRectI(minX, minY, maxX + 1, maxY + 1);
        }

        /// <summary>
        /// Corta uma imagem para o retângulo especificado
        /// </summary>
        private SKBitmap CropBitmap(SKBitmap source, SKRectI cropRect)
        {
            var cropped = new SKBitmap(cropRect.Width, cropRect.Height);
            using (var canvas = new SKCanvas(cropped))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(source,
                    new SKRect(cropRect.Left, cropRect.Top, cropRect.Right, cropRect.Bottom),
                    new SKRect(0, 0, cropRect.Width, cropRect.Height));
            }
            return cropped;
        }
    }
}