using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System.Security.Claims;

namespace PicStoneFotoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly string _uploadsPath;

        public ImageController(ILogger<ImageController> logger)
        {
            _logger = logger;
            // ✨ CACHE: Pasta temp para armazenar imagens temporárias por usuário
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");

            try
            {
                Directory.CreateDirectory(_uploadsPath);
                _logger.LogInformation($"✅ Pasta temp criada/verificada: {_uploadsPath}");
            }
            catch (Exception ex)
            {
                // ⚠️ FALLBACK: Se não conseguir criar temp, usa uploads/originals
                _logger.LogWarning(ex, $"⚠️ Não foi possível criar pasta temp: {_uploadsPath}. Usando fallback.");
                _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "originals");
                try
                {
                    Directory.CreateDirectory(_uploadsPath);
                    _logger.LogInformation($"✅ Pasta fallback criada: {_uploadsPath}");
                }
                catch (Exception exFallback)
                {
                    _logger.LogError(exFallback, $"❌ ERRO CRÍTICO: Não foi possível criar nem temp nem uploads/originals");
                    // Não faz throw - permite aplicação continuar (uploads falharão mas app não crashará)
                }
            }
        }

        /// <summary>
        /// POST /api/image/upload
        /// Faz upload da imagem original e retorna um ID único para reutilização
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile imagem)
        {
            try
            {
                _logger.LogInformation("=== IMAGE UPLOAD REQUEST ===");

                if (imagem == null || imagem.Length == 0)
                {
                    _logger.LogWarning("Nenhuma imagem foi enviada");
                    return BadRequest(new { sucesso = false, mensagem = "Nenhuma imagem foi enviada" });
                }

                _logger.LogInformation($"Imagem recebida: {imagem.FileName}, Tamanho: {imagem.Length} bytes");

                // Valida se é uma imagem válida
                SKBitmap imagemBitmap;
                using (var stream = imagem.OpenReadStream())
                {
                    imagemBitmap = SKBitmap.Decode(stream);
                }

                if (imagemBitmap == null)
                {
                    _logger.LogWarning("Não foi possível decodificar a imagem");
                    return BadRequest(new { sucesso = false, mensagem = "Imagem inválida ou corrompida" });
                }

                _logger.LogInformation($"Imagem decodificada: {imagemBitmap.Width}x{imagemBitmap.Height}");

                // Gera ID único: userId_timestamp_guid
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
                var imageId = $"{usuarioId}_{timestamp}_{guid}.jpg";
                var caminhoCompleto = Path.Combine(_uploadsPath, imageId);

                // Salva imagem com qualidade JPEG 95%
                using (var fileStream = System.IO.File.OpenWrite(caminhoCompleto))
                {
                    using (var image = SKImage.FromBitmap(imagemBitmap))
                    {
                        var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
                        data.SaveTo(fileStream);
                    }
                }

                imagemBitmap.Dispose();

                _logger.LogInformation($"Imagem salva com sucesso: {imageId}");

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Imagem enviada com sucesso",
                    imageId = imageId,
                    largura = imagemBitmap.Width,
                    altura = imagemBitmap.Height
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload da imagem");
                return StatusCode(500, new { sucesso = false, mensagem = $"Erro interno: {ex.Message}" });
            }
        }

        /// <summary>
        /// DELETE /api/image/{imageId}
        /// Remove a imagem original armazenada no servidor
        /// </summary>
        [HttpDelete("{imageId}")]
        public IActionResult DeleteImage(string imageId)
        {
            try
            {
                _logger.LogInformation($"=== IMAGE DELETE REQUEST: {imageId} ===");

                // Valida imageId para evitar path traversal
                if (string.IsNullOrWhiteSpace(imageId) || imageId.Contains("..") || imageId.Contains("/") || imageId.Contains("\\"))
                {
                    _logger.LogWarning($"ImageId inválido: {imageId}");
                    return BadRequest(new { sucesso = false, mensagem = "ID de imagem inválido" });
                }

                // ✨ SEGURANÇA: Valida ownership - imageId deve começar com userId do usuário logado
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
                if (!imageId.StartsWith($"{usuarioId}_"))
                {
                    _logger.LogWarning($"Tentativa de deletar imagem de outro usuário! UserId: {usuarioId}, ImageId: {imageId}");
                    return Forbid(); // 403 Forbidden
                }

                var caminhoCompleto = Path.Combine(_uploadsPath, imageId);

                if (!System.IO.File.Exists(caminhoCompleto))
                {
                    _logger.LogWarning($"Imagem não encontrada: {imageId}");
                    return NotFound(new { sucesso = false, mensagem = "Imagem não encontrada" });
                }

                // Deleta arquivo
                System.IO.File.Delete(caminhoCompleto);

                _logger.LogInformation($"Imagem deletada com sucesso: {imageId}");

                return Ok(new { sucesso = true, mensagem = "Imagem removida com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao deletar imagem: {imageId}");
                return StatusCode(500, new { sucesso = false, mensagem = $"Erro interno: {ex.Message}" });
            }
        }

        /// <summary>
        /// GET /api/image/health
        /// Verifica status do serviço de imagens
        /// </summary>
        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            try
            {
                var exists = Directory.Exists(_uploadsPath);
                var fileCount = exists ? Directory.GetFiles(_uploadsPath).Length : 0;

                return Ok(new
                {
                    status = "ok",
                    uploadsPath = _uploadsPath,
                    directoryExists = exists,
                    imageCount = fileCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar health do serviço");
                return StatusCode(500, new { status = "error", mensagem = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/image/debug/thumbs
        /// Verifica disponibilidade dos thumbnails WebP das bancadas
        /// </summary>
        [AllowAnonymous]
        [HttpGet("debug/thumbs")]
        public IActionResult DebugThumbs()
        {
            try
            {
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                var result = new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    wwwrootPath = wwwrootPath,
                    directoryExists = Directory.Exists(wwwrootPath),
                    thumbs = new List<object>()
                };

                if (!Directory.Exists(wwwrootPath))
                {
                    return Ok(new
                    {
                        result.timestamp,
                        result.wwwrootPath,
                        result.directoryExists,
                        error = "Diretório wwwroot/images não existe!"
                    });
                }

                var thumbsList = (List<object>)result.thumbs;

                for (int i = 1; i <= 8; i++)
                {
                    var fileName = $"thumb-bancada{i}.webp";
                    var filePath = Path.Combine(wwwrootPath, fileName);
                    var fileExists = System.IO.File.Exists(filePath);

                    var thumbInfo = new
                    {
                        bancada = i,
                        fileName = fileName,
                        exists = fileExists,
                        size = fileExists ? new FileInfo(filePath).Length : 0,
                        sizeKB = fileExists ? $"{new FileInfo(filePath).Length / 1024}KB" : "N/A",
                        fullPath = filePath,
                        urlPath = $"/images/{fileName}",
                        readable = fileExists && new FileInfo(filePath).Length > 0
                    };

                    thumbsList.Add(thumbInfo);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar thumbnails");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
