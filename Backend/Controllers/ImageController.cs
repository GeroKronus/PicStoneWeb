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
            _logger.LogInformation("🔧 [CONSTRUCTOR] Iniciando ImageController");

            // ✨ CACHE: Pasta temp para armazenar imagens temporárias por usuário
            var currentDir = Directory.GetCurrentDirectory();
            _logger.LogInformation($"🔧 [CONSTRUCTOR] Current Directory: {currentDir}");

            _uploadsPath = Path.Combine(currentDir, "temp");
            _logger.LogInformation($"🔧 [CONSTRUCTOR] Tentando criar pasta temp: {_uploadsPath}");

            try
            {
                Directory.CreateDirectory(_uploadsPath);
                var dirExists = Directory.Exists(_uploadsPath);
                _logger.LogInformation($"✅ [CONSTRUCTOR] Pasta temp criada/verificada. Existe: {dirExists}, Path: {_uploadsPath}");
            }
            catch (Exception ex)
            {
                // ⚠️ FALLBACK: Se não conseguir criar temp, usa uploads/originals
                _logger.LogWarning(ex, $"⚠️ [CONSTRUCTOR] Não foi possível criar pasta temp: {_uploadsPath}. Usando fallback.");
                _uploadsPath = Path.Combine(currentDir, "uploads", "originals");
                _logger.LogInformation($"🔧 [CONSTRUCTOR] Tentando criar pasta fallback: {_uploadsPath}");
                try
                {
                    Directory.CreateDirectory(_uploadsPath);
                    var dirExists = Directory.Exists(_uploadsPath);
                    _logger.LogInformation($"✅ [CONSTRUCTOR] Pasta fallback criada. Existe: {dirExists}, Path: {_uploadsPath}");
                }
                catch (Exception exFallback)
                {
                    _logger.LogError(exFallback, $"❌ [CONSTRUCTOR] ERRO CRÍTICO: Não foi possível criar nem temp nem uploads/originals");
                    _logger.LogError($"❌ [CONSTRUCTOR] _uploadsPath final: {_uploadsPath}");
                    // Não faz throw - permite aplicação continuar (uploads falharão mas app não crashará)
                }
            }

            _logger.LogInformation($"🔧 [CONSTRUCTOR] ImageController inicializado. _uploadsPath = {_uploadsPath}");
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
                _logger.LogInformation("=== 📤 [UPLOAD] IMAGE UPLOAD REQUEST INICIADA ===");
                _logger.LogInformation($"📤 [UPLOAD] _uploadsPath configurado: {_uploadsPath}");
                _logger.LogInformation($"📤 [UPLOAD] _uploadsPath existe? {Directory.Exists(_uploadsPath)}");

                if (imagem == null || imagem.Length == 0)
                {
                    _logger.LogWarning("📤 [UPLOAD] ❌ Nenhuma imagem foi enviada (null ou length 0)");
                    return BadRequest(new { sucesso = false, mensagem = "Nenhuma imagem foi enviada" });
                }

                _logger.LogInformation($"📤 [UPLOAD] ✅ Imagem recebida: {imagem.FileName}, Tamanho: {imagem.Length} bytes, ContentType: {imagem.ContentType}");

                // Valida se é uma imagem válida
                _logger.LogInformation("📤 [UPLOAD] Iniciando decodificação da imagem com SkiaSharp...");
                SKBitmap imagemBitmap;
                try
                {
                    using (var stream = imagem.OpenReadStream())
                    {
                        _logger.LogInformation($"📤 [UPLOAD] Stream aberto. CanRead: {stream.CanRead}, Length: {stream.Length}");
                        imagemBitmap = SKBitmap.Decode(stream);
                    }
                }
                catch (Exception exDecode)
                {
                    _logger.LogError(exDecode, "📤 [UPLOAD] ❌ ERRO ao decodificar imagem com SkiaSharp");
                    throw;
                }

                if (imagemBitmap == null)
                {
                    _logger.LogWarning("📤 [UPLOAD] ❌ SKBitmap.Decode retornou null - imagem inválida");
                    return BadRequest(new { sucesso = false, mensagem = "Imagem inválida ou corrompida" });
                }

                _logger.LogInformation($"📤 [UPLOAD] ✅ Imagem decodificada com sucesso: {imagemBitmap.Width}x{imagemBitmap.Height}");

                // Gera ID único: userId_timestamp_guid
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
                var imageId = $"{usuarioId}_{timestamp}_{guid}.jpg";
                var caminhoCompleto = Path.Combine(_uploadsPath, imageId);

                _logger.LogInformation($"📤 [UPLOAD] ImageId gerado: {imageId}");
                _logger.LogInformation($"📤 [UPLOAD] Caminho completo para salvar: {caminhoCompleto}");
                _logger.LogInformation($"📤 [UPLOAD] Diretório do caminho existe? {Directory.Exists(Path.GetDirectoryName(caminhoCompleto))}");

                // Salva imagem com qualidade JPEG 95%
                _logger.LogInformation("📤 [UPLOAD] Iniciando salvamento do arquivo...");
                try
                {
                    using (var fileStream = System.IO.File.OpenWrite(caminhoCompleto))
                    {
                        _logger.LogInformation($"📤 [UPLOAD] FileStream aberto. CanWrite: {fileStream.CanWrite}");
                        using (var image = SKImage.FromBitmap(imagemBitmap))
                        {
                            _logger.LogInformation("📤 [UPLOAD] SKImage criado a partir do bitmap");
                            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
                            _logger.LogInformation($"📤 [UPLOAD] Imagem encodada. Data size: {data.Size} bytes");
                            data.SaveTo(fileStream);
                            _logger.LogInformation("📤 [UPLOAD] Dados salvos no FileStream");
                        }
                    }
                    _logger.LogInformation("📤 [UPLOAD] FileStream fechado");
                }
                catch (Exception exSave)
                {
                    _logger.LogError(exSave, $"📤 [UPLOAD] ❌ ERRO ao salvar arquivo em: {caminhoCompleto}");
                    throw;
                }

                // Verifica se arquivo foi salvo
                var fileExists = System.IO.File.Exists(caminhoCompleto);
                var fileSize = fileExists ? new FileInfo(caminhoCompleto).Length : 0;
                _logger.LogInformation($"📤 [UPLOAD] Arquivo existe após salvar? {fileExists}, Tamanho: {fileSize} bytes");

                imagemBitmap.Dispose();
                _logger.LogInformation("📤 [UPLOAD] Bitmap disposed");

                _logger.LogInformation($"📤 [UPLOAD] ✅✅✅ SUCESSO! Imagem salva: {imageId}");

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
                _logger.LogError(ex, "📤 [UPLOAD] ❌❌❌ ERRO FATAL ao fazer upload da imagem");
                _logger.LogError($"📤 [UPLOAD] Tipo da exceção: {ex.GetType().Name}");
                _logger.LogError($"📤 [UPLOAD] Mensagem: {ex.Message}");
                _logger.LogError($"📤 [UPLOAD] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"📤 [UPLOAD] InnerException: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { sucesso = false, mensagem = $"Erro interno: {ex.Message}", tipo = ex.GetType().Name });
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
        /// POST /api/image/test-upload
        /// TESTE: Recebe upload SEM processar a imagem (diagnóstico 502)
        /// </summary>
        [HttpPost("test-upload")]
        public IActionResult TestUpload([FromForm] IFormFile imagem)
        {
            try
            {
                _logger.LogInformation("🧪 [TEST] Test upload iniciado");

                if (imagem == null || imagem.Length == 0)
                {
                    _logger.LogWarning("🧪 [TEST] Imagem null ou vazia");
                    return BadRequest(new { sucesso = false, mensagem = "Sem imagem" });
                }

                _logger.LogInformation($"🧪 [TEST] Imagem recebida: {imagem.FileName}, {imagem.Length} bytes");

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Upload de teste OK - sem processamento",
                    fileName = imagem.FileName,
                    size = imagem.Length,
                    contentType = imagem.ContentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🧪 [TEST] Erro no test-upload");
                return StatusCode(500, new { sucesso = false, mensagem = ex.Message });
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
