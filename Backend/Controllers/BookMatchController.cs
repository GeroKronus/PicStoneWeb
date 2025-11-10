using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Services;
using System.Security.Claims;

namespace PicStoneFotoAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookMatchController : ControllerBase
    {
        private readonly BookMatchService _bookMatchService;
        private readonly HistoryService _historyService;
        private readonly ILogger<BookMatchController> _logger;

        public BookMatchController(
            BookMatchService bookMatchService,
            HistoryService historyService,
            ILogger<BookMatchController> logger)
        {
            _bookMatchService = bookMatchService;
            _historyService = historyService;
            _logger = logger;
            _logger.LogInformation("[DEBUG] BookMatchController instantiated");
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateBookMatch([FromBody] BookMatchRequest request)
        {
            try
            {
                _logger.LogInformation("[DEBUG] GenerateBookMatch endpoint called");
                _logger.LogInformation("[DEBUG] Request received - CropX: {CropX}, CropY: {CropY}, CropWidth: {CropWidth}, CropHeight: {CropHeight}, TargetWidth: {TargetWidth}, ImageDataLength: {ImageDataLength}",
                    request.CropX, request.CropY, request.CropWidth, request.CropHeight, request.TargetWidth,
                    string.IsNullOrEmpty(request.ImageData) ? 0 : request.ImageData.Length);
                _logger.LogInformation("Gerando BookMatch");

                // Validar entrada
                if (string.IsNullOrEmpty(request.ImageData))
                {
                    return BadRequest(new { message = "Dados da imagem são obrigatórios" });
                }

                if (request.CropWidth <= 0 || request.CropHeight <= 0)
                {
                    _logger.LogError("[DEBUG] Invalid crop dimensions - CropWidth: {CropWidth}, CropHeight: {CropHeight}", request.CropWidth, request.CropHeight);
                    return BadRequest(new { message = "Área de seleção inválida" });
                }

                // Salvar imagem temporária
                _logger.LogInformation("[DEBUG] Saving temporary image from base64 data");
                string tempImagePath = await SaveBase64Image(request.ImageData);
                _logger.LogInformation("[DEBUG] Temporary image saved to: {TempImagePath}", tempImagePath);

                try
                {
                    // Gerar BookMatch
                    _logger.LogInformation("[DEBUG] Calling BookMatchService.GenerateBookMatch with TempImagePath: {TempImagePath}", tempImagePath);
                    var result = _bookMatchService.GenerateBookMatch(new BookMatchService.BookMatchRequest
                    {
                        ImagePath = tempImagePath,
                        CropX = request.CropX,
                        CropY = request.CropY,
                        CropWidth = request.CropWidth,
                        CropHeight = request.CropHeight,
                        TargetWidth = request.TargetWidth,
                        AddSeparatorLines = request.AddSeparatorLines
                    });

                    _logger.LogInformation("[DEBUG] BookMatch generation succeeded - MosaicPath: {MosaicPath}", result.MosaicPath);
                    _logger.LogInformation("BookMatch gerado com sucesso");

                    // Registra geração no histórico
                    var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    await _historyService.RegistrarAmbienteAsync(
                        usuarioId: usuarioId,
                        tipoAmbiente: "BookMatch",
                        detalhes: $"{{\"targetWidth\":{request.TargetWidth},\"separator\":{request.AddSeparatorLines.ToString().ToLower()}}}",
                        quantidadeImagens: 5  // mosaic + 4 quadrants
                    );

                    return Ok(new
                    {
                        success = true,
                        message = "BookMatch gerado com sucesso",
                        mosaic = ConvertToRelativePath(result.MosaicPath),
                        quadrant1 = ConvertToRelativePath(result.Quadrant1Path),
                        quadrant2 = ConvertToRelativePath(result.Quadrant2Path),
                        quadrant3 = ConvertToRelativePath(result.Quadrant3Path),
                        quadrant4 = ConvertToRelativePath(result.Quadrant4Path)
                    });
                }
                finally
                {
                    // Limpar arquivo temporário
                    if (System.IO.File.Exists(tempImagePath))
                    {
                        System.IO.File.Delete(tempImagePath);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Argumentos inválidos: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar BookMatch");
                return StatusCode(500, new { message = "Erro ao gerar BookMatch", error = ex.Message });
            }
        }

        private async Task<string> SaveBase64Image(string base64Data)
        {
            // Remover prefixo "data:image/...;base64," se existir
            var base64String = base64Data;
            if (base64Data.Contains(","))
            {
                base64String = base64Data.Split(',')[1];
            }

            byte[] imageBytes = Convert.FromBase64String(base64String);

            // Criar diretório temporário
            string tempDir = Path.Combine(Path.GetTempPath(), "bookmatch-temp");
            Directory.CreateDirectory(tempDir);

            // Salvar arquivo temporário
            string tempFile = Path.Combine(tempDir, $"temp-{Guid.NewGuid()}.jpg");
            await System.IO.File.WriteAllBytesAsync(tempFile, imageBytes);

            return tempFile;
        }

        private string ConvertToRelativePath(string absolutePath)
        {
            // Converter caminho absoluto para URL relativa
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (absolutePath.StartsWith(wwwrootPath))
            {
                var relativePath = absolutePath.Substring(wwwrootPath.Length)
                    .Replace("\\", "/")
                    .TrimStart('/');
                return "/" + relativePath;
            }
            return absolutePath;
        }
    }

    public class BookMatchRequest
    {
        public string ImageData { get; set; }  // Base64 image data
        public int CropX { get; set; }
        public int CropY { get; set; }
        public int CropWidth { get; set; }
        public int CropHeight { get; set; }
        public int TargetWidth { get; set; } = 800;
        public bool AddSeparatorLines { get; set; } = false;
    }
}
