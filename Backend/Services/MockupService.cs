using SkiaSharp;
using PicStoneFotoAPI.Helpers;
using PicStoneFotoAPI.Models;

namespace PicStoneFotoAPI.Services
{
    public class MockupService
    {
        private readonly ILogger<MockupService> _logger;
        private readonly ImageWatermarkService _watermark;
        private readonly string _moldurasPath;
        private readonly string _uploadPath;

        public MockupService(ILogger<MockupService> logger,
                            IConfiguration configuration,
                            ImageWatermarkService watermark)
        {
            _logger = logger;
            _watermark = watermark;
            _moldurasPath = Path.Combine(Directory.GetCurrentDirectory(), "Molduras");
            _uploadPath = configuration["UPLOAD_PATH"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        }

        // ===== MÉTODO REMOVIDO: _watermark.AddWatermark (agora usa ImageWatermarkService) =====
        // ANTES: 37 linhas de código duplicado
        // DEPOIS: 1 linha usando _watermark.AddWatermark()
        // ECONOMIA: 36 linhas

        public async Task<MockupResponse> GerarMockupAsync(MockupRequest request)
        {
            try
            {
                _logger.LogInformation("Gerando mockup - Tipo: {Tipo}, Fundo: {Fundo}", request.TipoCavalete, request.Fundo);

                if (request.ImagemCropada == null)
                {
                    return new MockupResponse
                    {
                        Sucesso = false,
                        Mensagem = "Imagem cropada não fornecida"
                    };
                }

                // Carrega a imagem cropada
                using var streamCrop = new MemoryStream();
                await request.ImagemCropada.CopyToAsync(streamCrop);
                streamCrop.Position = 0;

                using var bitmapCropado = SKBitmap.Decode(streamCrop);
                if (bitmapCropado == null)
                {
                    return new MockupResponse
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao decodificar imagem cropada"
                    };
                }

                var caminhos = new List<string>();

                // Gera SEMPRE os 3 mockups como no VB.NET original

                // CavaletePronto - Duplo: original à esquerda, espelho à direita
                var caminhoDuplo1 = await GerarCavaleteDuplo(bitmapCropado, request.Fundo, inverterLados: false);
                caminhos.Add(caminhoDuplo1);

                // CavaletePronto2 - Duplo invertido: espelho à esquerda, original à direita
                var caminhoDuplo2 = await GerarCavaleteDuplo(bitmapCropado, request.Fundo, inverterLados: true);
                caminhos.Add(caminhoDuplo2);

                // CavaletePronto3 - Simples
                var caminhoSimples = await GerarCavaleteSimples(bitmapCropado, request.Fundo);
                caminhos.Add(caminhoSimples);

                return new MockupResponse
                {
                    Sucesso = true,
                    Mensagem = "3 mockups gerados com sucesso!",
                    CaminhosGerados = caminhos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar mockup");
                return new MockupResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro ao gerar mockup: {ex.Message}"
                };
            }
        }

        public async Task<string> GerarCavaleteSimples(SKBitmap chapaCropada, string fundo, int? usuarioId = null)
        {
            // Nome do arquivo de moldura
            var nomeMoldura = fundo.ToLower() == "claro"
                ? "CAVALETE SIMPLES.webp"
                : "CAVALETE SIMPLES Cinza.webp";

            var caminhoMoldura = Path.Combine(_moldurasPath, nomeMoldura);

            _logger.LogInformation("Gerando cavalete simples - Moldura: {NomeMoldura}", nomeMoldura);
            _logger.LogInformation("Caminho moldura: {CaminhoMoldura}", caminhoMoldura);
            _logger.LogInformation("Arquivo existe: {Existe}", File.Exists(caminhoMoldura));

            if (!File.Exists(caminhoMoldura))
            {
                throw new FileNotFoundException($"Moldura não encontrada: {caminhoMoldura}");
            }

            using var streamMoldura = File.OpenRead(caminhoMoldura);
            using var molduraOriginal = SKBitmap.Decode(streamMoldura);

            // Constantes da janela transparente na moldura original
            const int larguraJanela = 1487;
            const int alturaJanela = 749;
            const float propJanela = (float)larguraJanela / alturaJanela; // 1.9853

            // Redimensiona o crop para largura fixa 1487, mantendo proporção
            float propCrop = (float)chapaCropada.Width / chapaCropada.Height;
            int alturaCropRedim = (int)(larguraJanela / propCrop);

            // Calcula PropEntreProps como no VB.NET
            float propEntreProps = propCrop / propJanela;

            // Dimensões do canvas (como no VB.NET)
            int larguraCanvas = 1599;
            int alturaCanvas = (int)(1247 / propEntreProps);

            _logger.LogInformation("=== DEBUG MOCKUP SIMPLES ===");
            _logger.LogInformation("Crop original: {W}x{H}", chapaCropada.Width, chapaCropada.Height);
            _logger.LogInformation("PropCrop: {Prop}", propCrop);
            _logger.LogInformation("PropEntreProps: {PropEntreProps}", propEntreProps);
            _logger.LogInformation("Crop redimensionado: {W}x{H}", larguraJanela, alturaCropRedim);
            _logger.LogInformation("Canvas final: {W}x{H}", larguraCanvas, alturaCanvas);
            _logger.LogInformation("============================");

            // Redimensiona a chapa para largura da janela com altura proporcional
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(larguraJanela, alturaCropRedim), SKBitmapHelper.HighQuality);

