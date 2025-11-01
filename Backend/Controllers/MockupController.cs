using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Models;
using PicStoneFotoAPI.Services;
using SkiaSharp;

namespace PicStoneFotoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MockupController : ControllerBase
    {
        private readonly MockupService _mockupService;
        private readonly NichoService _nichoService;
        private readonly BancadaService _bancadaService;
        private readonly ILogger<MockupController> _logger;
        private readonly string _uploadsPath;

        public MockupController(MockupService mockupService, NichoService nichoService, BancadaService bancadaService, ILogger<MockupController> logger)
        {
            _mockupService = mockupService;
            _nichoService = nichoService;
            _bancadaService = bancadaService;
            _logger = logger;
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "mockups");
            Directory.CreateDirectory(_uploadsPath);
        }

        /// <summary>
        /// POST /api/mockup/gerar
        /// Gera mockup de cavalete com a imagem cropada
        /// </summary>
        [HttpPost("gerar")]
        public async Task<IActionResult> GerarMockup([FromForm] MockupRequest request)
        {
            try
            {
                _logger.LogInformation("=== MOCKUP REQUEST RECEBIDO ===");
                _logger.LogInformation("TipoCavalete: {Tipo}", request.TipoCavalete);
                _logger.LogInformation("Fundo: {Fundo}", request.Fundo);
                _logger.LogInformation("ImagemCropada presente: {Presente}", request.ImagemCropada != null);

                if (request.ImagemCropada != null)
                {
                    _logger.LogInformation("Tamanho da imagem: {Tamanho} bytes", request.ImagemCropada.Length);
                    _logger.LogInformation("Nome do arquivo: {Nome}", request.ImagemCropada.FileName);
                    _logger.LogInformation("Content-Type: {Type}", request.ImagemCropada.ContentType);
                }

                var response = await _mockupService.GerarMockupAsync(request);

                _logger.LogInformation("Resposta do service - Sucesso: {Sucesso}, Mensagem: {Mensagem}",
                    response.Sucesso, response.Mensagem);

                if (!response.Sucesso)
                {
                    _logger.LogWarning("Retornando BadRequest: {Mensagem}", response.Mensagem);
                    return BadRequest(response);
                }

                _logger.LogInformation("Mockup gerado com sucesso! Caminhos: {Caminhos}",
                    string.Join(", ", response.CaminhosGerados));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXCEÇÃO ao processar requisição de mockup");
                return StatusCode(500, new MockupResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// POST /api/mockup/nicho1
        /// Gera mockup tipo Nicho1 (nicho de banheiro)
        /// </summary>
        [HttpPost("nicho1")]
        public async Task<IActionResult> GerarNicho1([FromForm] IFormFile imagem,
                                                       [FromForm] bool fundoEscuro = false,
                                                       [FromForm] bool incluirShampoo = false,
                                                       [FromForm] bool incluirSabonete = false)
        {
            try
            {
                _logger.LogInformation("=== NICHO1 REQUEST RECEBIDO ===");
                _logger.LogInformation("Fundo: {Fundo}, Shampoo: {Shampoo}, Sabonete: {Sabonete}",
                    fundoEscuro ? "Escuro" : "Claro", incluirShampoo, incluirSabonete);

                if (imagem == null || imagem.Length == 0)
                {
                    return BadRequest(new { mensagem = "Nenhuma imagem foi enviada" });
                }

                _logger.LogInformation("Tamanho da imagem: {Tamanho} bytes", imagem.Length);

                // Carrega imagem do usuário
                SKBitmap imagemOriginal;
                using (var stream = imagem.OpenReadStream())
                {
                    imagemOriginal = SKBitmap.Decode(stream);
                }

                if (imagemOriginal == null)
                {
                    return BadRequest(new { mensagem = "Não foi possível decodificar a imagem" });
                }

                _logger.LogInformation("Imagem decodificada: {Width}x{Height}", imagemOriginal.Width, imagemOriginal.Height);

                // Gera os mockups (2 versões)
                var mockups = await Task.Run(() => _nichoService.GerarNicho1(imagemOriginal, fundoEscuro, incluirShampoo, incluirSabonete));

                if (mockups == null || mockups.Count == 0)
                {
                    return StatusCode(500, new { mensagem = "Erro ao gerar mockups" });
                }

                _logger.LogInformation("Mockups gerados: {Count} imagens", mockups.Count);

                // Salva as imagens geradas
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var caminhos = new List<string>();

                for (int i = 0; i < mockups.Count; i++)
                {
                    var sufixo = i == 0 ? "normal" : "rotacionado";
                    var nomeArquivo = $"nicho1_{timestamp}_{sufixo}.jpg";
                    var caminhoCompleto = Path.Combine(_uploadsPath, nomeArquivo);

                    // Salva com qualidade JPEG 95%
                    using (var fileStream = System.IO.File.OpenWrite(caminhoCompleto))
                    {
                        using (var image = SKImage.FromBitmap(mockups[i]))
                        {
                            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
                            data.SaveTo(fileStream);
                        }
                    }

                    caminhos.Add($"/uploads/mockups/{nomeArquivo}");
                    _logger.LogInformation("Mockup Nicho1 salvo: {Caminho}", nomeArquivo);
                }

                // Limpa bitmaps
                imagemOriginal.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                return Ok(new
                {
                    mensagem = "Mockups de nicho gerados com sucesso!",
                    mockups = caminhos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar mockup Nicho1");
                return StatusCode(500, new { mensagem = "Erro ao gerar mockup: " + ex.Message });
            }
        }

        /// <summary>
        /// POST /api/mockup/bancada1
        /// Gera mockup tipo Bancada1 (Countertop #1)
        /// </summary>
        [HttpPost("bancada1")]
        public async Task<IActionResult> GerarBancada1([FromForm] IFormFile imagem,
                                                        [FromForm] bool flip = false)
        {
            try
            {
                _logger.LogInformation("=== BANCADA1 REQUEST RECEBIDO ===");
                _logger.LogInformation($"Flip: {flip}");

                if (imagem == null || imagem.Length == 0)
                {
                    return BadRequest(new { mensagem = "Nenhuma imagem foi enviada" });
                }

                _logger.LogInformation($"Tamanho da imagem: {imagem.Length} bytes");

                // Carrega imagem do usuário
                SKBitmap imagemOriginal;
                using (var stream = imagem.OpenReadStream())
                {
                    imagemOriginal = SKBitmap.Decode(stream);
                }

                if (imagemOriginal == null)
                {
                    return BadRequest(new { mensagem = "Não foi possível decodificar a imagem" });
                }

                _logger.LogInformation($"Imagem decodificada: {imagemOriginal.Width}x{imagemOriginal.Height}");

                // Gera os mockups (2 versões)
                var mockups = await Task.Run(() => _bancadaService.GerarBancada1(imagemOriginal, flip));

                if (mockups == null || mockups.Count == 0)
                {
                    return StatusCode(500, new { mensagem = "Erro ao gerar mockups" });
                }

                _logger.LogInformation($"Mockups gerados: {mockups.Count} imagens");

                // Salva as imagens geradas
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var caminhos = new List<string>();

                for (int i = 0; i < mockups.Count; i++)
                {
                    var sufixo = i == 0 ? "normal" : "rotacionado";
                    var nomeArquivo = $"bancada1_{timestamp}_{sufixo}.jpg";
                    var caminhoCompleto = Path.Combine(_uploadsPath, nomeArquivo);

                    // Salva com qualidade JPEG 95%
                    using (var fileStream = System.IO.File.OpenWrite(caminhoCompleto))
                    {
                        using (var image = SKImage.FromBitmap(mockups[i]))
                        {
                            var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
                            data.SaveTo(fileStream);
                        }
                    }

                    caminhos.Add($"/uploads/mockups/{nomeArquivo}");
                    _logger.LogInformation($"Mockup Bancada1 salvo: {nomeArquivo}");
                }

                // Limpa bitmaps
                imagemOriginal.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                return Ok(new
                {
                    mensagem = "Mockups de bancada gerados com sucesso!",
                    mockups = caminhos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar mockup Bancada1");
                return StatusCode(500, new { mensagem = "Erro ao gerar mockup: " + ex.Message });
            }
        }
    }
}
