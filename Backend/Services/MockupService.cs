using SkiaSharp;
using PicStoneFotoAPI.Models;

namespace PicStoneFotoAPI.Services
{
    public class MockupService
    {
        private readonly ILogger<MockupService> _logger;
        private readonly string _moldurasPath;
        private readonly string _uploadPath;

        public MockupService(ILogger<MockupService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _moldurasPath = Path.Combine(Directory.GetCurrentDirectory(), "Molduras");
            _uploadPath = configuration["UPLOAD_PATH"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        }

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

                // Gera mockup baseado no tipo
                if (request.TipoCavalete.ToLower() == "simples")
                {
                    var caminho = await GerarCavaleteSimples(bitmapCropado, request.Fundo);
                    caminhos.Add(caminho);
                }
                else // duplo
                {
                    var caminho = await GerarCavaleteDuplo(bitmapCropado, request.Fundo);
                    caminhos.Add(caminho);
                }

                return new MockupResponse
                {
                    Sucesso = true,
                    Mensagem = "Mockup gerado com sucesso!",
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

        private async Task<string> GerarCavaleteSimples(SKBitmap chapaCropada, string fundo)
        {
            // Nome do arquivo de moldura
            var nomeMoldura = fundo.ToLower() == "claro"
                ? "CAVALETE SIMPLES.png"
                : "CAVALETE SIMPLES Cinza.png";

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

            // Calcula proporção do crop e ajusta a moldura para se adequar
            float propImagBook = (float)chapaCropada.Width / chapaCropada.Height;
            int alturaImagMold = (int)(1487 / propImagBook);
            float propEntreProps = propImagBook / 1.9853f;

            // Dimensões do canvas final (largura fixa 1599, altura proporcional)
            int larguraCanvas = 1599;
            int alturaCanvas = (int)(628 / propEntreProps);

            // Posição da chapa
            int posX = 55;
            int posY = (int)(130 / propEntreProps);

            _logger.LogInformation("=== DEBUG MOCKUP SIMPLES ===");
            _logger.LogInformation("Crop original: {W}x{H}", chapaCropada.Width, chapaCropada.Height);
            _logger.LogInformation("PropImagBook: {Prop}", propImagBook);
            _logger.LogInformation("AlturaImagMold: {Altura}", alturaImagMold);
            _logger.LogInformation("PropEntreProps: {PropEntreProps}", propEntreProps);
            _logger.LogInformation("Canvas final: {W}x{H}", larguraCanvas, alturaCanvas);
            _logger.LogInformation("Chapa redimensionada: 1487x{H}", alturaImagMold);
            _logger.LogInformation("Posição chapa: X={X} Y={Y}", posX, posY);
            _logger.LogInformation("Chapa vai até Y={YFim} (canvas altura={CanvasH})", posY + alturaImagMold, alturaCanvas);
            _logger.LogInformation("============================");

            // Redimensiona a chapa para largura 1487 com altura proporcional
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(1487, alturaImagMold), SKFilterQuality.High);

            // Redimensiona a moldura para se adequar ao canvas
            var molduraRedimensionada = molduraOriginal.Resize(new SKImageInfo(larguraCanvas, alturaCanvas), SKFilterQuality.High);

            // Cria canvas final com dimensões calculadas
            using var surface = SKSurface.Create(new SKImageInfo(larguraCanvas, alturaCanvas));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Desenha a chapa no fundo (posição Y proporcional)
            canvas.DrawBitmap(chapaRedimensionada, posX, posY);

            // Sobrepõe a moldura redimensionada
            canvas.DrawBitmap(molduraRedimensionada, 0, 0);

            // Salva resultado
            var nomeArquivo = $"mockup_simples_{fundo}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
            var caminhoFinal = Path.Combine(_uploadPath, nomeArquivo);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
            using var outputStream = File.OpenWrite(caminhoFinal);
            data.SaveTo(outputStream);

            _logger.LogInformation("Cavalete simples gerado: {Caminho}", caminhoFinal);
            return nomeArquivo;
        }

        private async Task<string> GerarCavaleteDuplo(SKBitmap chapaCropada, string fundo)
        {
            // Nome do arquivo de moldura
            var nomeMoldura = fundo.ToLower() == "claro"
                ? "CAVALETE BASE.png"
                : "CAVALETE BASE Cinza.png";

            var caminhoMoldura = Path.Combine(_moldurasPath, nomeMoldura);

            _logger.LogInformation("Gerando cavalete duplo - Moldura: {NomeMoldura}", nomeMoldura);
            _logger.LogInformation("Caminho moldura: {CaminhoMoldura}", caminhoMoldura);
            _logger.LogInformation("Arquivo existe: {Existe}", File.Exists(caminhoMoldura));

            if (!File.Exists(caminhoMoldura))
            {
                throw new FileNotFoundException($"Moldura não encontrada: {caminhoMoldura}");
            }

            using var streamMoldura = File.OpenRead(caminhoMoldura);
            using var molduraOriginal = SKBitmap.Decode(streamMoldura);

            // Calcula proporção do crop e ajusta a moldura para se adequar
            float propImagBook = (float)chapaCropada.Width / chapaCropada.Height;
            int alturaImagMold = (int)(1487 / propImagBook);
            float propEntreProps = propImagBook / 1.9853f;

            // Dimensões do canvas final (largura fixa 3102, altura proporcional)
            int larguraCanvas = 3102;
            int alturaCanvas = (int)(1247 / propEntreProps);

            // Posição da chapa
            int posY = (int)(262 / propEntreProps);

            _logger.LogInformation("=== DEBUG MOCKUP DUPLO ===");
            _logger.LogInformation("Crop original: {W}x{H}", chapaCropada.Width, chapaCropada.Height);
            _logger.LogInformation("PropImagBook: {Prop}", propImagBook);
            _logger.LogInformation("AlturaImagMold: {Altura}", alturaImagMold);
            _logger.LogInformation("PropEntreProps: {PropEntreProps}", propEntreProps);
            _logger.LogInformation("Canvas final: {W}x{H}", larguraCanvas, alturaCanvas);
            _logger.LogInformation("Chapa redimensionada: 1487x{H}", alturaImagMold);
            _logger.LogInformation("Posição chapas: Y={Y}", posY);
            _logger.LogInformation("Chapas vão até Y={YFim} (canvas altura={CanvasH})", posY + alturaImagMold, alturaCanvas);
            _logger.LogInformation("===========================");

            // Redimensiona a chapa para largura 1487 com altura proporcional
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(1487, alturaImagMold), SKFilterQuality.High);

            // Cria espelho (bookmatch)
            var chapaEspelhada = new SKBitmap(chapaRedimensionada.Width, chapaRedimensionada.Height);
            using (var canvas2 = new SKCanvas(chapaEspelhada))
            {
                canvas2.Scale(-1, 1, chapaRedimensionada.Width / 2, 0);
                canvas2.DrawBitmap(chapaRedimensionada, 0, 0);
            }

            // Redimensiona a moldura para se adequar ao canvas
            var molduraRedimensionada = molduraOriginal.Resize(new SKImageInfo(larguraCanvas, alturaCanvas), SKFilterQuality.High);

            // Cria canvas final com dimensões calculadas
            using var surface = SKSurface.Create(new SKImageInfo(larguraCanvas, alturaCanvas));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Desenha as duas chapas (posição Y proporcional)
            canvas.DrawBitmap(chapaRedimensionada, 58, posY);      // Chapa 1
            canvas.DrawBitmap(chapaEspelhada, 1557, posY);         // Chapa 2 (espelhada)

            // Sobrepõe a moldura redimensionada
            canvas.DrawBitmap(molduraRedimensionada, 0, 0);

            // Salva resultado
            var nomeArquivo = $"mockup_duplo_{fundo}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
            var caminhoFinal = Path.Combine(_uploadPath, nomeArquivo);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
            using var outputStream = File.OpenWrite(caminhoFinal);
            data.SaveTo(outputStream);

            _logger.LogInformation("Cavalete duplo gerado: {Caminho}", caminhoFinal);
            return nomeArquivo;
        }
    }
}
