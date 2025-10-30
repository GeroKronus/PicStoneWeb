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
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(larguraJanela, alturaCropRedim), SKFilterQuality.High);

            // Redimensiona a moldura para as dimensões do canvas (VB.NET usa Size)
            var molduraRedimensionada = molduraOriginal.Resize(new SKImageInfo(larguraCanvas, alturaCanvas), SKFilterQuality.High);

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

            _logger.LogInformation("=== DEBUG MOCKUP DUPLO ===");
            _logger.LogInformation("Crop original: {W}x{H}", chapaCropada.Width, chapaCropada.Height);
            _logger.LogInformation("PropCrop: {Prop}", propCrop);
            _logger.LogInformation("PropEntreProps: {PropEntreProps}", propEntreProps);
            _logger.LogInformation("Crop redimensionado: {W}x{H}", larguraJanela, alturaCropRedim);
            _logger.LogInformation("Canvas final: {W}x{H}", larguraCanvas, alturaCanvas);
            _logger.LogInformation("===========================");

            // Redimensiona a chapa para largura da janela com altura proporcional
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(larguraJanela, alturaCropRedim), SKFilterQuality.High);

            // Cria espelho (bookmatch)
            var chapaEspelhada = new SKBitmap(chapaRedimensionada.Width, chapaRedimensionada.Height);
            using (var canvas2 = new SKCanvas(chapaEspelhada))
            {
                canvas2.Scale(-1, 1, chapaRedimensionada.Width / 2, 0);
                canvas2.DrawBitmap(chapaRedimensionada, 0, 0);
            }

            // Redimensiona a moldura para as dimensões do canvas (VB.NET usa Size)
            var molduraRedimensionada = molduraOriginal.Resize(new SKImageInfo(larguraCanvas, alturaCanvas), SKFilterQuality.High);

            // Cria canvas final
            using var surface = SKSurface.Create(new SKImageInfo(larguraCanvas, alturaCanvas));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Posição das chapas (como no VB.NET: Y = 262 / PropEntreProps)
            int posY = (int)(262 / propEntreProps);
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
