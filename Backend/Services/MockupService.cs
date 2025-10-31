using SkiaSharp;
using PicStoneFotoAPI.Models;

namespace PicStoneFotoAPI.Services
{
    public class MockupService
    {
        private readonly ILogger<MockupService> _logger;
        private readonly string _moldurasPath;
        private readonly string _uploadPath;
        private readonly string _logoPath;

        public MockupService(ILogger<MockupService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _moldurasPath = Path.Combine(Directory.GetCurrentDirectory(), "Molduras");
            _uploadPath = configuration["UPLOAD_PATH"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            _logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Cavaletes", "logoamarelo.png");
        }

        // Adiciona logo no canto inferior direito
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

                // Posição: canto inferior direito com margem de 20px
                int posX = canvasWidth - logoWidth - 20;
                int posY = canvasHeight - logoHeight - 20;

                // Desenha a logo sem redimensionar
                canvas.DrawBitmap(logo, posX, posY);

                _logger.LogInformation("Marca d'água adicionada: {W}x{H} em ({X},{Y})", logoWidth, logoHeight, posX, posY);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar marca d'água");
            }
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

            // Adiciona marca d'água
            AdicionarMarcaDagua(canvas, larguraCanvas, alturaCanvas);

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

        private async Task<string> GerarCavaleteDuplo(SKBitmap chapaCropada, string fundo, bool inverterLados)
        {
            // Nome do arquivo de moldura
            var nomeMoldura = fundo.ToLower() == "claro"
                ? "CAVALETE BASE.png"
                : "CAVALETE BASE Cinza.png";

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
            AdicionarMarcaDagua(canvas, larguraCanvas, alturaCanvas);

            // Salva resultado
            var sufixo = inverterLados ? "invertido" : "normal";
            var nomeArquivo = $"mockup_duplo_{sufixo}_{fundo}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
            var caminhoFinal = Path.Combine(_uploadPath, nomeArquivo);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
            using var outputStream = File.OpenWrite(caminhoFinal);
            data.SaveTo(outputStream);

            _logger.LogInformation("Cavalete duplo {Tipo} gerado: {Caminho}", sufixo, caminhoFinal);
            return nomeArquivo;
        }
    }
}
