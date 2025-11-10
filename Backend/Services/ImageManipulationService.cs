using SkiaSharp;
using System.IO;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Service centralizado para manipulação de imagens (crop, rotate, flip, resize)
    /// WHY: Elimina duplicação de código - antes havia 12+ métodos duplicados em 4 arquivos diferentes
    /// PATTERN: DRY (Don't Repeat Yourself) - uma implementação única para todas as transformações básicas
    /// </summary>
    public class ImageManipulationService
    {
        private readonly ILogger<ImageManipulationService> _logger;

        public ImageManipulationService(ILogger<ImageManipulationService> logger)
        {
            _logger = logger;
        }

        #region Crop Operations

        /// <summary>
        /// Faz crop de uma região específica da imagem
        /// </summary>
        public SKBitmap Crop(SKBitmap source, SKRectI cropRect)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (cropRect.Width <= 0 || cropRect.Height <= 0)
                throw new ArgumentException("Área de crop inválida", nameof(cropRect));

            var cropped = new SKBitmap(cropRect.Width, cropRect.Height);

            using var canvas = new SKCanvas(cropped);
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.Clear(SKColors.White);
            var destRect = new SKRect(0, 0, cropRect.Width, cropRect.Height);
            canvas.DrawBitmap(source, cropRect, destRect, paint);

            _logger.LogDebug($"Crop realizado: {source.Width}x{source.Height} -> {cropRect.Width}x{cropRect.Height}");

            return cropped;
        }

        /// <summary>
        /// Faz crop usando coordenadas x, y, width, height
        /// </summary>
        public SKBitmap Crop(SKBitmap source, int x, int y, int width, int height)
        {
            return Crop(source, new SKRectI(x, y, x + width, y + height));
        }

        #endregion

        #region Rotation Operations

        /// <summary>
        /// Rotaciona imagem em qualquer ângulo (suporta 90, 180, 270, -90, etc.)
        /// </summary>
        public SKBitmap Rotate(SKBitmap source, float degrees)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Normalizar ângulo para 0-360
            degrees = degrees % 360;
            if (degrees < 0) degrees += 360;

            // Para rotações de 90°, ajustar dimensões do canvas
            bool swap = (degrees == 90 || degrees == 270);
            int width = swap ? source.Height : source.Width;
            int height = swap ? source.Width : source.Height;

            var rotated = new SKBitmap(width, height);

            using var canvas = new SKCanvas(rotated);
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.Clear(SKColors.White);

            // Rotacionar ao redor do centro
            canvas.Translate(width / 2f, height / 2f);
            canvas.RotateDegrees(degrees);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);

            canvas.DrawBitmap(source, 0, 0, paint);

            _logger.LogDebug($"Rotação aplicada: {degrees}° - {source.Width}x{source.Height} -> {width}x{height}");

            return rotated;
        }

        /// <summary>
        /// Rotaciona 90° no sentido horário
        /// </summary>
        public SKBitmap Rotate90(SKBitmap source) => Rotate(source, 90);

        /// <summary>
        /// Rotaciona 180°
        /// </summary>
        public SKBitmap Rotate180(SKBitmap source) => Rotate(source, 180);

        /// <summary>
        /// Rotaciona 270° no sentido horário (ou 90° anti-horário)
        /// </summary>
        public SKBitmap Rotate270(SKBitmap source) => Rotate(source, 270);

        #endregion

        #region Flip Operations

        /// <summary>
        /// Espelha imagem horizontalmente (esquerda ↔ direita)
        /// </summary>
        public SKBitmap FlipHorizontal(SKBitmap source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var flipped = new SKBitmap(source.Width, source.Height);

            using var canvas = new SKCanvas(flipped);
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.Clear(SKColors.White);
            canvas.Scale(-1, 1, source.Width / 2f, 0);
            canvas.DrawBitmap(source, 0, 0, paint);

            _logger.LogDebug($"Flip horizontal aplicado: {source.Width}x{source.Height}");

            return flipped;
        }

        /// <summary>
        /// Espelha imagem verticalmente (cima ↔ baixo)
        /// </summary>
        public SKBitmap FlipVertical(SKBitmap source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var flipped = new SKBitmap(source.Width, source.Height);

            using var canvas = new SKCanvas(flipped);
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.Clear(SKColors.White);
            canvas.Scale(1, -1, 0, source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0, paint);

            _logger.LogDebug($"Flip vertical aplicado: {source.Width}x{source.Height}");

            return flipped;
        }

        #endregion

        #region Resize Operations

        /// <summary>
        /// Redimensiona imagem mantendo proporção
        /// </summary>
        public SKBitmap Resize(SKBitmap source, int targetWidth, int? targetHeight = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (targetWidth <= 0)
                throw new ArgumentException("Largura alvo deve ser maior que zero", nameof(targetWidth));

            // Se altura não especificada, manter proporção
            int finalHeight;
            if (targetHeight.HasValue)
            {
                finalHeight = targetHeight.Value;
            }
            else
            {
                double aspectRatio = (double)source.Height / source.Width;
                finalHeight = (int)(targetWidth * aspectRatio);
            }

            var resized = source.Resize(new SKImageInfo(targetWidth, finalHeight), SKFilterQuality.High);

            _logger.LogDebug($"Resize aplicado: {source.Width}x{source.Height} -> {targetWidth}x{finalHeight}");

            return resized;
        }

        /// <summary>
        /// Redimensiona imagem por porcentagem (ex: 0.5 = 50% do tamanho original)
        /// </summary>
        public SKBitmap ResizeByPercentage(SKBitmap source, double percentage)
        {
            if (percentage <= 0)
                throw new ArgumentException("Porcentagem deve ser maior que zero", nameof(percentage));

            int targetWidth = (int)(source.Width * percentage);
            int targetHeight = (int)(source.Height * percentage);

            return Resize(source, targetWidth, targetHeight);
        }

        #endregion

        #region Split Operations

        /// <summary>
        /// Divide imagem verticalmente em duas partes (esquerda/direita)
        /// </summary>
        public (SKBitmap Left, SKBitmap Right) SplitVertical(SKBitmap source, double leftRatio = 0.666)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (leftRatio <= 0 || leftRatio >= 1)
                throw new ArgumentException("Ratio deve estar entre 0 e 1", nameof(leftRatio));

            int leftWidth = (int)(source.Width * leftRatio);
            int rightWidth = source.Width - leftWidth;

            var rectLeft = new SKRectI(0, 0, leftWidth, source.Height);
            var rectRight = new SKRectI(leftWidth, 0, source.Width, source.Height);

            _logger.LogDebug($"Split vertical: {leftRatio:P0}/{(1-leftRatio):P0} - {leftWidth}px / {rightWidth}px");

            return (Crop(source, rectLeft), Crop(source, rectRight));
        }

        /// <summary>
        /// Divide imagem horizontalmente em duas partes (topo/fundo)
        /// </summary>
        public (SKBitmap Top, SKBitmap Bottom) SplitHorizontal(SKBitmap source, double topRatio = 0.95)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (topRatio <= 0 || topRatio >= 1)
                throw new ArgumentException("Ratio deve estar entre 0 e 1", nameof(topRatio));

            int topHeight = (int)(source.Height * topRatio);
            int bottomHeight = source.Height - topHeight;

            var rectTop = new SKRectI(0, 0, source.Width, topHeight);
            var rectBottom = new SKRectI(0, topHeight, source.Width, source.Height);

            _logger.LogDebug($"Split horizontal: {topRatio:P0}/{(1-topRatio):P0} - {topHeight}px / {bottomHeight}px");

            return (Crop(source, rectTop), Crop(source, rectBottom));
        }

        #endregion

        #region Copy Operation

        /// <summary>
        /// Cria uma cópia independente do bitmap
        /// </summary>
        public SKBitmap Copy(SKBitmap source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Copy();
        }

        #endregion
    }
}
