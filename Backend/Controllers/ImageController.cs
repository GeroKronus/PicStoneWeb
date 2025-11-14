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

            // ✅ CORRIGIDO: Salva em wwwroot/images para ser acessível via URL estática
            var currentDir = Directory.GetCurrentDirectory();
            _logger.LogInformation($"🔧 [CONSTRUCTOR] Current Directory: {currentDir}");

            _uploadsPath = Path.Combine(currentDir, "wwwroot", "images");
            _logger.LogInformation($"🔧 [CONSTRUCTOR] Configurando pasta de uploads: {_uploadsPath}");

            try
            {
                Directory.CreateDirectory(_uploadsPath);
                var dirExists = Directory.Exists(_uploadsPath);
                _logger.LogInformation($"✅ [CONSTRUCTOR] Pasta wwwroot/images criada/verificada. Existe: {dirExists}, Path: {_uploadsPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [CONSTRUCTOR] ERRO ao criar pasta wwwroot/images: {_uploadsPath}");
                // Não faz throw - permite aplicação continuar
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

                // Gera nome fixo por usuário: ImgUser{userId}.jpg (sobrescreve anterior)
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
                var imageId = $"ImgUser{usuarioId}.jpg";
                var caminhoCompleto = Path.Combine(_uploadsPath, imageId);

                _logger.LogInformation($"📤 [UPLOAD] ImageId gerado: {imageId}");
                _logger.LogInformation($"📤 [UPLOAD] Caminho completo para salvar: {caminhoCompleto}");
                _logger.LogInformation($"📤 [UPLOAD] Diretório do caminho existe? {Directory.Exists(Path.GetDirectoryName(caminhoCompleto))}");

                // Deleta arquivo anterior se existir
                if (System.IO.File.Exists(caminhoCompleto))
                {
                    _logger.LogInformation($"📤 [UPLOAD] Arquivo anterior encontrado, será sobrescrito: {imageId}");
                    System.IO.File.Delete(caminhoCompleto);
                }

                // Salva imagem com qualidade JPEG 95%
                _logger.LogInformation("📤 [UPLOAD] Iniciando salvamento do arquivo...");
                try
                {
                    using (var fileStream = System.IO.File.Create(caminhoCompleto))
                    {
                        _logger.LogInformation($"📤 [UPLOAD] FileStream criado. CanWrite: {fileStream.CanWrite}");
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

                // Captura dimensões ANTES do dispose
                var largura = imagemBitmap.Width;
                var altura = imagemBitmap.Height;

                imagemBitmap.Dispose();
                _logger.LogInformation("📤 [UPLOAD] Bitmap disposed");

                _logger.LogInformation($"📤 [UPLOAD] ✅✅✅ SUCESSO! Imagem salva: {imageId}");

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Imagem enviada com sucesso",
                    imageId = imageId,
                    largura = largura,
                    altura = altura
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

                // ✨ SEGURANÇA: Valida ownership - imageId deve ser ImgUser{userId}.jpg do próprio usuário
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
                var expectedImageId = $"ImgUser{usuarioId}.jpg";
                if (imageId != expectedImageId)
                {
                    _logger.LogWarning($"Tentativa de deletar imagem de outro usuário! UserId: {usuarioId}, ImageId: {imageId}, Expected: {expectedImageId}");
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
        /// GET /api/image/debug/mockups
        /// Lista mockups gerados em uploads/mockups
        /// </summary>
        [AllowAnonymous]
        [HttpGet("debug/mockups")]
        public IActionResult DebugMockups()
        {
            try
            {
                var mockupsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "mockups");
                var exists = Directory.Exists(mockupsPath);

                var mockups = new List<object>();
                if (exists)
                {
                    var files = Directory.GetFiles(mockupsPath, "*.jpg");
                    foreach (var file in files.OrderByDescending(f => new FileInfo(f).LastWriteTime))
                    {
                        var info = new FileInfo(file);
                        mockups.Add(new
                        {
                            fileName = Path.GetFileName(file),
                            size = info.Length,
                            sizeKB = $"{info.Length / 1024}KB",
                            created = info.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            modified = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }

                return Ok(new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                    mockupsPath,
                    directoryExists = exists,
                    totalFiles = mockups.Count,
                    mockups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar mockups");
                return StatusCode(500, new { error = ex.Message });
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