            // Redimensiona a moldura para as dimensões do canvas (VB.NET usa Size)
            var molduraRedimensionada = molduraOriginal.Resize(new SKImageInfo(larguraCanvas, alturaCanvas), SKBitmapHelper.HighQuality);

            // Cria canvas final
            using var surface = SKSurface.Create(new SKImageInfo(larguraCanvas, alturaCanvas));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Posição da chapa (como no VB.NET: Y = 262 / PropEntreProps)
            int posX = 55;
            int posY = (int)(262 / propEntreProps);
            canvas.DrawBitmap(chapaRedimensionada, posX, posY);

            // Sobrepõe a moldura redimensionada
            canvas.DrawBitmap(molduraRedimensionada, 0, 0);

            // Adiciona marca d'água
            _watermark.AddWatermark(canvas, larguraCanvas, alturaCanvas);

            // Salva resultado - usa usuarioId se fornecido, senão usa timestamp
            var nomeArquivo = usuarioId.HasValue
                ? FileNamingHelper.GenerateCavaleteSimpleFileName(fundo, usuarioId.Value)
                : $"mockup_simples_{fundo}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
            // ✅ Muda extensão para .webp
            nomeArquivo = Path.ChangeExtension(nomeArquivo, ".webp");
            var caminhoFinal = Path.Combine(_uploadPath, nomeArquivo);

            using var image = surface.Snapshot();

            // ✅ OTIMIZAÇÃO: Redimensiona se maior que 1600px (WhatsApp comprime para ~1280px)
            const int MAX_DIMENSION = 1600;
            SKData data;
            if (Math.Max(larguraCanvas, alturaCanvas) > MAX_DIMENSION)
            {
                float escala = (float)MAX_DIMENSION / Math.Max(larguraCanvas, alturaCanvas);
                int novaLargura = (int)(larguraCanvas * escala);
                int novaAltura = (int)(alturaCanvas * escala);

                using var bitmap = SKBitmap.FromImage(image);
                using var bitmapReduzido = bitmap.Resize(new SKImageInfo(novaLargura, novaAltura), SKFilterQuality.High);
                using var imageReduzida = SKImage.FromBitmap(bitmapReduzido);
                data = imageReduzida.Encode(SKEncodedImageFormat.Webp, 85);

                _logger.LogInformation("Cavalete simples redimensionado de {W1}x{H1} para {W2}x{H2}",
                    larguraCanvas, alturaCanvas, novaLargura, novaAltura);
            }
            else
            {
                data = image.Encode(SKEncodedImageFormat.Webp, 85);
            }

            using (data)
            using (var outputStream = File.OpenWrite(caminhoFinal))
            {
                data.SaveTo(outputStream);
            }

            _logger.LogInformation("Cavalete simples gerado: {Caminho}", caminhoFinal);
            return nomeArquivo;
        }

        public async Task<string> GerarCavaleteDuplo(SKBitmap chapaCropada, string fundo, bool inverterLados, int? usuarioId = null)
        {
            // Nome do arquivo de moldura
            var nomeMoldura = fundo.ToLower() == "claro"
                ? "CAVALETE BASE.webp"
                : "CAVALETE BASE Cinza.webp";

            var caminhoMoldura = Path.Combine(_moldurasPath, nomeMoldura);

            _logger.LogInformation("Gerando cavalete duplo (invertido={Inv}) - Moldura: {NomeMoldura}", inverterLados, nomeMoldura);

            if (!File.Exists(caminhoMoldura))
            {
                throw new FileNotFoundException($"Moldura não encontrada: {caminhoMoldura}");
            }

            using var streamMoldura = File.OpenRead(caminhoMoldura);
            using var molduraOriginal = SKBitmap.Decode(streamMoldura);

            // Constantes da janela transparente na moldura original
            const int larguraJanela = 1487;
            const int alturaJanela = 749;
            const float propJanela = (float)larguraJanela / alturaJanela; // 1.9853

            // Redimensiona o crop para largura fixa 1487, mantendo proporção
            float propCrop = (float)chapaCropada.Width / chapaCropada.Height;
            int alturaCropRedim = (int)(larguraJanela / propCrop);

            // Calcula PropEntreProps como no VB.NET
            float propEntreProps = propCrop / propJanela;

            // Dimensões do canvas (como no VB.NET)
            int larguraCanvas = 3102;
            int alturaCanvas = (int)(1247 / propEntreProps);

            // Redimensiona a chapa para largura da janela com altura proporcional
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(larguraJanela, alturaCropRedim), SKBitmapHelper.HighQuality);

