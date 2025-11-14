using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Models;
using PicStoneFotoAPI.Services;
using SkiaSharp;
using System.Security.Claims;
using System.Text.Json;

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
        private readonly HistoryService _historyService;
        private readonly ILogger<MockupController> _logger;
        private readonly string _uploadsPath;

        public MockupController(MockupService mockupService, NichoService nichoService, BancadaService bancadaService, HistoryService historyService, ILogger<MockupController> logger)
        {
            _mockupService = mockupService;
            _nichoService = nichoService;
            _bancadaService = bancadaService;
            _historyService = historyService;
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

                // Registra geração no histórico
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Cavalete",
                    detalhes: $"{{\"tipo\":\"{request.TipoCavalete}\",\"fundo\":\"{request.Fundo}\"}}",
                    quantidadeImagens: response.CaminhosGerados.Count
                );

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

                // Salva as imagens geradas - obtém usuarioId para nomeação consistente
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                for (int i = 0; i < mockups.Count; i++)
                {
                    var sufixo = i == 0 ? "normal" : "rotacionado";
                    var nomeArquivo = $"nicho1_{sufixo}_User{usuarioId}.jpg";
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

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Nicho",
                    detalhes: $"{{\"fundo\":\"{(fundoEscuro ? "escuro" : "claro")}\",\"shampoo\":{incluirShampoo.ToString().ToLower()},\"sabonete\":{incluirSabonete.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                return Ok(new
                {
                    mensagem = "Mockups de nicho gerados com sucesso!",
                    ambientes = caminhos
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

                // Salva as imagens geradas - obtém usuarioId para nomeação consistente
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                for (int i = 0; i < mockups.Count; i++)
                {
                    var sufixo = i == 0 ? "normal" : "rotacionado";
                    var nomeArquivo = $"bancada1_{sufixo}_User{usuarioId}.jpg";
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

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Bancada1",
                    detalhes: $"{{\"flip\":{flip.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                return Ok(new
                {
                    mensagem = "Mockups de bancada gerados com sucesso!",
                    ambientes = caminhos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar mockup Bancada1");
                return StatusCode(500, new { mensagem = "Erro ao gerar mockup: " + ex.Message });
            }
        }

        /// <summary>
        /// POST /api/mockup/bancada2
        /// Gera mockup tipo Bancada2 (Countertop #2)
        /// </summary>
        [HttpPost("bancada2")]
        public async Task<IActionResult> GerarBancada2([FromForm] IFormFile imagem,
                                                        [FromForm] bool flip = false)
        {
            try
            {
                _logger.LogInformation("=== BANCADA2 REQUEST RECEBIDO ===");
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
                var mockups = await Task.Run(() => _bancadaService.GerarBancada2(imagemOriginal, flip));

                if (mockups == null || mockups.Count == 0)
                {
                    return StatusCode(500, new { mensagem = "Erro ao gerar mockups" });
                }

                _logger.LogInformation($"Mockups gerados: {mockups.Count} imagens");

                // Salva as imagens geradas - obtém usuarioId para nomeação consistente
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                for (int i = 0; i < mockups.Count; i++)
                {
                    var sufixo = i == 0 ? "normal" : "rotacionado";
                    var nomeArquivo = $"bancada2_{sufixo}_User{usuarioId}.jpg";
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
                    _logger.LogInformation($"Mockup Bancada2 salvo: {nomeArquivo}");
                }

                // Limpa bitmaps
                imagemOriginal.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Bancada2",
                    detalhes: $"{{\"flip\":{flip.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                return Ok(new
                {
                    mensagem = "Mockups de bancada #2 gerados com sucesso!",
                    ambientes = caminhos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar mockup Bancada2");
                return StatusCode(500, new { mensagem = "Erro ao gerar mockup: " + ex.Message });
            }
        }

        /// <summary>
        /// POST /api/mockup/bancada3
        /// Gera mockup tipo Bancada3 (Countertop #3)
        /// </summary>
        [HttpPost("bancada3")]
        public async Task<IActionResult> GerarBancada3([FromForm] IFormFile imagem, [FromForm] bool flip = false)
        {
            return await GerarBancadaGenerico(imagem, flip, 3, _bancadaService.GerarBancada3);
        }

        /// <summary>
        /// POST /api/mockup/bancada4
        /// Gera mockup tipo Bancada4 (Countertop #4)
        /// </summary>
        [HttpPost("bancada4")]
        public async Task<IActionResult> GerarBancada4([FromForm] IFormFile imagem, [FromForm] bool flip = false)
        {
            return await GerarBancadaGenerico(imagem, flip, 4, _bancadaService.GerarBancada4);
        }

        /// <summary>
        /// POST /api/mockup/bancada5
        /// Gera mockup tipo Bancada5 (Countertop #5) - 4 variações
        /// TAMBÉM salva imagem transformada (sem moldura) para download
        /// </summary>
        [HttpPost("bancada5")]
        public async Task<IActionResult> GerarBancada5([FromForm] IFormFile imagem, [FromForm] bool flip = false)
        {
            try
            {
                _logger.LogInformation("=== BANCADA5 REQUEST RECEBIDO ===");
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
                    return BadRequest(new { mensagem = "Formato de imagem inválido" });
                }

                _logger.LogInformation($"Imagem carregada: {imagemOriginal.Width}x{imagemOriginal.Height}");

                // 1. Gera os mockups normais com moldura
                var mockups = _bancadaService.GerarBancada5(imagemOriginal, flip);

                // 2. NOVO: Prepara diretório de debug
                string debugPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "bancada5_debug");
                Directory.CreateDirectory(debugPath);

                // 3. Salva a imagem cropada ORIGINAL (fornecida pelo usuário)
                string croppedOriginalPath = Path.Combine(debugPath, "bancada5_cropped_original.png");
                using (var fileStream = System.IO.File.OpenWrite(croppedOriginalPath))
                {
                    imagemOriginal.Encode(fileStream, SKEncodedImageFormat.Png, 100);
                }
                _logger.LogInformation($"Imagem cropada original salva em: {croppedOriginalPath}");

                // 4. Corta 2/3 DIREITOS da imagem (de 33.33% até 100% da largura)
                int umTerco = imagemOriginal.Width / 3;
                int doisTercosLargura = imagemOriginal.Width - umTerco; // Largura dos 2/3
                // IMPORTANTE: pega de 1/3 (umTerco) até o fim (imagemOriginal.Width)
                var rectDoisTercos = new SKRectI(umTerco, 0, imagemOriginal.Width, imagemOriginal.Height);

                var imagemDoisTercos = new SKBitmap(doisTercosLargura, imagemOriginal.Height);
                using (var canvas = new SKCanvas(imagemDoisTercos))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
                    canvas.DrawBitmap(imagemOriginal, rectDoisTercos, new SKRect(0, 0, doisTercosLargura, imagemOriginal.Height), paint);
                }

                // 5. Salva os 2/3 (antes da transformação)
                string doisTercosPath = Path.Combine(debugPath, "bancada5_dois_tercos.png");
                using (var fileStream = System.IO.File.OpenWrite(doisTercosPath))
                {
                    imagemDoisTercos.Encode(fileStream, SKEncodedImageFormat.Png, 100);
                }
                _logger.LogInformation($"Imagem 2/3 salva em: {doisTercosPath}");

                // 6. Aplica transformação nos 2/3
                var imagemTransformada = _bancadaService.MapToCustomQuadrilateral_Bancada3_TEST(
                    input: imagemDoisTercos,
                    canvasWidth: 1500,
                    canvasHeight: 1068,
                    v1x: 309, v1y: 623,    // topLeft
                    v2x: 670, v2y: 598,    // topRight
                    v4x: 309, v4y: 1036,   // bottomLeft
                    v3x: 669, v3y: 939     // bottomRight
                );

                // 7. Salva a imagem TRANSFORMADA (após aplicar perspectiva)
                string transformedFilePath = Path.Combine(debugPath, "bancada5_transformed.png");
                using (var fileStream = System.IO.File.OpenWrite(transformedFilePath))
                {
                    imagemTransformada.Encode(fileStream, SKEncodedImageFormat.Png, 100);
                }
                _logger.LogInformation($"Imagem transformada salva em: {transformedFilePath}");

                // 8. NOVA ESTRATÉGIA: Pega os mesmos 2/3, faz flip horizontal e aplica transformação com novas coordenadas
                // 8.1 - Flip horizontal dos 2/3
                var imagemDoisTercosFlipped = FlipHorizontal(imagemDoisTercos);
                _logger.LogInformation($"Flip horizontal aplicado nos 2/3: {imagemDoisTercosFlipped.Width}x{imagemDoisTercosFlipped.Height}");

                // 8.2 - Aplica transformação com as NOVAS coordenadas
                var imagemFlippedTransformada = _bancadaService.MapToCustomQuadrilateral_Bancada3_TEST(
                    input: imagemDoisTercosFlipped,
                    canvasWidth: 1500,
                    canvasHeight: 1068,
                    v1x: 670, v1y: 598,    // topLeft
                    v2x: 968, v2y: 577,    // topRight
                    v4x: 670, v4y: 937,    // bottomLeft
                    v3x: 975, v3y: 854     // bottomRight
                );

                // 8.3 - Salva a imagem FLIPADA + TRANSFORMADA
                string flippedTransformedPath = Path.Combine(debugPath, "bancada5_flipped_transformed.png");
                using (var fileStream = System.IO.File.OpenWrite(flippedTransformedPath))
                {
                    imagemFlippedTransformada.Encode(fileStream, SKEncodedImageFormat.Png, 100);
                }
                _logger.LogInformation($"Imagem flipada+transformada salva em: {flippedTransformedPath}");

                // 9. TERCEIRA ESTRATÉGIA: Pega o 1/3 ESQUERDO (0% a 33.33%) e aplica transformação
                // 9.1 - Recorta o 1/3 esquerdo
                var rectUmTerco = new SKRectI(0, 0, umTerco, imagemOriginal.Height);
                var imagemUmTerco = new SKBitmap(umTerco, imagemOriginal.Height);
                using (var canvas = new SKCanvas(imagemUmTerco))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
                    canvas.DrawBitmap(imagemOriginal, rectUmTerco, new SKRect(0, 0, umTerco, imagemOriginal.Height), paint);
                }
                _logger.LogInformation($"1/3 esquerdo recortado: {imagemUmTerco.Width}x{imagemUmTerco.Height}");

                // 9.2 - Aplica transformação com as coordenadas do 1/3 esquerdo
                var imagemUmTercoTransformada = _bancadaService.MapToCustomQuadrilateral_Bancada3_TEST(
                    input: imagemUmTerco,
                    canvasWidth: 1500,
                    canvasHeight: 1068,
                    v1x: 188, v1y: 601,    // topLeft
                    v2x: 309, v2y: 623,    // topRight
                    v4x: 196, v4y: 922,    // bottomLeft
                    v3x: 309, v3y: 1036    // bottomRight
                );

                // 9.3 - Salva a imagem do 1/3 TRANSFORMADA
                string umTercoTransformedPath = Path.Combine(debugPath, "bancada5_um_terco_transformed.png");
                using (var fileStream = System.IO.File.OpenWrite(umTercoTransformedPath))
                {
                    imagemUmTercoTransformada.Encode(fileStream, SKEncodedImageFormat.Png, 100);
                }
                _logger.LogInformation($"Imagem 1/3 transformada salva em: {umTercoTransformedPath}");

                // 3. Salva os mockups normais (com moldura) como de costume
                var caminhos = new List<string>();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

                for (int i = 0; i < mockups.Count; i++)
                {
                    string filename = $"bancada5_{timestamp}_{i + 1}.jpg";
                    string filepath = Path.Combine(uploadPath, filename);

                    using (var stream = System.IO.File.OpenWrite(filepath))
                    {
                        mockups[i].Encode(stream, SKEncodedImageFormat.Jpeg, 95);
                    }

                    caminhos.Add($"/uploads/{filename}");
                    _logger.LogInformation($"Mockup {i + 1} salvo: {filepath}");
                }

                // Limpa recursos
                imagemOriginal.Dispose();
                imagemDoisTercos.Dispose();
                imagemDoisTercosFlipped.Dispose();
                imagemTransformada.Dispose();
                imagemFlippedTransformada.Dispose();
                imagemUmTerco.Dispose();
                imagemUmTercoTransformada.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                // Registra geração no histórico
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Bancada5",
                    detalhes: $"{{\"flip\":{flip.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                // 4. Retorna resposta com URLs dos mockups E URLs das imagens de análise
                return Ok(new
                {
                    mensagem = $"Bancada 5 gerada com sucesso! {mockups.Count} mockups criados.",
                    ambientes = caminhos,
                    // URLs para análise detalhada (5 imagens)
                    imagemCroppedOriginal = "/api/mockup/bancada5-debug/cropped_original",
                    imagemDoisTercos = "/api/mockup/bancada5-debug/dois_tercos",
                    imagemTransformada = "/api/mockup/bancada5-debug/transformed",
                    imagemFlippedTransformada = "/api/mockup/bancada5-debug/flipped_transformed",
                    imagemUmTercoTransformada = "/api/mockup/bancada5-debug/um_terco_transformed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Bancada5");
                return StatusCode(500, new { mensagem = $"Erro ao gerar Bancada5: {ex.Message}" });
            }
        }

        /// <summary>
        /// POST /api/mockup/bancada6
        /// Gera mockup tipo Bancada6 (Countertop #6)
        /// </summary>
        [HttpPost("bancada6")]
        public async Task<IActionResult> GerarBancada6([FromForm] IFormFile imagem, [FromForm] bool flip = false)
        {
            return await GerarBancadaGenerico(imagem, flip, 6, _bancadaService.GerarBancada6);
        }

        /// <summary>
        /// POST /api/mockup/bancada7
        /// Gera mockup tipo Bancada7 (Countertop #7)
        /// </summary>
        [HttpPost("bancada7")]
        public async Task<IActionResult> GerarBancada7([FromForm] IFormFile imagem, [FromForm] bool flip = false)
        {
            return await GerarBancadaGenerico(imagem, flip, 7, _bancadaService.GerarBancada7);
        }

        /// <summary>
        /// POST /api/mockup/bancada8
        /// Gera mockup tipo Bancada8 (Countertop #8)
        /// </summary>
        [HttpPost("bancada8")]
        public async Task<IActionResult> GerarBancada8([FromForm] IFormFile imagem, [FromForm] bool flip = false)
        {
            return await GerarBancadaGenerico(imagem, flip, 8, _bancadaService.GerarBancada8);
        }

        /// <summary>
        /// Método genérico para processar bancadas (DRY)
        /// </summary>
        private async Task<IActionResult> GerarBancadaGenerico(IFormFile imagem, bool flip, int numeroBancada, Func<SKBitmap, bool, List<SKBitmap>> gerador)
        {
            try
            {
                _logger.LogInformation($"=== BANCADA{numeroBancada} REQUEST RECEBIDO ===");
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

                // Gera os mockups
                var mockups = await Task.Run(() => gerador(imagemOriginal, flip));

                if (mockups == null || mockups.Count == 0)
                {
                    return StatusCode(500, new { mensagem = "Erro ao gerar mockups" });
                }

                _logger.LogInformation($"Mockups gerados: {mockups.Count} imagens");

                // Salva as imagens geradas - obtém usuarioId para nomeação consistente
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                for (int i = 0; i < mockups.Count; i++)
                {
                    var sufixo = i == 0 ? "normal" : $"variacao{i}";
                    var nomeArquivo = $"bancada{numeroBancada}_{sufixo}_User{usuarioId}.jpg";
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
                    _logger.LogInformation($"Mockup Bancada{numeroBancada} salvo: {nomeArquivo}");
                }

                // Limpa bitmaps
                imagemOriginal.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: $"Bancada{numeroBancada}",
                    detalhes: $"{{\"flip\":{flip.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                return Ok(new
                {
                    mensagem = $"Mockups de bancada #{numeroBancada} gerados com sucesso!",
                    ambientes = caminhos  // Frontend espera "ambientes" não "mockups"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar mockup Bancada{numeroBancada}");
                return StatusCode(500, new { mensagem = "Erro ao gerar mockup: " + ex.Message });
            }
        }

        /// <summary>
        /// POST /api/mockup/bancada5-test
        /// Teste de transformação para Bancada 5 - Nova estratégia
        /// Corta metade da imagem e aplica MapToCustomQuadrilateral
        /// </summary>
        [HttpPost("bancada5-test")]
        public async Task<IActionResult> TestarBancada5([FromForm] IFormFile imagemCropada)
        {
            try
            {
                if (imagemCropada == null)
                {
                    return BadRequest(new { mensagem = "Imagem não fornecida" });
                }

                // Carrega a imagem
                using var stream = imagemCropada.OpenReadStream();
                var imagemOriginal = SKBitmap.Decode(stream);

                _logger.LogInformation($"Imagem recebida: {imagemOriginal.Width}x{imagemOriginal.Height}");

                // Corta metade da largura (50%)
                int metadeLargura = imagemOriginal.Width / 2;
                var rectMetade = new SKRectI(0, 0, metadeLargura, imagemOriginal.Height);

                var imagemMetade = new SKBitmap(metadeLargura, imagemOriginal.Height);
                using (var canvas = new SKCanvas(imagemMetade))
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
                    canvas.DrawBitmap(imagemOriginal, rectMetade, new SKRect(0, 0, metadeLargura, imagemOriginal.Height), paint);
                }

                _logger.LogInformation($"Metade cortada: {imagemMetade.Width}x{imagemMetade.Height}");

                // Aplica MapToCustomQuadrilateral com as coordenadas fornecidas
                // Canvas: 1500x1068
                // Coordenadas frente direita:
                // topLeft: (670, 598), topRight: (968, 577)
                // bottomRight: (975, 854), bottomLeft: (309, 1037)

                var resultado = _bancadaService.MapToCustomQuadrilateral_Bancada3_TEST(
                    input: imagemMetade,
                    canvasWidth: 1500,
                    canvasHeight: 1068,
                    v1x: 670, v1y: 598,    // topLeft
                    v2x: 968, v2y: 577,    // topRight
                    v4x: 309, v4y: 1037,   // bottomLeft
                    v3x: 975, v3y: 854     // bottomRight
                );

                _logger.LogInformation($"Resultado: {resultado.Width}x{resultado.Height}");

                // Salva resultado
                string debugPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "bancada5_debug");
                Directory.CreateDirectory(debugPath);
                string fileName = "bancada5_test_resultado.png";
                string filePath = Path.Combine(debugPath, fileName);

                using (var fileStream = System.IO.File.OpenWrite(filePath))
                {
                    resultado.Encode(fileStream, SKEncodedImageFormat.Png, 100);
                }

                _logger.LogInformation($"Arquivo salvo: {filePath}");

                // Limpa recursos
                imagemOriginal.Dispose();
                imagemMetade.Dispose();
                resultado.Dispose();

                return Ok(new
                {
                    mensagem = "Teste concluído com sucesso!",
                    downloadUrl = "/api/mockup/bancada5-debug/test_resultado",
                    dimensoesOriginal = $"{imagemCropada.Length} bytes",
                    dimensoesResultado = "1500x1068"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar Bancada 5");
                return StatusCode(500, new { mensagem = "Erro: " + ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// GET /api/mockup/bancada5-debug/{parte}
        /// Baixa as partes individuais da Bancada 5 para debug
        /// Partes: lateral, frenteDireita, frenteEsquerda, test_resultado, cropped_original, half, transformed
        /// </summary>
        [AllowAnonymous]
        [HttpGet("bancada5-debug/{parte}")]
        public IActionResult BaixarBancada5Debug(string parte)
        {
            try
            {
                string debugPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "bancada5_debug");
                string fileName = $"bancada5_{parte}.png";
                string filePath = Path.Combine(debugPath, fileName);

                _logger.LogInformation($"Tentando baixar: {filePath}");
                _logger.LogInformation($"Diretório existe? {Directory.Exists(debugPath)}");
                _logger.LogInformation($"Arquivo existe? {System.IO.File.Exists(filePath)}");

                if (Directory.Exists(debugPath))
                {
                    var arquivos = Directory.GetFiles(debugPath);
                    _logger.LogInformation($"Arquivos no diretório: {string.Join(", ", arquivos.Select(Path.GetFileName))}");
                }

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { mensagem = $"Arquivo {fileName} não encontrado. Gere primeiro uma Bancada 5." });
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "image/png", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao baixar parte da Bancada 5: {parte}");
                return StatusCode(500, new { mensagem = "Erro ao baixar arquivo: " + ex.Message });
            }
        }

        // ============================================================================
        // ENDPOINTS SSE PROGRESSIVOS (Server-Sent Events)
        // ============================================================================

        /// <summary>
        /// Helper: Carrega imagem do servidor (imageId) ou do upload (arquivo)
        /// </summary>
        /// <summary>
        /// Aplica crop em uma imagem se coordenadas foram fornecidas
        /// </summary>
        private SKBitmap? AplicarCrop(SKBitmap original, int? cropX, int? cropY, int? cropWidth, int? cropHeight)
        {
            // Se qualquer parâmetro de crop está faltando, retorna original
            if (!cropX.HasValue || !cropY.HasValue || !cropWidth.HasValue || !cropHeight.HasValue)
            {
                _logger.LogInformation("Nenhum crop especificado, usando imagem original completa");
                return original;
            }

            _logger.LogInformation($"Aplicando crop: x={cropX}, y={cropY}, width={cropWidth}, height={cropHeight}");

            // Valida coordenadas
            if (cropX.Value < 0 || cropY.Value < 0 || cropWidth.Value <= 0 || cropHeight.Value <= 0)
            {
                _logger.LogWarning("Coordenadas de crop inválidas, usando imagem original");
                return original;
            }

            if (cropX.Value + cropWidth.Value > original.Width || cropY.Value + cropHeight.Value > original.Height)
            {
                _logger.LogWarning("Crop excede dimensões da imagem original, usando imagem original");
                return original;
            }

            // Cria bitmap cropado
            var cropped = new SKBitmap(cropWidth.Value, cropHeight.Value);
            using (var canvas = new SKCanvas(cropped))
            {
                var srcRect = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                var destRect = new SKRectI(0, 0, cropWidth.Value, cropHeight.Value);
                canvas.DrawBitmap(original, srcRect, destRect);
            }

            _logger.LogInformation($"Crop aplicado com sucesso: {cropped.Width}x{cropped.Height}");
            return cropped;
        }

        private async Task<SKBitmap?> CarregarImagemAsync(string? imageId, IFormFile? imagem, int? cropX = null, int? cropY = null, int? cropWidth = null, int? cropHeight = null)
        {
            try
            {
                SKBitmap? bitmapOriginal = null;

                // Prioridade 1: Se imageId fornecido, carrega do servidor
                if (!string.IsNullOrWhiteSpace(imageId))
                {
                    _logger.LogInformation($"Carregando imagem do servidor: {imageId}");

                    // Valida imageId para evitar path traversal
                    if (imageId.Contains("..") || imageId.Contains("/") || imageId.Contains("\\"))
                    {
                        _logger.LogWarning($"ImageId inválido detectado: {imageId}");
                        return null;
                    }

                    // ✅ SEGURANÇA: Valida ownership - imageId deve ser ImgUser{userId}.jpg do próprio usuário
                    var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
                    var expectedImageId = $"ImgUser{usuarioId}.jpg";
                    if (imageId != expectedImageId)
                    {
                        _logger.LogWarning($"Tentativa de acessar imagem de outro usuário! UserId: {usuarioId}, ImageId: {imageId}, Expected: {expectedImageId}");
                        return null; // Retorna null para negar acesso silenciosamente
                    }

                    // ✅ CORRIGIDO: Carrega de wwwroot/images (onde ImageController salva)
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    var caminhoCompleto = Path.Combine(imagePath, imageId);

                    if (!System.IO.File.Exists(caminhoCompleto))
                    {
                        _logger.LogWarning($"Imagem não encontrada em wwwroot/images: {imageId}");
                        return null;
                    }

                    using var fileStream = System.IO.File.OpenRead(caminhoCompleto);
                    bitmapOriginal = SKBitmap.Decode(fileStream);
                    _logger.LogInformation($"Imagem carregada do servidor: {bitmapOriginal?.Width}x{bitmapOriginal?.Height}");
                }

                // Prioridade 2: Se arquivo fornecido, carrega do upload
                else if (imagem != null && imagem.Length > 0)
                {
                    _logger.LogInformation($"Carregando imagem do upload: {imagem.FileName}, {imagem.Length} bytes");
                    using var stream = imagem.OpenReadStream();
                    bitmapOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation($"Imagem carregada do upload: {bitmapOriginal?.Width}x{bitmapOriginal?.Height}");
                }

                if (bitmapOriginal == null)
                {
                    _logger.LogWarning("Nenhuma imagem fornecida (nem imageId nem arquivo)");
                    return null;
                }

                // ✨ OTIMIZAÇÃO: Aplica crop se coordenadas foram fornecidas
                return AplicarCrop(bitmapOriginal, cropX, cropY, cropWidth, cropHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar imagem");
                return null;
            }
        }

        /// <summary>
        /// POST /api/mockup/bancada1/progressive
        /// Gera mockup tipo Bancada1 com download progressivo via SSE
        /// </summary>
        [HttpPost("bancada1/progressive")]
        public async Task GerarBancada1Progressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? imagem,
            [FromForm] bool flip = false,
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            try
            {
                _logger.LogInformation("=== BANCADA1 PROGRESSIVE SSE REQUEST RECEBIDO ===");
                _logger.LogInformation($"ImageId: {imageId}, Flip: {flip}, Crop: x={cropX}, y={cropY}, w={cropWidth}, h={cropHeight}");

                // Configura response como SSE
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                // Envia evento de início
                await EnviarEventoSSE("start", new { mensagem = "Iniciando geração de mockups..." });

                // Carrega imagem (do servidor ou do upload) e aplica crop se fornecido
                var imagemOriginal = await CarregarImagemAsync(imageId, imagem, cropX, cropY, cropWidth, cropHeight);

                if (imagemOriginal == null)
                {
                    await EnviarEventoSSE("error", new { mensagem = "Não foi possível carregar a imagem" });
                    return;
                }

                _logger.LogInformation($"Imagem decodificada: {imagemOriginal.Width}x{imagemOriginal.Height}");

                // Gera os mockups (2 versões)
                var mockups = await Task.Run(() => _bancadaService.GerarBancada1(imagemOriginal, flip));

                if (mockups == null || mockups.Count == 0)
                {
                    await EnviarEventoSSE("error", new { mensagem = "Erro ao gerar mockups" });
                    imagemOriginal.Dispose();
                    return;
                }

                _logger.LogInformation($"Mockups gerados: {mockups.Count} imagens");

                // Obtém usuarioId para nomeação consistente dos arquivos
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Salva e envia cada mockup progressivamente
                for (int i = 0; i < mockups.Count; i++)
                {
                    // Envia evento de progresso
                    await EnviarEventoSSE("progress", new
                    {
                        index = i,
                        total = mockups.Count,
                        mensagem = $"Gerando mockup {i + 1}/{mockups.Count}..."
                    });

                    var sufixo = i == 0 ? "normal" : "rotacionado";
                    var nomeArquivo = $"bancada1_{sufixo}_User{usuarioId}.jpg";
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

                    var caminhoUrl = $"/uploads/mockups/{nomeArquivo}";
                    caminhos.Add(caminhoUrl);

                    _logger.LogInformation($"Mockup Bancada1 salvo: {nomeArquivo}");

                    // Envia evento de mockup completo
                    await EnviarEventoSSE("mockup", new
                    {
                        index = i,
                        total = mockups.Count,
                        url = caminhoUrl,
                        mensagem = $"Mockup {i + 1}/{mockups.Count} pronto!"
                    });
                }

                // Limpa bitmaps
                imagemOriginal.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Bancada1",
                    detalhes: $"{{\"flip\":{flip.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                // Envia evento de conclusão
                await EnviarEventoSSE("done", new
                {
                    total = mockups.Count,
                    caminhos = caminhos,
                    mensagem = "Todos os mockups foram gerados com sucesso!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar mockup Bancada1 Progressive");
                await EnviarEventoSSE("error", new { mensagem = "Erro ao gerar mockup: " + ex.Message });
            }
        }

        /// <summary>
        /// POST /api/mockup/gerar/progressive
        /// Gera mockup de cavalete com SSE progressivo (3 mockups sempre)
        /// </summary>
        [HttpPost("gerar/progressive")]
        public async Task GerarCavaleteProgressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? ImagemCropada,
            [FromForm] string? TipoCavalete,
            [FromForm] string? Fundo,
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            try
            {
                _logger.LogInformation("=== CAVALETE PROGRESSIVE SSE REQUEST RECEBIDO ===");
                _logger.LogInformation("ImageId: {ImageId}, TipoCavalete: {Tipo}, Fundo: {Fundo}, Crop: x={CropX}, y={CropY}, w={CropWidth}, h={CropHeight}",
                    imageId, TipoCavalete, Fundo, cropX, cropY, cropWidth, cropHeight);

                // Configura response como SSE
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                // Envia evento de início
                await EnviarEventoSSE("start", new { mensagem = "Iniciando geração de cavaletes..." });

                // Carrega a imagem (do servidor ou do upload) e aplica crop se fornecido
                var bitmapCropado = await CarregarImagemAsync(imageId, ImagemCropada, cropX, cropY, cropWidth, cropHeight);
                if (bitmapCropado == null)
                {
                    await EnviarEventoSSE("error", new { mensagem = "Não foi possível carregar a imagem" });
                    return;
                }

                var caminhos = new List<string>();

                // Gera SEMPRE os 3 mockups (como no endpoint original)

                // Define valores padrão
                var fundo = Fundo ?? "claro";
                var tipoCavalete = TipoCavalete ?? "simples";

                // Obtém usuarioId para nomeação consistente dos arquivos
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 1. CavaletePronto - Duplo: original à esquerda, espelho à direita
                await EnviarEventoSSE("progress", new { index = 0, total = 3, mensagem = "Gerando cavalete duplo (normal)..." });
                var caminhoDuplo1 = await _mockupService.GerarCavaleteDuplo(bitmapCropado, fundo, inverterLados: false, usuarioId: usuarioId);
                caminhos.Add($"/uploads/{caminhoDuplo1}");
                await EnviarEventoSSE("mockup", new { index = 0, total = 3, url = $"/uploads/{caminhoDuplo1}", mensagem = "Cavalete duplo 1/3 pronto!" });

                // 2. CavaletePronto2 - Duplo invertido: espelho à esquerda, original à direita
                await EnviarEventoSSE("progress", new { index = 1, total = 3, mensagem = "Gerando cavalete duplo (invertido)..." });
                var caminhoDuplo2 = await _mockupService.GerarCavaleteDuplo(bitmapCropado, fundo, inverterLados: true, usuarioId: usuarioId);
                caminhos.Add($"/uploads/{caminhoDuplo2}");
                await EnviarEventoSSE("mockup", new { index = 1, total = 3, url = $"/uploads/{caminhoDuplo2}", mensagem = "Cavalete duplo 2/3 pronto!" });

                // 3. CavaletePronto3 - Simples
                await EnviarEventoSSE("progress", new { index = 2, total = 3, mensagem = "Gerando cavalete simples..." });
                var caminhoSimples = await _mockupService.GerarCavaleteSimples(bitmapCropado, fundo, usuarioId: usuarioId);
                caminhos.Add($"/uploads/{caminhoSimples}");
                await EnviarEventoSSE("mockup", new { index = 2, total = 3, url = $"/uploads/{caminhoSimples}", mensagem = "Cavalete simples 3/3 pronto!" });

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Cavalete",
                    detalhes: $"{{\"tipo\":\"{tipoCavalete}\",\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                // Envia evento de conclusão
                await EnviarEventoSSE("done", new
                {
                    total = caminhos.Count,
                    caminhos = caminhos,
                    mensagem = "Todos os cavaletes foram gerados com sucesso!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Cavalete Progressive");
                await EnviarEventoSSE("error", new { mensagem = "Erro ao gerar cavalete: " + ex.Message });
            }
        }

        /// <summary>
        /// POST /api/mockup/nicho1/progressive
        /// Gera mockup Nicho1 com SSE progressivo (2 mockups: normal + rotacionado)
        /// </summary>
        [HttpPost("nicho1/progressive")]
        public async Task GerarNicho1Progressive([FromForm] string? imageId,
                                                   [FromForm] IFormFile? imagem,
                                                   [FromForm] bool fundoEscuro = false,
                                                   [FromForm] bool incluirShampoo = false,
                                                   [FromForm] bool incluirSabonete = false)
        {
            try
            {
                _logger.LogInformation("=== NICHO1 PROGRESSIVE SSE REQUEST RECEBIDO ===");
                _logger.LogInformation("ImageId: {ImageId}, Fundo: {Fundo}, Shampoo: {Shampoo}, Sabonete: {Sabonete}",
                    imageId, fundoEscuro ? "Escuro" : "Claro", incluirShampoo, incluirSabonete);

                // Configura response como SSE
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                // Envia evento de início
                await EnviarEventoSSE("start", new { mensagem = "Iniciando geração de nichos..." });

                // Carrega imagem (do servidor ou do upload)
                var imagemOriginal = await CarregarImagemAsync(imageId, imagem);

                if (imagemOriginal == null)
                {
                    await EnviarEventoSSE("error", new { mensagem = "Não foi possível carregar a imagem" });
                    return;
                }

                _logger.LogInformation("Imagem decodificada: {Width}x{Height}", imagemOriginal.Width, imagemOriginal.Height);

                // Gera os mockups (2 versões)
                var mockups = await Task.Run(() => _nichoService.GerarNicho1(imagemOriginal, fundoEscuro, incluirShampoo, incluirSabonete));

                if (mockups == null || mockups.Count == 0)
                {
                    await EnviarEventoSSE("error", new { mensagem = "Erro ao gerar mockups" });
                    imagemOriginal.Dispose();
                    return;
                }

                _logger.LogInformation("Mockups gerados: {Count} imagens", mockups.Count);

                // Obtém usuarioId para nomeação consistente dos arquivos
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Salva e envia cada mockup progressivamente
                for (int i = 0; i < mockups.Count; i++)
                {
                    // Envia evento de progresso
                    await EnviarEventoSSE("progress", new
                    {
                        index = i,
                        total = mockups.Count,
                        mensagem = $"Gerando nicho {i + 1}/{mockups.Count}..."
                    });

                    var sufixo = i == 0 ? "normal" : "rotacionado";
                    var nomeArquivo = $"nicho1_{sufixo}_User{usuarioId}.jpg";
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

                    var caminhoUrl = $"/uploads/mockups/{nomeArquivo}";
                    caminhos.Add(caminhoUrl);

                    _logger.LogInformation("Mockup Nicho1 salvo: {Caminho}", nomeArquivo);

                    // Envia evento de mockup completo
                    await EnviarEventoSSE("mockup", new
                    {
                        index = i,
                        total = mockups.Count,
                        url = caminhoUrl,
                        mensagem = $"Nicho {i + 1}/{mockups.Count} pronto!"
                    });
                }

                // Limpa bitmaps
                imagemOriginal.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Nicho",
                    detalhes: $"{{\"fundo\":\"{(fundoEscuro ? "escuro" : "claro")}\",\"shampoo\":{incluirShampoo.ToString().ToLower()},\"sabonete\":{incluirSabonete.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                // Envia evento de conclusão
                await EnviarEventoSSE("done", new
                {
                    total = mockups.Count,
                    caminhos = caminhos,
                    mensagem = "Todos os nichos foram gerados com sucesso!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Nicho1 Progressive");
                await EnviarEventoSSE("error", new { mensagem = "Erro ao gerar nicho: " + ex.Message });
            }
        }

        /// <summary>
        /// POST /api/mockup/bancada2/progressive
        /// </summary>
        [HttpPost("bancada2/progressive")]
        public async Task GerarBancada2Progressive([FromForm] string? imageId, [FromForm] IFormFile? imagem, [FromForm] bool flip = false, [FromForm] int? cropX = null, [FromForm] int? cropY = null, [FromForm] int? cropWidth = null, [FromForm] int? cropHeight = null)
        {
            await GerarBancadaProgressiveGenerico(imageId, imagem, flip, 2, _bancadaService.GerarBancada2, cropX, cropY, cropWidth, cropHeight);
        }

        /// <summary>
        /// POST /api/mockup/bancada3/progressive
        /// </summary>
        [HttpPost("bancada3/progressive")]
        public async Task GerarBancada3Progressive([FromForm] string? imageId, [FromForm] IFormFile? imagem, [FromForm] bool flip = false, [FromForm] int? cropX = null, [FromForm] int? cropY = null, [FromForm] int? cropWidth = null, [FromForm] int? cropHeight = null)
        {
            await GerarBancadaProgressiveGenerico(imageId, imagem, flip, 3, _bancadaService.GerarBancada3, cropX, cropY, cropWidth, cropHeight);
        }

        /// <summary>
        /// POST /api/mockup/bancada4/progressive
        /// </summary>
        [HttpPost("bancada4/progressive")]
        public async Task GerarBancada4Progressive([FromForm] string? imageId, [FromForm] IFormFile? imagem, [FromForm] bool flip = false, [FromForm] int? cropX = null, [FromForm] int? cropY = null, [FromForm] int? cropWidth = null, [FromForm] int? cropHeight = null)
        {
            await GerarBancadaProgressiveGenerico(imageId, imagem, flip, 4, _bancadaService.GerarBancada4, cropX, cropY, cropWidth, cropHeight);
        }

        /// <summary>
        /// POST /api/mockup/bancada5/progressive
        /// </summary>
        [HttpPost("bancada5/progressive")]
        public async Task GerarBancada5Progressive([FromForm] string? imageId, [FromForm] IFormFile? imagem, [FromForm] bool flip = false, [FromForm] int? cropX = null, [FromForm] int? cropY = null, [FromForm] int? cropWidth = null, [FromForm] int? cropHeight = null)
        {
            await GerarBancadaProgressiveGenerico(imageId, imagem, flip, 5, _bancadaService.GerarBancada5, cropX, cropY, cropWidth, cropHeight);
        }

        /// <summary>
        /// POST /api/mockup/bancada6/progressive
        /// </summary>
        [HttpPost("bancada6/progressive")]
        public async Task GerarBancada6Progressive([FromForm] string? imageId, [FromForm] IFormFile? imagem, [FromForm] bool flip = false, [FromForm] int? cropX = null, [FromForm] int? cropY = null, [FromForm] int? cropWidth = null, [FromForm] int? cropHeight = null)
        {
            await GerarBancadaProgressiveGenerico(imageId, imagem, flip, 6, _bancadaService.GerarBancada6, cropX, cropY, cropWidth, cropHeight);
        }

        /// <summary>
        /// POST /api/mockup/bancada7/progressive
        /// </summary>
        [HttpPost("bancada7/progressive")]
        public async Task GerarBancada7Progressive([FromForm] string? imageId, [FromForm] IFormFile? imagem, [FromForm] bool flip = false, [FromForm] int? cropX = null, [FromForm] int? cropY = null, [FromForm] int? cropWidth = null, [FromForm] int? cropHeight = null)
        {
            await GerarBancadaProgressiveGenerico(imageId, imagem, flip, 7, _bancadaService.GerarBancada7, cropX, cropY, cropWidth, cropHeight);
        }

        /// <summary>
        /// POST /api/mockup/bancada8/progressive
        /// </summary>
        [HttpPost("bancada8/progressive")]
        public async Task GerarBancada8Progressive([FromForm] string? imageId, [FromForm] IFormFile? imagem, [FromForm] bool flip = false, [FromForm] int? cropX = null, [FromForm] int? cropY = null, [FromForm] int? cropWidth = null, [FromForm] int? cropHeight = null)
        {
            await GerarBancadaProgressiveGenerico(imageId, imagem, flip, 8, _bancadaService.GerarBancada8, cropX, cropY, cropWidth, cropHeight);
        }

        /// <summary>
        /// Método genérico para gerar bancadas com SSE progressivo
        /// </summary>
        private async Task GerarBancadaProgressiveGenerico(string? imageId, IFormFile? imagem, bool flip, int numeroBancada, Func<SKBitmap, bool, List<SKBitmap>> gerador, int? cropX = null, int? cropY = null, int? cropWidth = null, int? cropHeight = null)
        {
            try
            {
                _logger.LogInformation($"=== BANCADA{numeroBancada} PROGRESSIVE SSE REQUEST RECEBIDO ===");
                _logger.LogInformation($"ImageId: {imageId}, Flip: {flip}, Crop: x={cropX}, y={cropY}, w={cropWidth}, h={cropHeight}");

                // Configura response como SSE
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                // Envia evento de início
                await EnviarEventoSSE("start", new { mensagem = "Iniciando geração de mockups..." });

                // Carrega imagem (do servidor ou do upload) e aplica crop se fornecido
                var imagemOriginal = await CarregarImagemAsync(imageId, imagem, cropX, cropY, cropWidth, cropHeight);

                if (imagemOriginal == null)
                {
                    await EnviarEventoSSE("error", new { mensagem = "Não foi possível carregar a imagem" });
                    return;
                }

                _logger.LogInformation($"Imagem decodificada: {imagemOriginal.Width}x{imagemOriginal.Height}");

                // Gera os mockups
                var mockups = await Task.Run(() => gerador(imagemOriginal, flip));

                if (mockups == null || mockups.Count == 0)
                {
                    await EnviarEventoSSE("error", new { mensagem = "Erro ao gerar mockups" });
                    imagemOriginal.Dispose();
                    return;
                }

                _logger.LogInformation($"Mockups gerados: {mockups.Count} imagens");

                // Obtém usuarioId para nomeação consistente dos arquivos
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Salva e envia cada mockup progressivamente
                for (int i = 0; i < mockups.Count; i++)
                {
                    // Envia evento de progresso
                    await EnviarEventoSSE("progress", new
                    {
                        index = i,
                        total = mockups.Count,
                        mensagem = $"Gerando mockup {i + 1}/{mockups.Count}..."
                    });

                    var sufixo = i == 0 ? "normal" : $"variacao{i}";
                    var nomeArquivo = $"bancada{numeroBancada}_{sufixo}_User{usuarioId}.jpg";
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

                    var caminhoUrl = $"/uploads/mockups/{nomeArquivo}";
                    caminhos.Add(caminhoUrl);

                    _logger.LogInformation($"Mockup Bancada{numeroBancada} salvo: {nomeArquivo}");

                    // Envia evento de mockup completo
                    await EnviarEventoSSE("mockup", new
                    {
                        index = i,
                        total = mockups.Count,
                        url = caminhoUrl,
                        mensagem = $"Mockup {i + 1}/{mockups.Count} pronto!"
                    });
                }

                // Limpa bitmaps
                imagemOriginal.Dispose();
                foreach (var mockup in mockups)
                {
                    mockup.Dispose();
                }

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: $"Bancada{numeroBancada}",
                    detalhes: $"{{\"flip\":{flip.ToString().ToLower()}}}",
                    quantidadeImagens: mockups.Count
                );

                // Envia evento de conclusão
                await EnviarEventoSSE("done", new
                {
                    total = mockups.Count,
                    caminhos = caminhos,
                    mensagem = "Todos os mockups foram gerados com sucesso!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar mockup Bancada{numeroBancada} Progressive");
                await EnviarEventoSSE("error", new { mensagem = "Erro ao gerar mockup: " + ex.Message });
            }
        }

        /// <summary>
        /// Método auxiliar para enviar eventos SSE
        /// </summary>
        private async Task EnviarEventoSSE(string tipo, object dados)
        {
            var json = JsonSerializer.Serialize(new { type = tipo, data = dados });
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }

        /// <summary>
        /// Faz flip horizontal (espelha) de um bitmap
        /// </summary>
        private SKBitmap FlipHorizontal(SKBitmap source)
        {
            var flipped = new SKBitmap(source.Width, source.Height);
            using var canvas = new SKCanvas(flipped);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };

            // Aplica transformação de espelhamento horizontal
            canvas.Scale(-1, 1, source.Width / 2f, source.Height / 2f);
            canvas.DrawBitmap(source, 0, 0, paint);

            return flipped;
        }
    }
}
