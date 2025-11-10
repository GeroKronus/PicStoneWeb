using Microsoft.AspNetCore.Http;
using SkiaSharp;
using System.IO;

namespace PicStoneFotoAPI.Services
{
    /// <summary>
    /// Service centralizado para validação de imagens
    /// WHY: Elimina duplicação - antes havia validações inline repetidas em 5+ lugares
    /// PATTERN: DRY + Single Responsibility - toda validação de imagem em um lugar
    /// </summary>
    public class ImageValidationService
    {
        private readonly ILogger<ImageValidationService> _logger;
        private static readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long _maxFileSize = 10 * 1024 * 1024; // 10MB

        public ImageValidationService(ILogger<ImageValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Valida arquivo de imagem (tamanho, extensão, MIME type)
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Validação falhou: arquivo vazio ou nulo");
                return (false, "Arquivo não fornecido ou vazio");
            }

            // Validar tamanho
            if (file.Length > _maxFileSize)
            {
                _logger.LogWarning($"Validação falhou: arquivo muito grande ({file.Length} bytes)");
                return (false, $"Arquivo excede o tamanho máximo de {_maxFileSize / 1024 / 1024}MB");
            }

            // Validar extensão
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
            {
                _logger.LogWarning($"Validação falhou: extensão inválida ({extension})");
                return (false, $"Tipo de arquivo não permitido. Use {string.Join(", ", _permittedExtensions.Select(e => e.ToUpper()))}");
            }

            // Validar MIME type
            var mimeType = file.ContentType.ToLowerInvariant();
            if (!mimeType.StartsWith("image/"))
            {
                _logger.LogWarning($"Validação falhou: MIME type inválido ({mimeType})");
                return (false, "O arquivo não é uma imagem válida");
            }

            _logger.LogDebug($"Arquivo validado com sucesso: {file.FileName} ({file.Length} bytes)");
            return (true, string.Empty);
        }

        /// <summary>
        /// Decodifica arquivo de imagem para SKBitmap
        /// </summary>
        public async Task<SKBitmap?> DecodeImageAsync(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var bitmap = SKBitmap.Decode(stream);

                if (bitmap == null)
                {
                    _logger.LogWarning($"Falha ao decodificar imagem: {file.FileName}");
                    return null;
                }

                _logger.LogDebug($"Imagem decodificada: {bitmap.Width}x{bitmap.Height}");
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao decodificar imagem: {file.FileName}");
                return null;
            }
        }

        /// <summary>
        /// Decodifica imagem Base64 para SKBitmap
        /// </summary>
        public SKBitmap? DecodeBase64Image(string base64Data)
        {
            try
            {
                // Remover prefixo "data:image/...;base64," se existir
                var base64String = base64Data;
                if (base64Data.Contains(","))
                {
                    base64String = base64Data.Split(',')[1];
                }

                byte[] imageBytes = Convert.FromBase64String(base64String);

                using var stream = new MemoryStream(imageBytes);
                var bitmap = SKBitmap.Decode(stream);

                if (bitmap == null)
                {
                    _logger.LogWarning("Falha ao decodificar imagem Base64");
                    return null;
                }

                _logger.LogDebug($"Imagem Base64 decodificada: {bitmap.Width}x{bitmap.Height}");
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao decodificar imagem Base64");
                return null;
            }
        }

        /// <summary>
        /// Valida dimensões mínimas da imagem
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateDimensions(SKBitmap bitmap, int minWidth = 100, int minHeight = 100)
        {
            if (bitmap == null)
                return (false, "Bitmap é nulo");

            if (bitmap.Width < minWidth || bitmap.Height < minHeight)
            {
                _logger.LogWarning($"Dimensões inválidas: {bitmap.Width}x{bitmap.Height} (mínimo: {minWidth}x{minHeight})");
                return (false, $"Imagem muito pequena. Dimensões mínimas: {minWidth}x{minHeight}px");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida aspect ratio da imagem
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateAspectRatio(SKBitmap bitmap, double minRatio = 0.5, double maxRatio = 2.0)
        {
            if (bitmap == null)
                return (false, "Bitmap é nulo");

            double ratio = (double)bitmap.Width / bitmap.Height;

            if (ratio < minRatio || ratio > maxRatio)
            {
                _logger.LogWarning($"Aspect ratio inválido: {ratio:F2} (permitido: {minRatio:F2} a {maxRatio:F2})");
                return (false, $"Proporções da imagem fora do permitido (aspect ratio: {ratio:F2})");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Valida arquivo E decodifica em uma única operação
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage, SKBitmap? Bitmap)> ValidateAndDecodeAsync(IFormFile file)
        {
            // Validar arquivo
            var (isValid, error) = ValidateImage(file);
            if (!isValid)
                return (false, error, null);

            // Decodificar
            var bitmap = await DecodeImageAsync(file);
            if (bitmap == null)
                return (false, "Falha ao decodificar a imagem", null);

            return (true, string.Empty, bitmap);
        }

        /// <summary>
        /// Validação completa: arquivo + dimensões + aspect ratio
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage, SKBitmap? Bitmap)> ValidateCompleteAsync(
            IFormFile file,
            int minWidth = 100,
            int minHeight = 100,
            double minRatio = 0.5,
            double maxRatio = 2.0)
        {
            // Validar e decodificar
            var (isValid, error, bitmap) = await ValidateAndDecodeAsync(file);
            if (!isValid)
                return (false, error, null);

            // Validar dimensões
            var (dimValid, dimError) = ValidateDimensions(bitmap!, minWidth, minHeight);
            if (!dimValid)
            {
                bitmap!.Dispose();
                return (false, dimError, null);
            }

            // Validar aspect ratio
            var (ratioValid, ratioError) = ValidateAspectRatio(bitmap!, minRatio, maxRatio);
            if (!ratioValid)
            {
                bitmap!.Dispose();
                return (false, ratioError, null);
            }

            return (true, string.Empty, bitmap);
        }

        /// <summary>
        /// Retorna informações do arquivo sem validar
        /// </summary>
        public (string FileName, long Size, string Extension, string MimeType) GetFileInfo(IFormFile file)
        {
            return (
                file.FileName,
                file.Length,
                Path.GetExtension(file.FileName).ToLowerInvariant(),
                file.ContentType.ToLowerInvariant()
            );
        }
    }
}