            // Cria espelho (bookmatch)
            var chapaEspelhada = new SKBitmap(chapaRedimensionada.Width, chapaRedimensionada.Height);
            using (var canvas2 = new SKCanvas(chapaEspelhada))
            {
                canvas2.Scale(-1, 1, chapaRedimensionada.Width / 2, 0);
                canvas2.DrawBitmap(chapaRedimensionada, 0, 0);
            }

            // Redimensiona a moldura para as dimensões do canvas (VB.NET usa Size)
            var molduraRedimensionada = molduraOriginal.Resize(new SKImageInfo(larguraCanvas, alturaCanvas), SKBitmapHelper.HighQuality);

            // Cria canvas final
            using var surface = SKSurface.Create(new SKImageInfo(larguraCanvas, alturaCanvas));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Posição das chapas (como no VB.NET: Y = 262 / PropEntreProps)
            int posY = (int)(262 / propEntreProps);

            if (inverterLados)
            {
                // CavaletePronto2: espelho à esquerda, original à direita
                canvas.DrawBitmap(chapaEspelhada, 58, posY);
                canvas.DrawBitmap(chapaRedimensionada, 1557, posY);
            }
            else
            {
                // CavaletePronto: original à esquerda, espelho à direita
                canvas.DrawBitmap(chapaRedimensionada, 58, posY);
                canvas.DrawBitmap(chapaEspelhada, 1557, posY);
            }

            // Sobrepõe a moldura redimensionada
            canvas.DrawBitmap(molduraRedimensionada, 0, 0);

            // Adiciona marca d'água
            _watermark.AddWatermark(canvas, larguraCanvas, alturaCanvas);

            // Salva resultado - usa usuarioId se fornecido, senão usa timestamp
            var sufixo = inverterLados ? "invertido" : "normal";
            var nomeArquivo = usuarioId.HasValue
                ? FileNamingHelper.GenerateCavaleteDuploFileName(sufixo, fundo, usuarioId.Value)
                : $"mockup_duplo_{sufixo}_{fundo}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
            // ✅ Muda extensão para .webp
            nomeArquivo = Path.ChangeExtension(nomeArquivo, ".webp");
            var caminhoFinal = Path.Combine(_uploadPath, nomeArquivo);

            using var image = surface.Snapshot();

            // ✅ OTIMIZAÇÃO: Redimensiona se maior que 1600px (WhatsApp comprime para ~1280px)
            const int MAX_DIMENSION = 1600;
            SKData data;
            if (Math.Max(larguraCanvas, alturaCanvas) > MAX_DIMENSION)
            {
                float escala = (float)MAX_DIMENSION / Math.Max(larguraCanvas, alturaCanvas);
                int novaLargura = (int)(larguraCanvas * escala);
                int novaAltura = (int)(alturaCanvas * escala);

                using var bitmap = SKBitmap.FromImage(image);
                using var bitmapReduzido = bitmap.Resize(new SKImageInfo(novaLargura, novaAltura), SKFilterQuality.High);
                using var imageReduzida = SKImage.FromBitmap(bitmapReduzido);
                data = imageReduzida.Encode(SKEncodedImageFormat.Webp, 85);

                _logger.LogInformation("Cavalete duplo redimensionado de {W1}x{H1} para {W2}x{H2}",
                    larguraCanvas, alturaCanvas, novaLargura, novaAltura);
            }
            else
            {
                data = image.Encode(SKEncodedImageFormat.Webp, 85);
            }

            using (data)
            using (var outputStream = File.OpenWrite(caminhoFinal))
            {
                data.SaveTo(outputStream);
            }

            _logger.LogInformation("Cavalete duplo {Tipo} gerado: {Caminho}", sufixo, caminhoFinal);
            return nomeArquivo;
        }
    }
}
