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
            using var moldura = SKBitmap.Decode(streamMoldura);

            // Dimensões da moldura original: 1599 x 628 (aproximado)
            // Área útil para a chapa: largura ~1487px, posição X ~55px, Y ~130px

            // Calcula redimensionamento proporcional
            int larguraUtil = 1487;
            int alturaUtil = (int)(larguraUtil * ((float)chapaCropada.Height / chapaCropada.Width));

            // Redimensiona a chapa cropada
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(larguraUtil, alturaUtil), SKFilterQuality.High);

            // Cria canvas final
            using var surface = SKSurface.Create(new SKImageInfo(moldura.Width, moldura.Height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Desenha a chapa no fundo
            int posX = 55;
            int posY = 130;
            canvas.DrawBitmap(chapaRedimensionada, posX, posY);

            // Sobrepõe a moldura
            canvas.DrawBitmap(moldura, 0, 0);

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
            using var moldura = SKBitmap.Decode(streamMoldura);

            // Dimensões da moldura: 3102 x 628 (aproximado)
            // Área útil por chapa: largura ~1487px
            // Posição chapa 1: X ~58px
            // Posição chapa 2: X ~1557px

            int larguraUtil = 1487;
            int alturaUtil = (int)(larguraUtil * ((float)chapaCropada.Height / chapaCropada.Width));

            // Redimensiona a chapa
            var chapaRedimensionada = chapaCropada.Resize(new SKImageInfo(larguraUtil, alturaUtil), SKFilterQuality.High);

            // Cria espelho (bookmatch)
            var chapaEspelhada = new SKBitmap(chapaRedimensionada.Width, chapaRedimensionada.Height);
            using (var canvas2 = new SKCanvas(chapaEspelhada))
            {
                canvas2.Scale(-1, 1, chapaRedimensionada.Width / 2, 0);
                canvas2.DrawBitmap(chapaRedimensionada, 0, 0);
            }

            // Cria canvas final
            using var surface = SKSurface.Create(new SKImageInfo(moldura.Width, moldura.Height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Desenha as duas chapas
            int posY = 130;
            canvas.DrawBitmap(chapaRedimensionada, 58, posY);  // Chapa 1
            canvas.DrawBitmap(chapaEspelhada, 1557, posY);     // Chapa 2 (espelhada)

            // Sobrepõe a moldura
            canvas.DrawBitmap(moldura, 0, 0);

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
