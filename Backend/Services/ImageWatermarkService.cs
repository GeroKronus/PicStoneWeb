using SkiaSharp;

namespace PicStoneFotoAPI.Services;

/// <summary>
/// Serviço centralizado para adicionar marca d'água em todas as imagens geradas pela aplicação.
/// WHY: Elimina duplicação de código (estava repetido em FotoService, MockupService, BancadaService)
///      e garante consistência visual em todas as imagens.
/// </summary>
public class ImageWatermarkService
{
    private readonly ILogger<ImageWatermarkService> _logger;
    private readonly string _logoPath;
    private SKBitmap? _cachedLogo;

    public ImageWatermarkService(ILogger<ImageWatermarkService> logger)
    {
        _logger = logger;
        _logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "watermark.png");
    }

    /// <summary>
    /// Adiciona marca d'água no canto inferior direito da imagem.
    /// </summary>
    /// <param name="canvas">Canvas onde desenhar a marca</param>
    /// <param name="canvasWidth">Largura do canvas</param>
    /// <param name="canvasHeight">Altura do canvas</param>
    /// <param name="marginRight">Margem direita em pixels (padrão: 5)</param>
    /// <param name="marginBottom">Margem inferior em pixels (padrão: 5)</param>
    public void AddWatermark(SKCanvas canvas, int canvasWidth, int canvasHeight,
                            int marginRight = 5, int marginBottom = 5)
    {
        if (!File.Exists(_logoPath))
        {
            _logger.LogWarning("Logo não encontrada em: {LogoPath}", _logoPath);
            return;
        }

        try
        {
            // Carrega logo (usa cache se já carregou antes)
            if (_cachedLogo == null)
            {
                using var streamLogo = File.OpenRead(_logoPath);
                _cachedLogo = SKBitmap.Decode(streamLogo);

                if (_cachedLogo == null)
                {
                    _logger.LogWarning("Não foi possível decodificar a logo");
                    return;
                }
            }

            // Calcula posição (canto inferior direito)
            int posX = canvasWidth - _cachedLogo.Width - marginRight;
            int posY = canvasHeight - _cachedLogo.Height - marginBottom;

            // Desenha marca d'água
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High
            };

            canvas.DrawBitmap(_cachedLogo, posX, posY, paint);

            _logger.LogInformation("Marca d'água adicionada em ({PosX}, {PosY})", posX, posY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar marca d'água");
        }
    }

    /// <summary>
    /// Adiciona marca d'água diretamente em um SKBitmap (cria novo bitmap com marca).
    /// </summary>
    /// <param name="source">Bitmap original</param>
    /// <param name="marginRight">Margem direita em pixels (padrão: 5)</param>
    /// <param name="marginBottom">Margem inferior em pixels (padrão: 5)</param>
    /// <returns>Novo bitmap com marca d'água aplicada</returns>
    public SKBitmap AddWatermarkToBitmap(SKBitmap source, int marginRight = 5, int marginBottom = 5)
    {
        var result = new SKBitmap(source.Width, source.Height);
        using var canvas = new SKCanvas(result);

        // Desenha imagem original
        canvas.DrawBitmap(source, 0, 0);

        // Adiciona marca d'água
        AddWatermark(canvas, source.Width, source.Height, marginRight, marginBottom);

        return result;
    }

    /// <summary>
    /// Libera recursos do cache da logo.
    /// </summary>
    public void Dispose()
    {
        _cachedLogo?.Dispose();
        _cachedLogo = null;
    }
}
