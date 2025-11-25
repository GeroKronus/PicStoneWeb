using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Helpers;
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
        private readonly GraphicsTransformService _graphicsTransformService;
        private readonly LivingRoomService _livingRoomService;
        private readonly StairsService _stairsService;
        private readonly KitchenService _kitchenService;
        private readonly ImageWatermarkService _watermark;
        private readonly ILogger<MockupController> _logger;
        private readonly string _uploadsPath;

        public MockupController(MockupService mockupService, NichoService nichoService, BancadaService bancadaService, HistoryService historyService, GraphicsTransformService graphicsTransformService, LivingRoomService livingRoomService, StairsService stairsService, KitchenService kitchenService, ImageWatermarkService watermark, ILogger<MockupController> logger)
        {
            _mockupService = mockupService;
            _nichoService = nichoService;
            _bancadaService = bancadaService;
            _historyService = historyService;
            _graphicsTransformService = graphicsTransformService;
            _livingRoomService = livingRoomService;
            _stairsService = stairsService;
            _kitchenService = kitchenService;
            _watermark = watermark;
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
                    var nomeArquivo = FileNamingHelper.GenerateNichoFileName(1, sufixo, usuarioId);
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
                    var nomeArquivo = FileNamingHelper.GenerateBancadaFileName(1, sufixo, usuarioId);
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
                    var nomeArquivo = FileNamingHelper.GenerateBancadaFileName(2, sufixo, usuarioId);
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
                    var nomeArquivo = FileNamingHelper.GenerateBancadaFileName(numeroBancada, sufixo, usuarioId);
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
                    var nomeArquivo = FileNamingHelper.GenerateBancadaFileName(1, sufixo, usuarioId);

                    // Salva com cache-busting timestamp
                    var caminhoUrl = SalvarMockupComCacheBusting(mockups[i], nomeArquivo, "/uploads/mockups");
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
                var timestamp1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var urlDuplo1 = $"/uploads/{caminhoDuplo1}?v={timestamp1}";
                caminhos.Add(urlDuplo1);
                await EnviarEventoSSE("mockup", new { index = 0, total = 3, url = urlDuplo1, mensagem = "Cavalete duplo 1/3 pronto!" });

                // 2. CavaletePronto2 - Duplo invertido: espelho à esquerda, original à direita
                await EnviarEventoSSE("progress", new { index = 1, total = 3, mensagem = "Gerando cavalete duplo (invertido)..." });
                var caminhoDuplo2 = await _mockupService.GerarCavaleteDuplo(bitmapCropado, fundo, inverterLados: true, usuarioId: usuarioId);
                var timestamp2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var urlDuplo2 = $"/uploads/{caminhoDuplo2}?v={timestamp2}";
                caminhos.Add(urlDuplo2);
                await EnviarEventoSSE("mockup", new { index = 1, total = 3, url = urlDuplo2, mensagem = "Cavalete duplo 2/3 pronto!" });

                // 3. CavaletePronto3 - Simples
                await EnviarEventoSSE("progress", new { index = 2, total = 3, mensagem = "Gerando cavalete simples..." });
                var caminhoSimples = await _mockupService.GerarCavaleteSimples(bitmapCropado, fundo, usuarioId: usuarioId);
                var timestamp3 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var urlSimples = $"/uploads/{caminhoSimples}?v={timestamp3}";
                caminhos.Add(urlSimples);
                await EnviarEventoSSE("mockup", new { index = 2, total = 3, url = urlSimples, mensagem = "Cavalete simples 3/3 pronto!" });

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
                    var nomeArquivo = FileNamingHelper.GenerateNichoFileName(1, sufixo, usuarioId);

                    // Salva com cache-busting timestamp
                    var caminhoUrl = SalvarMockupComCacheBusting(mockups[i], nomeArquivo, "/uploads/mockups");
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
                    var nomeArquivo = FileNamingHelper.GenerateBancadaFileName(numeroBancada, sufixo, usuarioId);

                    // Salva com cache-busting timestamp
                    var caminhoUrl = SalvarMockupComCacheBusting(mockups[i], nomeArquivo, "/uploads/mockups");
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

        // ==================== BATHROOM PROGRESSIVE ====================

        [HttpPost("bathroom1/progressive")]
        public async Task GerarBathroom1Progressive(IFormFile imagemCropada, string fundo = "claro")
        {
            await GerarBathroomProgressiveGenerico(1, imagemCropada, fundo);
        }

        [HttpPost("bathroom2/progressive")]
        public async Task GerarBathroom2Progressive(IFormFile imagemCropada, string fundo = "claro")
        {
            await GerarBathroomProgressiveGenerico(2, imagemCropada, fundo);
        }

        private async Task GerarBathroomProgressiveGenerico(int numeroBathroom, IFormFile imagemCropada, string fundo)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Bathroom{Numero} Progressive ===", numeroBathroom);

                // Obtém usuarioId para nomeação consistente dos arquivos
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                await EnviarEventoSSE("inicio", new { mensagem = $"Gerando Bathroom #{numeroBathroom}..." });

                // Carrega imagem cropada
                using var streamCrop = new MemoryStream();
                await imagemCropada.CopyToAsync(streamCrop);
                streamCrop.Position = 0;

                using var bitmapCropado = SKBitmap.Decode(streamCrop);
                if (bitmapCropado == null)
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Erro ao decodificar imagem cropada" });
                    return;
                }

                // Parâmetros específicos de cada Bathroom (como no VB.NET original)
                int larguraMoldura, alturaMoldura, alturaQuadroSemSkew, larguraQuadroSemSkew;
                int ladoMaior, ladoMenor, primeiroPixelYNoQuadroSkew, coordPlotSkewX, coordPlotSkewY, fatorInclinacao;

                if (numeroBathroom == 1)
                {
                    // Bathroom 1 - Canvas 1440x1080 (do VB.NET Sub Banho1)
                    larguraMoldura = 1440;
                    alturaMoldura = 1080;
                    larguraQuadroSemSkew = 608;
                    alturaQuadroSemSkew = 686;
                    ladoMaior = 686;
                    ladoMenor = 554;
                    primeiroPixelYNoQuadroSkew = 199;
                    coordPlotSkewX = 800;
                    coordPlotSkewY = 131;
                    fatorInclinacao = 73;
                }
                else // Bathroom 2
                {
                    // Bathroom 2 - Canvas 1000x667 (horizontal)
                    larguraMoldura = 1000;
                    alturaMoldura = 667;
                    larguraQuadroSemSkew = 265;
                    alturaQuadroSemSkew = 575;
                    ladoMaior = 550;
                    ladoMenor = 340;
                    primeiroPixelYNoQuadroSkew = 155;
                    coordPlotSkewX = 535;
                    coordPlotSkewY = 0;
                    fatorInclinacao = 135;
                }

                // Redimensiona para largura fixa de 1500px
                const int tamanhoDoQuadro = 1500;
                float fatorDeAjuste = (float)bitmapCropado.Width / tamanhoDoQuadro;
                int novaAltura = (int)(bitmapCropado.Height / fatorDeAjuste);

                using var imagemRedimensionada = bitmapCropado.Resize(
                    new SKImageInfo(tamanhoDoQuadro, novaAltura),
                    SKFilterQuality.High);

                _logger.LogInformation("Imagem redimensionada: {W}x{H}", tamanhoDoQuadro, novaAltura);

                // Cria as 4 versões rotacionadas (como no VB.NET)
                using var bitmap90E = CriarBitmapRotacionado(imagemRedimensionada, SKEncodedOrigin.LeftBottom); // 90° sem flip
                using var bitmap90D = FlipHorizontal(bitmap90E); // 90° com flip
                using var bitmap270E = CriarBitmapRotacionado(imagemRedimensionada, SKEncodedOrigin.RightTop); // 270° sem flip
                using var bitmap270D = FlipHorizontal(bitmap270E); // 270° com flip

                // Cria os 4 mosaicos (quadrantes) como no VB.NET
                var larguraMosaico = (novaAltura * 2) + 1; // +1 para linha divisória opcional
                var alturaMosaico = tamanhoDoQuadro;

                var caminhos = new List<string>();

                // Gera os 4 quadrantes
                for (int quadrante = 1; quadrante <= 4; quadrante++)
                {
                    await EnviarEventoSSE("progresso", new {
                        etapa = $"Gerando quadrante {quadrante}/4",
                        porcentagem = (quadrante - 1) * 25
                    });

                    using var mosaico = new SKBitmap(larguraMosaico, alturaMosaico);
                    using var canvas = new SKCanvas(mosaico);
                    canvas.Clear(SKColors.White);

                    // Desenha as 2 chapas lado a lado conforme o quadrante
                    switch (quadrante)
                    {
                        case 1: // 90E + 90D
                            canvas.DrawBitmap(bitmap90E, 0, 0);
                            canvas.DrawBitmap(bitmap90D, novaAltura + 1, 0);
                            break;
                        case 2: // 90D + 90E (invertido)
                            canvas.DrawBitmap(bitmap90D, 0, 0);
                            canvas.DrawBitmap(bitmap90E, novaAltura + 1, 0);
                            break;
                        case 3: // 270E + 270D
                            canvas.DrawBitmap(bitmap270E, 0, 0);
                            canvas.DrawBitmap(bitmap270D, novaAltura + 1, 0);
                            break;
                        case 4: // 270D + 270E (invertido)
                            canvas.DrawBitmap(bitmap270D, 0, 0);
                            canvas.DrawBitmap(bitmap270E, novaAltura + 1, 0);
                            break;
                    }

                    // PASSO 1: Aplica DistortionInclina (redimensiona + skew)
                    _logger.LogInformation("Aplicando DistortionInclina ao quadrante {Q}...", quadrante);
                    using var quadranteDistorcido = _graphicsTransformService.DistortionInclina(
                        imagem: mosaico,
                        ladoMaior: ladoMaior,
                        ladoMenor: ladoMenor,
                        novaLargura: larguraQuadroSemSkew,
                        novaAltura: alturaQuadroSemSkew,
                        inclinacao: fatorInclinacao
                    );

                    _logger.LogInformation("Quadrante distorcido: {W}x{H}", quadranteDistorcido.Width, quadranteDistorcido.Height);

                    // PASSO 2: Cria canvas VAZIO (1440x1080px)
                    using var canvasBase = new SKBitmap(larguraMoldura, alturaMoldura);
                    using var canvasFinal = new SKCanvas(canvasBase);
                    canvasFinal.Clear(SKColors.Transparent); // Canvas transparente
                    _logger.LogInformation("Canvas vazio criado: {W}x{H}", larguraMoldura, alturaMoldura);

                    // PASSO 3: Plota transformações no canvas vazio
                    canvasFinal.DrawBitmap(quadranteDistorcido, 302, 187);
                    _logger.LogInformation("Primeira transformação plotada em (302, 187)");

                    canvasFinal.DrawBitmap(quadranteDistorcido, coordPlotSkewX, coordPlotSkewY);
                    _logger.LogInformation("Segunda transformação plotada em ({X}, {Y})", coordPlotSkewX, coordPlotSkewY);

                    // PASSO 4: Carrega moldura do banheiro como OVERLAY FINAL (camada por cima)
                    var caminhoBanho = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Banheiros", $"Banho{numeroBathroom}.webp");

                    if (System.IO.File.Exists(caminhoBanho))
                    {
                        using var banho = SKBitmap.Decode(caminhoBanho);
                        _logger.LogInformation("Banho{Num}.webp carregado: {W}x{H}", numeroBathroom, banho.Width, banho.Height);

                        // Desenha moldura POR CIMA em (0, 0)
                        canvasFinal.DrawBitmap(banho, 0, 0);
                        _logger.LogInformation("Banho{Num}.webp desenhado como overlay final em (0, 0)", numeroBathroom);
                    }
                    else
                    {
                        _logger.LogWarning("Banho{Num}.webp não encontrado: {Path}. Canvas sem moldura.", numeroBathroom, caminhoBanho);
                    }

                    // ✅ IMPERATIVO: Adiciona marca d'água (canto inferior direito)
                    _watermark.AddWatermark(canvasFinal, canvasBase.Width, canvasBase.Height);
                    _logger.LogInformation("Marca d'água adicionada ao quadrante {Q}", quadrante);

                    // PASSO 5: Salva o quadrante final com cache-busting
                    var nomeArquivo = FileNamingHelper.GenerateBathroomFileName(numeroBathroom, quadrante, fundo, usuarioId);

                    // Salva com cache-busting timestamp (sem prefixo de URL)
                    var caminhoComTimestamp = SalvarMockupComCacheBusting(canvasBase, nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Quadrante {Q} salvo: {Path}", quadrante, nomeArquivo);
                }

                await EnviarEventoSSE("sucesso", new {
                    mensagem = $"Bathroom #{numeroBathroom} gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Bathroom{Numero} Progressive ===", numeroBathroom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Bathroom{Numero}", numeroBathroom);
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cria bitmap rotacionado conforme orientação EXIF
        /// </summary>
        private SKBitmap CriarBitmapRotacionado(SKBitmap source, SKEncodedOrigin orientation)
        {
            var rotated = new SKBitmap(source.Height, source.Width); // Inverte dimensões para rotação 90/270
            using var canvas = new SKCanvas(rotated);

            switch (orientation)
            {
                case SKEncodedOrigin.LeftBottom: // 90° anti-horário
                    canvas.Translate(0, source.Width);
                    canvas.RotateDegrees(-90);
                    break;
                case SKEncodedOrigin.RightTop: // 90° horário (270° anti-horário)
                    canvas.Translate(source.Height, 0);
                    canvas.RotateDegrees(90);
                    break;
            }

            canvas.DrawBitmap(source, 0, 0);
            return rotated;
        }

        // ==================== FIM BATHROOM ====================

        // ==================== LIVING ROOM PROGRESSIVE ====================

        [HttpPost("livingroom1/progressive")]
        public async Task GerarLivingRoom1Progressive(
            [FromForm] string imageId,
            [FromForm] string fundo = "claro",
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Living Room #1 Progressive (imageId + crop) ===");

                // Obtém usuarioId para nomeação consistente dos arquivos
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                await EnviarEventoSSE("inicio", new { mensagem = "Gerando Living Room #1..." });

                // ✨ NOVA ARQUITETURA: Carrega imagem do servidor usando imageId (DRY com countertops)
                // ✅ CORRIGIDO: Carrega de wwwroot/images (mesmo local que ImageController salva e CarregarImagemAsync usa)
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                var caminhoImagemOriginal = Path.Combine(imagePath, imageId);

                if (!System.IO.File.Exists(caminhoImagemOriginal))
                {
                    _logger.LogError("Imagem não encontrada em wwwroot/images: {ImageId}", imageId);
                    await EnviarEventoSSE("erro", new { mensagem = $"Imagem não encontrada: {imageId}" });
                    return;
                }

                using var bitmapOriginal = SKBitmap.Decode(caminhoImagemOriginal);
                if (bitmapOriginal == null)
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Erro ao decodificar imagem original" });
                    return;
                }

                _logger.LogInformation("Imagem original carregada: {W}x{H}", bitmapOriginal.Width, bitmapOriginal.Height);

                // ✂️ Aplica crop se coordenadas foram fornecidas (CORRIGE BUG: estava usando imagem original)
                SKBitmap bitmapCropado;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    _logger.LogInformation("Aplicando crop: ({X},{Y}) {W}x{H}", cropX.Value, cropY.Value, cropWidth.Value, cropHeight.Value);

                    var info = new SKImageInfo(cropWidth.Value, cropHeight.Value);
                    bitmapCropado = new SKBitmap(info);

                    using var canvas = new SKCanvas(bitmapCropado);
                    var srcRect = new SKRect(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    var destRect = new SKRect(0, 0, cropWidth.Value, cropHeight.Value);
                    canvas.DrawBitmap(bitmapOriginal, srcRect, destRect);

                    _logger.LogInformation("Crop aplicado com sucesso: {W}x{H}", bitmapCropado.Width, bitmapCropado.Height);
                }
                else
                {
                    _logger.LogWarning("⚠️ Nenhuma coordenada de crop fornecida - usando imagem ORIGINAL");
                    bitmapCropado = bitmapOriginal.Copy();
                }

                _logger.LogInformation("Imagem final para processamento: {W}x{H}", bitmapCropado.Width, bitmapCropado.Height);

                // Gera os 4 quadrantes usando LivingRoomService
                await EnviarEventoSSE("progresso", new { etapa = "Processando transformações...", porcentagem = 10 });

                // NOTA: Não podemos fazer 'using' aqui porque o serviço retorna a lista e precisamos salvá-la primeiro
                var quadrantesBitmaps = _livingRoomService.GerarLivingRoom1(bitmapCropado);

                if (quadrantesBitmaps == null || quadrantesBitmaps.Count == 0)
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Erro ao gerar Living Room #1" });
                    return;
                }

                _logger.LogInformation("Living Room #1 gerado: {Count} quadrantes", quadrantesBitmaps.Count);

                var caminhos = new List<string>();

                // Salva e envia cada quadrante progressivamente
                for (int i = 0; i < quadrantesBitmaps.Count; i++)
                {
                    int quadrante = i + 1;

                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Salvando quadrante {quadrante}/4",
                        porcentagem = 10 + (quadrante * 20)
                    });

                    var nomeArquivo = FileNamingHelper.GenerateLivingRoomFileName(1, quadrante, fundo, usuarioId);

                    // Salva com cache-busting timestamp (sem prefixo de URL)
                    var caminhoComTimestamp = SalvarMockupComCacheBusting(quadrantesBitmaps[i], nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Quadrante {Q} salvo: {Path}", quadrante, nomeArquivo);
                }

                // Limpa bitmaps agora que foram salvos
                foreach (var bitmap in quadrantesBitmaps)
                {
                    bitmap.Dispose();
                }
                quadrantesBitmaps.Clear();

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "LivingRoom1",
                    detalhes: $"{{\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Living Room #1 gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Living Room #1 Progressive ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Living Room #1");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        [HttpPost("livingroom2/progressive")]
        public async Task GerarLivingRoom2Progressive(
            [FromForm] string imageId,
            [FromForm] string fundo = "claro",
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Living Room #2 Progressive (imageId + crop) ===");

                // Obtém usuarioId para nomeação consistente dos arquivos
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                await EnviarEventoSSE("inicio", new { mensagem = "Gerando Living Room #2..." });

                // ✨ NOVA ARQUITETURA: Carrega imagem do servidor usando imageId (DRY com countertops)
                // ✅ CORRIGIDO: Carrega de wwwroot/images (mesmo local que ImageController salva e CarregarImagemAsync usa)
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                var caminhoImagemOriginal = Path.Combine(imagePath, imageId);

                if (!System.IO.File.Exists(caminhoImagemOriginal))
                {
                    _logger.LogError("Imagem não encontrada em wwwroot/images: {ImageId}", imageId);
                    await EnviarEventoSSE("erro", new { mensagem = $"Imagem não encontrada: {imageId}" });
                    return;
                }

                using var bitmapOriginal = SKBitmap.Decode(caminhoImagemOriginal);
                if (bitmapOriginal == null)
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Erro ao decodificar imagem original" });
                    return;
                }

                _logger.LogInformation("Imagem original carregada: {W}x{H}", bitmapOriginal.Width, bitmapOriginal.Height);

                // ✂️ Aplica crop se coordenadas foram fornecidas (CORRIGE BUG: estava usando imagem original)
                SKBitmap bitmapCropado;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    _logger.LogInformation("Aplicando crop: ({X},{Y}) {W}x{H}", cropX.Value, cropY.Value, cropWidth.Value, cropHeight.Value);

                    var info = new SKImageInfo(cropWidth.Value, cropHeight.Value);
                    bitmapCropado = new SKBitmap(info);

                    using var canvas = new SKCanvas(bitmapCropado);
                    var srcRect = new SKRect(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    var destRect = new SKRect(0, 0, cropWidth.Value, cropHeight.Value);
                    canvas.DrawBitmap(bitmapOriginal, srcRect, destRect);

                    _logger.LogInformation("Crop aplicado com sucesso: {W}x{H}", bitmapCropado.Width, bitmapCropado.Height);
                }
                else
                {
                    _logger.LogWarning("⚠️ Nenhuma coordenada de crop fornecida - usando imagem ORIGINAL");
                    bitmapCropado = bitmapOriginal.Copy();
                }

                _logger.LogInformation("Imagem final para processamento: {W}x{H}", bitmapCropado.Width, bitmapCropado.Height);

                // Gera os 4 quadrantes usando LivingRoomService
                await EnviarEventoSSE("progresso", new { etapa = "Processando transformações...", porcentagem = 10 });

                // NOTA: Não podemos fazer 'using' aqui porque o serviço retorna a lista e precisamos salvá-la primeiro
                var quadrantesBitmaps = _livingRoomService.GerarLivingRoom2(bitmapCropado);

                if (quadrantesBitmaps == null || quadrantesBitmaps.Count == 0)
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Erro ao gerar Living Room #2" });
                    return;
                }

                _logger.LogInformation("Living Room #2 gerado: {Count} quadrantes", quadrantesBitmaps.Count);

                var caminhos = new List<string>();

                // Salva e envia cada quadrante progressivamente
                for (int i = 0; i < quadrantesBitmaps.Count; i++)
                {
                    int quadrante = i + 1;

                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Salvando quadrante {quadrante}/4",
                        porcentagem = 10 + (quadrante * 20)
                    });

                    var nomeArquivo = FileNamingHelper.GenerateLivingRoomFileName(2, quadrante, fundo, usuarioId);

                    // Salva com cache-busting timestamp (sem prefixo de URL)
                    var caminhoComTimestamp = SalvarMockupComCacheBusting(quadrantesBitmaps[i], nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Quadrante {Q} salvo: {Path}", quadrante, nomeArquivo);
                }

                // Limpa bitmaps agora que foram salvos
                foreach (var bitmap in quadrantesBitmaps)
                {
                    bitmap.Dispose();
                }
                quadrantesBitmaps.Clear();

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "LivingRoom2",
                    detalhes: $"{{\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Living Room #2 gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Living Room #2 Progressive ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Living Room #2");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        // ==================== FIM LIVING ROOM ====================

        // ==================== STAIRS ====================

        [HttpPost("stairs1/progressive")]
        public async Task GerarStairs1Progressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? imagem,
            [FromForm] string fundo = "claro",
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Stairs #1 Progressive (imageId + crop) ===");

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Carregando imagem...",
                    porcentagem = 5
                });

                // Carrega imagem (igual LivingRoom)
                SKBitmap imagemOriginal;
                if (!string.IsNullOrEmpty(imageId))
                {
                    var caminhoImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageId);
                    using var stream = System.IO.File.OpenRead(caminhoImagem);
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem carregada do servidor: {ImageId}", imageId);
                }
                else if (imagem != null)
                {
                    using var stream = imagem.OpenReadStream();
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem recebida do upload");
                }
                else
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Nenhuma imagem fornecida" });
                    return;
                }

                _logger.LogInformation("Imagem original carregada: {Width}x{Height}", imagemOriginal.Width, imagemOriginal.Height);

                // Crop se necessário
                SKBitmap imagemFinal;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    _logger.LogInformation("Aplicando crop: x={X}, y={Y}, w={W}, h={H}", cropX, cropY, cropWidth, cropHeight);
                    var rectCrop = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    imagemFinal = new SKBitmap(cropWidth.Value, cropHeight.Value);
                    imagemOriginal.ExtractSubset(imagemFinal, rectCrop);
                    imagemOriginal.Dispose();
                    _logger.LogInformation("Imagem cropada: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);
                }
                else
                {
                    _logger.LogWarning("⚠️ Nenhuma coordenada de crop fornecida - usando imagem ORIGINAL");
                    imagemFinal = imagemOriginal;
                }

                _logger.LogInformation("Imagem final para processamento: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);

                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Gera 2 versões: normal e rotacionada
                for (int versao = 1; versao <= 2; versao++)
                {
                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Gerando escada versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 30 : 60)
                    });

                    bool rotacionado = versao == 2;
                    var mockup = _stairsService.GerarStairs1(imagemFinal, rotacionado);

                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Salvando versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 35 : 85)
                    });

                    var sufixo = versao == 1 ? "normal" : "rotate";
                    var nomeArquivo = FileNamingHelper.GenerateStairsFileName(1, sufixo, fundo, usuarioId);

                    // Salva com cache-busting timestamp (sem prefixo de URL)
                    var caminhoComTimestamp = SalvarMockupComCacheBusting(mockup, nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Stairs versão {V} salvo: {Path}", versao, nomeArquivo);

                    mockup.Dispose();
                }

                imagemFinal.Dispose();

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Stairs1",
                    detalhes: $"{{\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Stairs #1 gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Stairs #1 Progressive ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Stairs #1");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        [HttpPost("stairs2/progressive")]
        public async Task GerarStairs2Progressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? imagem,
            [FromForm] string fundo = "claro",
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Stairs #2 Progressive (imageId + crop) ===");

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Carregando imagem...",
                    porcentagem = 5
                });

                // Carrega imagem (igual LivingRoom)
                SKBitmap imagemOriginal;
                if (!string.IsNullOrEmpty(imageId))
                {
                    var caminhoImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageId);
                    using var stream = System.IO.File.OpenRead(caminhoImagem);
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem carregada do servidor: {ImageId}", imageId);
                }
                else if (imagem != null)
                {
                    using var stream = imagem.OpenReadStream();
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem recebida do upload");
                }
                else
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Nenhuma imagem fornecida" });
                    return;
                }

                _logger.LogInformation("Imagem original carregada: {Width}x{Height}", imagemOriginal.Width, imagemOriginal.Height);

                // Crop se necessário
                SKBitmap imagemFinal;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    _logger.LogInformation("Aplicando crop: x={X}, y={Y}, w={W}, h={H}", cropX, cropY, cropWidth, cropHeight);
                    var rectCrop = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    imagemFinal = new SKBitmap(cropWidth.Value, cropHeight.Value);
                    imagemOriginal.ExtractSubset(imagemFinal, rectCrop);
                    imagemOriginal.Dispose();
                    _logger.LogInformation("Imagem cropada: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);
                }
                else
                {
                    _logger.LogWarning("⚠️ Nenhuma coordenada de crop fornecida - usando imagem ORIGINAL");
                    imagemFinal = imagemOriginal;
                }

                _logger.LogInformation("Imagem final para processamento: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);

                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Gera 2 versões: normal e rotacionada
                for (int versao = 1; versao <= 2; versao++)
                {
                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Gerando escada versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 30 : 60)
                    });

                    bool rotacionado = versao == 2;
                    var mockup = _stairsService.GerarStairs2(imagemFinal, rotacionado);

                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Salvando versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 35 : 85)
                    });

                    var sufixo = versao == 1 ? "normal" : "rotate";
                    var nomeArquivo = FileNamingHelper.GenerateStairsFileName(2, sufixo, fundo, usuarioId);

                    // Salva com cache-busting timestamp (sem prefixo de URL)
                    var caminhoComTimestamp = SalvarMockupComCacheBusting(mockup, nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Stairs versão {V} salvo: {Path}", versao, nomeArquivo);

                    mockup.Dispose();
                }

                imagemFinal.Dispose();

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Stairs2",
                    detalhes: $"{{\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Stairs #2 gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Stairs #2 Progressive ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Stairs #2");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        [HttpPost("stairs3/progressive")]
        public async Task GerarStairs3Progressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? imagem,
            [FromForm] string fundo = "claro",
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Stairs #3 Progressive (imageId + crop) ===");

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Carregando imagem...",
                    porcentagem = 5
                });

                // Carrega imagem (igual LivingRoom)
                SKBitmap imagemOriginal;
                if (!string.IsNullOrEmpty(imageId))
                {
                    var caminhoImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageId);
                    using var stream = System.IO.File.OpenRead(caminhoImagem);
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem carregada do servidor: {ImageId}", imageId);
                }
                else if (imagem != null)
                {
                    using var stream = imagem.OpenReadStream();
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem recebida do upload");
                }
                else
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Nenhuma imagem fornecida" });
                    return;
                }

                _logger.LogInformation("Imagem original carregada: {Width}x{Height}", imagemOriginal.Width, imagemOriginal.Height);

                // Crop se necessário
                SKBitmap imagemFinal;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    _logger.LogInformation("Aplicando crop: x={X}, y={Y}, w={W}, h={H}", cropX, cropY, cropWidth, cropHeight);
                    var rectCrop = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    imagemFinal = new SKBitmap(cropWidth.Value, cropHeight.Value);
                    imagemOriginal.ExtractSubset(imagemFinal, rectCrop);
                    imagemOriginal.Dispose();
                    _logger.LogInformation("Imagem cropada: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);
                }
                else
                {
                    _logger.LogWarning("⚠️ Nenhuma coordenada de crop fornecida - usando imagem ORIGINAL");
                    imagemFinal = imagemOriginal;
                }

                _logger.LogInformation("Imagem final para processamento: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);

                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Gera 2 versões: normal e rotacionada
                for (int versao = 1; versao <= 2; versao++)
                {
                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Gerando escada versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 30 : 60)
                    });

                    bool rotacionado = versao == 2;
                    // TODO: Implementar GerarStairs3
                    var mockup = _stairsService.GerarStairs1(imagemFinal, rotacionado);

                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Salvando versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 35 : 85)
                    });

                    var sufixo = versao == 1 ? "normal" : "rotate";
                    var nomeArquivo = FileNamingHelper.GenerateStairsFileName(3, sufixo, fundo, usuarioId);

                    // Salva com cache-busting timestamp (sem prefixo de URL)
                    var caminhoComTimestamp = SalvarMockupComCacheBusting(mockup, nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Stairs versão {V} salvo: {Path}", versao, nomeArquivo);

                    mockup.Dispose();
                }

                imagemFinal.Dispose();

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Stairs3",
                    detalhes: $"{{\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Stairs #3 gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Stairs #3 Progressive ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Stairs #3");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        [HttpPost("stairs4/progressive")]
        public async Task GerarStairs4Progressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? imagem,
            [FromForm] string fundo = "claro",
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Stairs #4 Progressive (imageId + crop) ===");

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Carregando imagem...",
                    porcentagem = 5
                });

                // Carrega imagem (igual LivingRoom)
                SKBitmap imagemOriginal;
                if (!string.IsNullOrEmpty(imageId))
                {
                    var caminhoImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageId);
                    using var stream = System.IO.File.OpenRead(caminhoImagem);
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem carregada do servidor: {ImageId}", imageId);
                }
                else if (imagem != null)
                {
                    using var stream = imagem.OpenReadStream();
                    imagemOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem recebida do upload");
                }
                else
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Nenhuma imagem fornecida" });
                    return;
                }

                _logger.LogInformation("Imagem original carregada: {Width}x{Height}", imagemOriginal.Width, imagemOriginal.Height);

                // Crop se necessário
                SKBitmap imagemFinal;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    _logger.LogInformation("Aplicando crop: x={X}, y={Y}, w={W}, h={H}", cropX, cropY, cropWidth, cropHeight);
                    var rectCrop = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    imagemFinal = new SKBitmap(cropWidth.Value, cropHeight.Value);
                    imagemOriginal.ExtractSubset(imagemFinal, rectCrop);
                    imagemOriginal.Dispose();
                    _logger.LogInformation("Imagem cropada: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);
                }
                else
                {
                    _logger.LogWarning("⚠️ Nenhuma coordenada de crop fornecida - usando imagem ORIGINAL");
                    imagemFinal = imagemOriginal;
                }

                _logger.LogInformation("Imagem final para processamento: {Width}x{Height}", imagemFinal.Width, imagemFinal.Height);

                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Gera 2 versões: normal e rotacionada
                for (int versao = 1; versao <= 2; versao++)
                {
                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Gerando escada versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 30 : 60)
                    });

                    bool rotacionado = versao == 2;
                    // TODO: Reimplementar GerarStairs4 sem TransformacaoConfig
                    var mockup = _stairsService.GerarStairs2(imagemFinal, rotacionado);

                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Salvando versão {versao}/2...",
                        porcentagem = 10 + (versao == 1 ? 35 : 85)
                    });

                    var sufixo = versao == 1 ? "normal" : "rotate";
                    var nomeArquivo = FileNamingHelper.GenerateStairsFileName(4, sufixo, fundo, usuarioId);

                    // Salva com cache-busting timestamp (sem prefixo de URL)
                    var caminhoComTimestamp = SalvarMockupComCacheBusting(mockup, nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Stairs versão {V} salvo: {Path}", versao, nomeArquivo);

                    mockup.Dispose();
                }

                imagemFinal.Dispose();

                // Registra geração no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Stairs4",
                    detalhes: $"{{\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Stairs #4 gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Stairs #4 Progressive ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Stairs #4");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        // ==================== FIM STAIRS ====================

        // ==================== KITCHEN PROGRESSIVE ====================

        [HttpPost("kitchen1/progressive")]
        public async Task GerarKitchen1Progressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? image,
            [FromForm] string fundo = "claro",
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Kitchen #1 Progressive ===");

                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                await EnviarEventoSSE("inicio", new { mensagem = "Gerando Kitchen #1..." });

                // Carrega imagem (igual Stairs - aceita imageId OU IFormFile)
                SKBitmap bitmapOriginal;
                if (!string.IsNullOrEmpty(imageId))
                {
                    var caminhoImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageId);
                    if (!System.IO.File.Exists(caminhoImagem))
                    {
                        _logger.LogError("Imagem não encontrada: {ImageId}", imageId);
                        await EnviarEventoSSE("erro", new { mensagem = $"Imagem não encontrada: {imageId}" });
                        return;
                    }
                    using var stream = System.IO.File.OpenRead(caminhoImagem);
                    bitmapOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem carregada do servidor: {ImageId}", imageId);
                }
                else if (image != null)
                {
                    using var stream = image.OpenReadStream();
                    bitmapOriginal = SKBitmap.Decode(stream);
                    _logger.LogInformation("Imagem recebida do upload: {FileName}", image.FileName);
                }
                else
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Nenhuma imagem fornecida" });
                    return;
                }

                if (bitmapOriginal == null)
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Erro ao decodificar imagem original" });
                    return;
                }

                _logger.LogInformation("Imagem original carregada: {W}x{H}", bitmapOriginal.Width, bitmapOriginal.Height);

                // Aplica crop se coordenadas foram fornecidas
                SKBitmap bitmapCropado;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    _logger.LogInformation("Aplicando crop: ({X},{Y}) {W}x{H}", cropX.Value, cropY.Value, cropWidth.Value, cropHeight.Value);

                    var rectCrop = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    bitmapCropado = new SKBitmap(cropWidth.Value, cropHeight.Value);
                    bitmapOriginal.ExtractSubset(bitmapCropado, rectCrop);
                    bitmapOriginal.Dispose();

                    _logger.LogInformation("Crop aplicado: {W}x{H}", bitmapCropado.Width, bitmapCropado.Height);
                }
                else
                {
                    _logger.LogWarning("Nenhuma coordenada de crop - usando imagem original");
                    bitmapCropado = bitmapOriginal;
                }

                await EnviarEventoSSE("progresso", new { etapa = "Processando transformações...", porcentagem = 10 });

                // Gera as 2 versões usando KitchenService
                var mockupsBitmaps = _kitchenService.GerarKitchen1(bitmapCropado);

                if (mockupsBitmaps == null || mockupsBitmaps.Count == 0)
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Erro ao gerar Kitchen #1" });
                    return;
                }

                _logger.LogInformation("Kitchen #1 gerado: {Count} versões", mockupsBitmaps.Count);

                var caminhos = new List<string>();

                // Salva e envia cada versão progressivamente
                for (int i = 0; i < mockupsBitmaps.Count; i++)
                {
                    int versao = i + 1;

                    await EnviarEventoSSE("progresso", new
                    {
                        etapa = $"Salvando versão {versao}/{mockupsBitmaps.Count}",
                        porcentagem = 10 + (versao * 40)
                    });

                    var sufixo = versao == 1 ? "normal" : "rotate";
                    var nomeArquivo = FileNamingHelper.GenerateKitchenFileName(1, sufixo, fundo, usuarioId);

                    var caminhoComTimestamp = SalvarMockupComCacheBusting(mockupsBitmaps[i], nomeArquivo);
                    caminhos.Add(caminhoComTimestamp);
                    _logger.LogInformation("Versão {V} salva: {Path}", versao, nomeArquivo);
                }

                // Limpa bitmaps
                foreach (var bitmap in mockupsBitmaps)
                {
                    bitmap.Dispose();
                }
                mockupsBitmaps.Clear();
                bitmapCropado.Dispose();

                // Registra no histórico
                await _historyService.RegistrarAmbienteAsync(
                    usuarioId: usuarioId,
                    tipoAmbiente: "Kitchen1",
                    detalhes: $"{{\"fundo\":\"{fundo}\"}}",
                    quantidadeImagens: caminhos.Count
                );

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Kitchen #1 gerado com sucesso!",
                    caminhos = caminhos
                });

                _logger.LogInformation("=== FIM Kitchen #1 Progressive ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Kitchen #1");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
        }

        // ==================== FIM KITCHEN ====================

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
        /// Salva um mockup com cache-busting timestamp e retorna o caminho relativo com query string
        /// </summary>
        /// <param name="bitmap">Bitmap a ser salvo</param>
        /// <param name="nomeArquivo">Nome do arquivo (ex: "nicho1_normal_User1.jpg")</param>
        /// <param name="prefixoUrl">Prefixo da URL (ex: "/uploads/mockups" ou null para retornar só o nome)</param>
        /// <returns>Caminho com timestamp (ex: "/uploads/mockups/nicho1_normal_User1.jpg?v=1763321819989")</returns>
        private string SalvarMockupComCacheBusting(SKBitmap bitmap, string nomeArquivo, string? prefixoUrl = null)
        {
            var caminhoCompleto = Path.Combine(_uploadsPath, nomeArquivo);

            // ✅ FIX: Deleta arquivo antigo se existir (evita erro de arquivo bloqueado)
            if (System.IO.File.Exists(caminhoCompleto))
            {
                try
                {
                    System.IO.File.Delete(caminhoCompleto);
                    _logger.LogInformation("Arquivo antigo deletado: {Path}", caminhoCompleto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Não foi possível deletar arquivo antigo: {Path}", caminhoCompleto);
                    // Continua mesmo se não conseguir deletar (tentará sobrescrever)
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
            using var outputStream = System.IO.File.OpenWrite(caminhoCompleto);
            data.SaveTo(outputStream);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nomeComTimestamp = $"{nomeArquivo}?v={timestamp}";

            return prefixoUrl != null
                ? $"{prefixoUrl}/{nomeComTimestamp}"
                : nomeComTimestamp;
        }

        /// <summary>
        /// ENDPOINT DE TESTE: Processa apenas degrau1 e espelho1
        /// </summary>
        [HttpPost("test/degrau1-espelho1")]
        public IActionResult TestarDegrau1Espelho1(IFormFile imagem)
        {
            try
            {
                if (imagem == null || imagem.Length == 0)
                    return BadRequest("Nenhuma imagem fornecida");

                using var stream = imagem.OpenReadStream();
                using var imagemOriginal = SKBitmap.Decode(stream);

                var resultado = _stairsService.TestarDegrau1Espelho1(imagemOriginal);

                // Retornar a imagem salva em debug
                var debugPath = Path.Combine("wwwroot", "debug", "test_degrau1_espelho1.jpg");
                return Ok(new {
                    success = true,
                    debugPath = $"/debug/test_degrau1_espelho1.jpg?v={DateTime.Now.Ticks}",
                    message = "Teste executado! Veja o resultado em /debug/test_degrau1_espelho1.jpg"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no teste degrau1+espelho1");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("teste1/progressive")]
        public async Task GerarTeste1Progressive(
            [FromForm] string? imageId,
            [FromForm] IFormFile? imagem,
            [FromForm] int? cropX = null,
            [FromForm] int? cropY = null,
            [FromForm] int? cropWidth = null,
            [FromForm] int? cropHeight = null)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("=== INÍCIO Teste #1 Progressive ===");

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Carregando imagem...",
                    porcentagem = 5
                });

                // Carrega imagem
                SKBitmap imagemOriginal;
                if (!string.IsNullOrEmpty(imageId))
                {
                    var caminhoImagem = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageId);
                    using var stream = System.IO.File.OpenRead(caminhoImagem);
                    imagemOriginal = SKBitmap.Decode(stream);
                }
                else if (imagem != null)
                {
                    using var stream = imagem.OpenReadStream();
                    imagemOriginal = SKBitmap.Decode(stream);
                }
                else
                {
                    await EnviarEventoSSE("erro", new { mensagem = "Nenhuma imagem fornecida" });
                    return;
                }

                // Crop se necessário
                SKBitmap imagemFinal;
                if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                {
                    var rectCrop = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                    imagemFinal = new SKBitmap(cropWidth.Value, cropHeight.Value);
                    imagemOriginal.ExtractSubset(imagemFinal, rectCrop);
                    imagemOriginal.Dispose();
                }
                else
                {
                    imagemFinal = imagemOriginal;
                }

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Recortando porções da imagem...",
                    porcentagem = 20
                });

                // === PORÇÃO 1: 66% largura x 45% altura (do TOPO) ===
                int largura66 = (int)(imagemFinal.Width * 0.66f);
                int altura45 = (int)(imagemFinal.Height * 0.45f);
                int altura5 = (int)(imagemFinal.Height * 0.05f);

                var porcao1 = new SKBitmap(largura66, altura45);
                using (var canvas = new SKCanvas(porcao1))
                {
                    var srcRect = new SKRectI(0, 0, largura66, altura45);
                    canvas.DrawBitmap(imagemFinal, srcRect, new SKRect(0, 0, largura66, altura45));
                }
                _logger.LogInformation("Porção 1 (66%x45%): {Width}x{Height}", porcao1.Width, porcao1.Height);

                // === PORÇÃO 2: 66% largura x 5% altura (de 45% a 50%) ===
                var porcao2 = new SKBitmap(largura66, altura5);
                using (var canvas = new SKCanvas(porcao2))
                {
                    var srcRect = new SKRectI(0, altura45, largura66, altura45 + altura5);
                    canvas.DrawBitmap(imagemFinal, srcRect, new SKRect(0, 0, largura66, altura5));
                }
                _logger.LogInformation("Porção 2 (66%x5%): {Width}x{Height}", porcao2.Width, porcao2.Height);

                // === PORÇÃO 3: 5% largura x 50% altura (de Y=50% até Y=100%) - COLUNA ===
                int largura5 = (int)(imagemFinal.Width * 0.05f);
                int altura50 = (int)(imagemFinal.Height * 0.50f);
                int yInicio50 = (int)(imagemFinal.Height * 0.50f);

                var porcao3 = new SKBitmap(largura5, altura50);
                using (var canvas = new SKCanvas(porcao3))
                {
                    var srcRect = new SKRectI(0, yInicio50, largura5, yInicio50 + altura50);
                    canvas.DrawBitmap(imagemFinal, srcRect, new SKRect(0, 0, largura5, altura50));
                }
                _logger.LogInformation("Porção 3 (coluna 5%x50%): {Width}x{Height}", porcao3.Width, porcao3.Height);

                // === PORÇÃO 4: 30% largura (X=5% a 35%) x 50% altura (Y=50% a 100%) - COLUNA COM PARALELOGRAMO ===
                int xInicio5percent = (int)(imagemFinal.Width * 0.05f);
                int largura30 = (int)(imagemFinal.Width * 0.30f);

                var porcao4 = new SKBitmap(largura30, altura50);
                using (var canvas = new SKCanvas(porcao4))
                {
                    var srcRect = new SKRectI(xInicio5percent, yInicio50, xInicio5percent + largura30, yInicio50 + altura50);
                    canvas.DrawBitmap(imagemFinal, srcRect, new SKRect(0, 0, largura30, altura50));
                }
                _logger.LogInformation("Porção 4 (30%x50%): {Width}x{Height}", porcao4.Width, porcao4.Height);

                // === PORÇÃO 5: X=45% até 100%, Y=45% até 100% (55% largura x 55% altura) ===
                int xInicio45percent = (int)(imagemFinal.Width * 0.45f);
                int yInicio45percent = (int)(imagemFinal.Height * 0.45f);
                int largura55 = imagemFinal.Width - xInicio45percent;
                int altura55 = imagemFinal.Height - yInicio45percent;

                var porcao5 = new SKBitmap(largura55, altura55);
                using (var canvas = new SKCanvas(porcao5))
                {
                    var srcRect = new SKRectI(xInicio45percent, yInicio45percent, imagemFinal.Width, imagemFinal.Height);
                    canvas.DrawBitmap(imagemFinal, srcRect, new SKRect(0, 0, largura55, altura55));
                }
                _logger.LogInformation("Porção 5 (55%x55%): {Width}x{Height}", porcao5.Width, porcao5.Height);

                // === PORÇÃO 6: 55% largura x 50% altura (do TOPO) ===
                int largura55p = (int)(imagemFinal.Width * 0.55f);
                int altura50p = (int)(imagemFinal.Height * 0.50f);

                var porcao6 = new SKBitmap(largura55p, altura50p);
                using (var canvas = new SKCanvas(porcao6))
                {
                    var srcRect = new SKRectI(0, 0, largura55p, altura50p);
                    canvas.DrawBitmap(imagemFinal, srcRect, new SKRect(0, 0, largura55p, altura50p));
                }
                _logger.LogInformation("Porção 6 (55%x50% topo): {Width}x{Height}", porcao6.Width, porcao6.Height);

                // === PORÇÃO 7: 55% largura x 50% altura (do TOPO) - será flip horizontal ===
                var porcao7 = new SKBitmap(largura55p, altura50p);
                using (var canvas = new SKCanvas(porcao7))
                {
                    var srcRect = new SKRectI(0, 0, largura55p, altura50p);
                    canvas.DrawBitmap(imagemFinal, srcRect, new SKRect(0, 0, largura55p, altura50p));
                }
                _logger.LogInformation("Porção 7 (55%x50% topo): {Width}x{Height}", porcao7.Width, porcao7.Height);

                imagemFinal.Dispose();

                // DEBUG: Salva porção 1 antes da transformação
                var debugPath1 = Path.Combine(_uploadsPath, "debug_porcao1_antes_transform.png");
                using (var imgDebug = SKImage.FromBitmap(porcao1))
                using (var dataDebug = imgDebug.Encode(SKEncodedImageFormat.Png, 100))
                using (var streamDebug = System.IO.File.OpenWrite(debugPath1))
                {
                    dataDebug.SaveTo(streamDebug);
                }
                _logger.LogInformation("DEBUG: Porção 1 salva em {Path}", debugPath1);

                // DEBUG: Salva porção 2
                var debugPath2 = Path.Combine(_uploadsPath, "debug_porcao2.png");
                using (var imgDebug = SKImage.FromBitmap(porcao2))
                using (var dataDebug = imgDebug.Encode(SKEncodedImageFormat.Png, 100))
                using (var streamDebug = System.IO.File.OpenWrite(debugPath2))
                {
                    dataDebug.SaveTo(streamDebug);
                }
                _logger.LogInformation("DEBUG: Porção 2 salva em {Path}", debugPath2);

                // DEBUG: Salva porção 3
                var debugPath3 = Path.Combine(_uploadsPath, "debug_porcao3.png");
                using (var imgDebug = SKImage.FromBitmap(porcao3))
                using (var dataDebug = imgDebug.Encode(SKEncodedImageFormat.Png, 100))
                using (var streamDebug = System.IO.File.OpenWrite(debugPath3))
                {
                    dataDebug.SaveTo(streamDebug);
                }
                _logger.LogInformation("DEBUG: Porção 3 salva em {Path}", debugPath3);

                // DEBUG: Salva porção 4
                var debugPath4 = Path.Combine(_uploadsPath, "debug_porcao4.png");
                using (var imgDebug = SKImage.FromBitmap(porcao4))
                using (var dataDebug = imgDebug.Encode(SKEncodedImageFormat.Png, 100))
                using (var streamDebug = System.IO.File.OpenWrite(debugPath4))
                {
                    dataDebug.SaveTo(streamDebug);
                }
                _logger.LogInformation("DEBUG: Porção 4 salva em {Path}", debugPath4);

                // DEBUG: Salva porção 5
                var debugPath5 = Path.Combine(_uploadsPath, "debug_porcao5.png");
                using (var imgDebug = SKImage.FromBitmap(porcao5))
                using (var dataDebug = imgDebug.Encode(SKEncodedImageFormat.Png, 100))
                using (var streamDebug = System.IO.File.OpenWrite(debugPath5))
                {
                    dataDebug.SaveTo(streamDebug);
                }
                _logger.LogInformation("DEBUG: Porção 5 salva em {Path}", debugPath5);

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Processando Porção 1 (trapézio)...",
                    porcentagem = 35
                });

                // === PORÇÃO 1: Redimensionar e transformar em trapézio ===
                const int LARGURA_BASE = 943;
                const int ALTURA_TRAPEZIO = 36;
                const int INCLINACAO = 92;

                var porcao1Redim = porcao1.Resize(
                    new SKImageInfo(LARGURA_BASE, ALTURA_TRAPEZIO),
                    SKFilterQuality.High);
                porcao1.Dispose();

                var porcao1Transformada = _graphicsTransformService.TransformPerspective(
                    input: porcao1Redim,
                    canvasWidth: LARGURA_BASE,
                    canvasHeight: ALTURA_TRAPEZIO,
                    topLeft: new SKPoint(INCLINACAO, 0),
                    topRight: new SKPoint(LARGURA_BASE - INCLINACAO, 0),
                    bottomLeft: new SKPoint(0, ALTURA_TRAPEZIO),
                    bottomRight: new SKPoint(LARGURA_BASE, ALTURA_TRAPEZIO)
                );
                porcao1Redim.Dispose();
                _logger.LogInformation("Porção 1 transformada: {Width}x{Height}", porcao1Transformada.Width, porcao1Transformada.Height);

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Processando Porção 2 (retângulo)...",
                    porcentagem = 50
                });

                // === PORÇÃO 2: Redimensionar para 943x21 (sem transformação) ===
                const int LARGURA_PORCAO2 = 943;
                const int ALTURA_PORCAO2 = 21;

                var porcao2Redim = porcao2.Resize(
                    new SKImageInfo(LARGURA_PORCAO2, ALTURA_PORCAO2),
                    SKFilterQuality.High);
                porcao2.Dispose();
                _logger.LogInformation("Porção 2 redimensionada: {Width}x{Height}", porcao2Redim.Width, porcao2Redim.Height);

                // === PORÇÃO 3: Redimensionar para 17x315 (sem transformação) ===
                const int LARGURA_PORCAO3 = 17;
                const int ALTURA_PORCAO3 = 315;

                var porcao3Redim = porcao3.Resize(
                    new SKImageInfo(LARGURA_PORCAO3, ALTURA_PORCAO3),
                    SKFilterQuality.High);
                porcao3.Dispose();
                _logger.LogInformation("Porção 3 redimensionada: {Width}x{Height}", porcao3Redim.Width, porcao3Redim.Height);

                // === PORÇÃO 4: Redimensionar para 35x315 e aplicar skew (direita sobe 37px) ===
                const int LARGURA_PORCAO4 = 35;
                const int ALTURA_PORCAO4 = 315;
                const int SKEW_PORCAO4 = 37; // Lateral direita sobe 37px

                var porcao4Redim = porcao4.Resize(
                    new SKImageInfo(LARGURA_PORCAO4, ALTURA_PORCAO4),
                    SKFilterQuality.High);
                porcao4.Dispose();
                _logger.LogInformation("Porção 4 redimensionada: {Width}x{Height}", porcao4Redim.Width, porcao4Redim.Height);

                // Aplicar skew: lateral direita sobe 37px
                // Canvas maior para acomodar o skew (altura + skew)
                int alturaCanvas = ALTURA_PORCAO4 + SKEW_PORCAO4;
                var porcao4Transformada = _graphicsTransformService.TransformPerspective(
                    input: porcao4Redim,
                    canvasWidth: LARGURA_PORCAO4,
                    canvasHeight: alturaCanvas,
                    topLeft: new SKPoint(0, SKEW_PORCAO4),            // (0, 37) - esquerda desce
                    topRight: new SKPoint(LARGURA_PORCAO4, 0),        // (35, 0) - direita no topo
                    bottomLeft: new SKPoint(0, alturaCanvas),         // (0, 352) - esquerda embaixo
                    bottomRight: new SKPoint(LARGURA_PORCAO4, ALTURA_PORCAO4)  // (35, 315) - direita sobe
                );
                porcao4Redim.Dispose();
                _logger.LogInformation("Porção 4 com skew: {Width}x{Height}", porcao4Transformada.Width, porcao4Transformada.Height);

                // === PORÇÃO 5: Redimensionar para 580x238 (sem transformação) ===
                const int LARGURA_PORCAO5 = 580;
                const int ALTURA_PORCAO5 = 238;

                var porcao5Redim = porcao5.Resize(
                    new SKImageInfo(LARGURA_PORCAO5, ALTURA_PORCAO5),
                    SKFilterQuality.High);
                porcao5.Dispose();
                _logger.LogInformation("Porção 5 redimensionada: {Width}x{Height}", porcao5Redim.Width, porcao5Redim.Height);

                // === PORÇÃO 6: Redimensionar para 550x166 (sem transformação) ===
                const int LARGURA_PORCAO6 = 550;
                const int ALTURA_PORCAO6 = 166;

                var porcao6Redim = porcao6.Resize(
                    new SKImageInfo(LARGURA_PORCAO6, ALTURA_PORCAO6),
                    SKFilterQuality.High);
                porcao6.Dispose();
                _logger.LogInformation("Porção 6 redimensionada: {Width}x{Height}", porcao6Redim.Width, porcao6Redim.Height);

                // === PORÇÃO 7: Flip horizontal e redimensionar para 550x83 ===
                var porcao7Flip = FlipHorizontal(porcao7);
                porcao7.Dispose();

                var porcao7Redim = porcao7Flip.Resize(
                    new SKImageInfo(LARGURA_PORCAO6, ALTURA_PORCAO6),
                    SKFilterQuality.High);
                porcao7Flip.Dispose();
                _logger.LogInformation("Porção 7 (flip + redim): {Width}x{Height}", porcao7Redim.Width, porcao7Redim.Height);

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Montando canvas com fundo transparente...",
                    porcentagem = 70
                });

                // === CANVAS: 1536x1024 transparente ===
                var mockup = new SKBitmap(1536, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
                using (var canvas = new SKCanvas(mockup))
                {
                    canvas.Clear(SKColors.Transparent);

                    // Porção 4 (paralelogramo/skew) em (311, 644) - CAMADA 1 (embaixo de todas)
                    canvas.DrawBitmap(porcao4Transformada, 311, 644);
                    _logger.LogInformation("Porção 4 plotada em (311, 644) - CAMADA 1");

                    // Porção 5 (retângulo grande) em (346, 681) - CAMADA 2
                    canvas.DrawBitmap(porcao5Redim, 346, 681);
                    _logger.LogInformation("Porção 5 plotada em (346, 681) - CAMADA 2");

                    // Porção 1 (trapézio) em (294, 624) - CAMADA 3
                    canvas.DrawBitmap(porcao1Transformada, 294, 624);
                    _logger.LogInformation("Porção 1 plotada em (294, 624) - CAMADA 3");

                    // Porção 2 (retângulo) em (294, 660) - CAMADA 4
                    canvas.DrawBitmap(porcao2Redim, 294, 660);
                    _logger.LogInformation("Porção 2 plotada em (294, 660) - CAMADA 4");

                    // Porção 3 (coluna) em (294, 681) - CAMADA 5
                    canvas.DrawBitmap(porcao3Redim, 294, 681);
                    _logger.LogInformation("Porção 3 plotada em (294, 681) - CAMADA 5");

                    // Porção 6 em (204, 431) - CAMADA 6
                    canvas.DrawBitmap(porcao6Redim, 204, 431);
                    _logger.LogInformation("Porção 6 plotada em (204, 431) - CAMADA 6");

                    // Porção 7 (flip) em (754, 431) - CAMADA 7
                    canvas.DrawBitmap(porcao7Redim, 754, 431);
                    _logger.LogInformation("Porção 7 plotada em (754, 431) - CAMADA 7");

                    // === OVERLAY: cozinha1.webp - ÚLTIMA CAMADA (por cima de tudo) ===
                    var overlayPath = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Cozinhas", "cozinha1.webp");
                    if (System.IO.File.Exists(overlayPath))
                    {
                        using var overlayData = System.IO.File.OpenRead(overlayPath);
                        using var overlayBitmap = SKBitmap.Decode(overlayData);

                        // Redimensiona o overlay para o tamanho do canvas se necessário
                        if (overlayBitmap.Width != 1536 || overlayBitmap.Height != 1024)
                        {
                            using var overlayResized = overlayBitmap.Resize(new SKImageInfo(1536, 1024), SKFilterQuality.High);
                            canvas.DrawBitmap(overlayResized, 0, 0);
                        }
                        else
                        {
                            canvas.DrawBitmap(overlayBitmap, 0, 0);
                        }
                        _logger.LogInformation("Overlay cozinha1.webp plotado - CAMADA FINAL");
                    }
                    else
                    {
                        _logger.LogWarning("Overlay não encontrado: {Path}", overlayPath);
                    }
                }

                porcao1Transformada.Dispose();
                porcao2Redim.Dispose();
                porcao3Redim.Dispose();
                porcao4Transformada.Dispose();
                porcao5Redim.Dispose();
                porcao6Redim.Dispose();
                porcao7Redim.Dispose();

                // === SALVA VERSÃO 1 (NORMAL) ===
                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Salvando versão 1 (normal)...",
                    porcentagem = 80
                });

                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var caminhos = new List<string>();

                // Versão 1 - Normal
                var nomeArquivo1 = $"testeX_normal_User{usuarioId}.png";
                var caminhoCompleto1 = Path.Combine(_uploadsPath, nomeArquivo1);
                if (System.IO.File.Exists(caminhoCompleto1)) System.IO.File.Delete(caminhoCompleto1);

                using (var image1 = SKImage.FromBitmap(mockup))
                using (var data1 = image1.Encode(SKEncodedImageFormat.Png, 100))
                using (var outputStream1 = System.IO.File.OpenWrite(caminhoCompleto1))
                {
                    data1.SaveTo(outputStream1);
                }
                var timestamp1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                caminhos.Add($"{nomeArquivo1}?v={timestamp1}");
                _logger.LogInformation("Versão 1 (normal) salva: {Path}", nomeArquivo1);

                mockup.Dispose();

                // === VERSÃO 2: Rotaciona imagem original 180° e gera novamente ===
                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Gerando versão 2 (180°)...",
                    porcentagem = 85
                });

                // Recarrega a imagem original para a versão 2
                SKBitmap imagemParaRotacao;
                if (!string.IsNullOrEmpty(imageId))
                {
                    var caminhoImagem2 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageId);
                    using var stream2 = System.IO.File.OpenRead(caminhoImagem2);
                    var imgOriginal2 = SKBitmap.Decode(stream2);

                    if (cropX.HasValue && cropY.HasValue && cropWidth.HasValue && cropHeight.HasValue)
                    {
                        var rectCrop2 = new SKRectI(cropX.Value, cropY.Value, cropX.Value + cropWidth.Value, cropY.Value + cropHeight.Value);
                        imagemParaRotacao = new SKBitmap(cropWidth.Value, cropHeight.Value);
                        imgOriginal2.ExtractSubset(imagemParaRotacao, rectCrop2);
                        imgOriginal2.Dispose();
                    }
                    else
                    {
                        imagemParaRotacao = imgOriginal2;
                    }
                }
                else
                {
                    // Fallback - não deveria acontecer
                    _logger.LogWarning("imageId não disponível para versão 2");
                    await EnviarEventoSSE("sucesso", new
                    {
                        mensagem = "Mockup de teste gerado com sucesso! (apenas versão 1)",
                        caminhos = caminhos.ToArray()
                    });
                    return;
                }

                // Rotaciona a imagem 180°
                var imagemRotacionada = new SKBitmap(imagemParaRotacao.Width, imagemParaRotacao.Height);
                using (var canvasRot = new SKCanvas(imagemRotacionada))
                {
                    canvasRot.RotateDegrees(180, imagemParaRotacao.Width / 2f, imagemParaRotacao.Height / 2f);
                    canvasRot.DrawBitmap(imagemParaRotacao, 0, 0);
                }
                imagemParaRotacao.Dispose();

                // Recorta as porções da imagem rotacionada
                var largura66_v2 = (int)(imagemRotacionada.Width * 0.66f);
                var altura45_v2 = (int)(imagemRotacionada.Height * 0.45f);
                var altura5_v2 = (int)(imagemRotacionada.Height * 0.05f);
                var largura5_v2 = (int)(imagemRotacionada.Width * 0.05f);
                var altura50_v2 = (int)(imagemRotacionada.Height * 0.50f);
                var yInicio50_v2 = (int)(imagemRotacionada.Height * 0.50f);
                var xInicio5percent_v2 = (int)(imagemRotacionada.Width * 0.05f);
                var largura30_v2 = (int)(imagemRotacionada.Width * 0.30f);
                var xInicio45percent_v2 = (int)(imagemRotacionada.Width * 0.45f);
                var yInicio45percent_v2 = (int)(imagemRotacionada.Height * 0.45f);
                var largura55_v2 = imagemRotacionada.Width - xInicio45percent_v2;
                var altura55_v2 = imagemRotacionada.Height - yInicio45percent_v2;
                var largura55p_v2 = (int)(imagemRotacionada.Width * 0.55f);
                var altura50p_v2 = (int)(imagemRotacionada.Height * 0.50f);

                // Porção 1 v2
                var porcao1_v2 = new SKBitmap(largura66_v2, altura45_v2);
                using (var c = new SKCanvas(porcao1_v2)) c.DrawBitmap(imagemRotacionada, new SKRectI(0, 0, largura66_v2, altura45_v2), new SKRect(0, 0, largura66_v2, altura45_v2));

                // Porção 2 v2
                var porcao2_v2 = new SKBitmap(largura66_v2, altura5_v2);
                using (var c = new SKCanvas(porcao2_v2)) c.DrawBitmap(imagemRotacionada, new SKRectI(0, altura45_v2, largura66_v2, altura45_v2 + altura5_v2), new SKRect(0, 0, largura66_v2, altura5_v2));

                // Porção 3 v2
                var porcao3_v2 = new SKBitmap(largura5_v2, altura50_v2);
                using (var c = new SKCanvas(porcao3_v2)) c.DrawBitmap(imagemRotacionada, new SKRectI(0, yInicio50_v2, largura5_v2, yInicio50_v2 + altura50_v2), new SKRect(0, 0, largura5_v2, altura50_v2));

                // Porção 4 v2
                var porcao4_v2 = new SKBitmap(largura30_v2, altura50_v2);
                using (var c = new SKCanvas(porcao4_v2)) c.DrawBitmap(imagemRotacionada, new SKRectI(xInicio5percent_v2, yInicio50_v2, xInicio5percent_v2 + largura30_v2, yInicio50_v2 + altura50_v2), new SKRect(0, 0, largura30_v2, altura50_v2));

                // Porção 5 v2
                var porcao5_v2 = new SKBitmap(largura55_v2, altura55_v2);
                using (var c = new SKCanvas(porcao5_v2)) c.DrawBitmap(imagemRotacionada, new SKRectI(xInicio45percent_v2, yInicio45percent_v2, imagemRotacionada.Width, imagemRotacionada.Height), new SKRect(0, 0, largura55_v2, altura55_v2));

                // Porção 6 v2
                var porcao6_v2 = new SKBitmap(largura55p_v2, altura50p_v2);
                using (var c = new SKCanvas(porcao6_v2)) c.DrawBitmap(imagemRotacionada, new SKRectI(0, 0, largura55p_v2, altura50p_v2), new SKRect(0, 0, largura55p_v2, altura50p_v2));

                // Porção 7 v2
                var porcao7_v2 = new SKBitmap(largura55p_v2, altura50p_v2);
                using (var c = new SKCanvas(porcao7_v2)) c.DrawBitmap(imagemRotacionada, new SKRectI(0, 0, largura55p_v2, altura50p_v2), new SKRect(0, 0, largura55p_v2, altura50p_v2));

                imagemRotacionada.Dispose();

                // Transforma porções v2
                var p1Redim_v2 = porcao1_v2.Resize(new SKImageInfo(LARGURA_BASE, ALTURA_TRAPEZIO), SKFilterQuality.High);
                porcao1_v2.Dispose();
                var p1Trans_v2 = _graphicsTransformService.TransformPerspective(p1Redim_v2, LARGURA_BASE, ALTURA_TRAPEZIO,
                    new SKPoint(INCLINACAO, 0), new SKPoint(LARGURA_BASE - INCLINACAO, 0), new SKPoint(0, ALTURA_TRAPEZIO), new SKPoint(LARGURA_BASE, ALTURA_TRAPEZIO));
                p1Redim_v2.Dispose();

                var p2Redim_v2 = porcao2_v2.Resize(new SKImageInfo(LARGURA_PORCAO2, ALTURA_PORCAO2), SKFilterQuality.High);
                porcao2_v2.Dispose();

                var p3Redim_v2 = porcao3_v2.Resize(new SKImageInfo(LARGURA_PORCAO3, ALTURA_PORCAO3), SKFilterQuality.High);
                porcao3_v2.Dispose();

                var p4Redim_v2 = porcao4_v2.Resize(new SKImageInfo(LARGURA_PORCAO4, ALTURA_PORCAO4), SKFilterQuality.High);
                porcao4_v2.Dispose();
                var p4Trans_v2 = _graphicsTransformService.TransformPerspective(p4Redim_v2, LARGURA_PORCAO4, alturaCanvas,
                    new SKPoint(0, SKEW_PORCAO4), new SKPoint(LARGURA_PORCAO4, 0), new SKPoint(0, alturaCanvas), new SKPoint(LARGURA_PORCAO4, ALTURA_PORCAO4));
                p4Redim_v2.Dispose();

                var p5Redim_v2 = porcao5_v2.Resize(new SKImageInfo(LARGURA_PORCAO5, ALTURA_PORCAO5), SKFilterQuality.High);
                porcao5_v2.Dispose();

                var p6Redim_v2 = porcao6_v2.Resize(new SKImageInfo(LARGURA_PORCAO6, ALTURA_PORCAO6), SKFilterQuality.High);
                porcao6_v2.Dispose();

                var p7Flip_v2 = FlipHorizontal(porcao7_v2);
                porcao7_v2.Dispose();
                var p7Redim_v2 = p7Flip_v2.Resize(new SKImageInfo(LARGURA_PORCAO6, ALTURA_PORCAO6), SKFilterQuality.High);
                p7Flip_v2.Dispose();

                // Monta canvas v2
                var mockup2 = new SKBitmap(1536, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
                using (var canvas2 = new SKCanvas(mockup2))
                {
                    canvas2.Clear(SKColors.Transparent);
                    canvas2.DrawBitmap(p4Trans_v2, 311, 644);
                    canvas2.DrawBitmap(p5Redim_v2, 346, 681);
                    canvas2.DrawBitmap(p1Trans_v2, 294, 624);
                    canvas2.DrawBitmap(p2Redim_v2, 294, 660);
                    canvas2.DrawBitmap(p3Redim_v2, 294, 681);
                    canvas2.DrawBitmap(p6Redim_v2, 204, 431);
                    canvas2.DrawBitmap(p7Redim_v2, 754, 431);

                    var overlayPath2 = Path.Combine(Directory.GetCurrentDirectory(), "MockupResources", "Cozinhas", "cozinha1.webp");
                    if (System.IO.File.Exists(overlayPath2))
                    {
                        using var overlayData2 = System.IO.File.OpenRead(overlayPath2);
                        using var overlayBitmap2 = SKBitmap.Decode(overlayData2);
                        if (overlayBitmap2.Width != 1536 || overlayBitmap2.Height != 1024)
                        {
                            using var overlayResized2 = overlayBitmap2.Resize(new SKImageInfo(1536, 1024), SKFilterQuality.High);
                            canvas2.DrawBitmap(overlayResized2, 0, 0);
                        }
                        else
                        {
                            canvas2.DrawBitmap(overlayBitmap2, 0, 0);
                        }
                    }
                }

                p1Trans_v2.Dispose();
                p2Redim_v2.Dispose();
                p3Redim_v2.Dispose();
                p4Trans_v2.Dispose();
                p5Redim_v2.Dispose();
                p6Redim_v2.Dispose();
                p7Redim_v2.Dispose();

                await EnviarEventoSSE("progresso", new
                {
                    etapa = "Salvando versão 2 (180°)...",
                    porcentagem = 95
                });

                // Salva versão 2
                var nomeArquivo2 = $"testeX_rotate_User{usuarioId}.png";
                var caminhoCompleto2 = Path.Combine(_uploadsPath, nomeArquivo2);
                if (System.IO.File.Exists(caminhoCompleto2)) System.IO.File.Delete(caminhoCompleto2);

                using (var image2 = SKImage.FromBitmap(mockup2))
                using (var data2 = image2.Encode(SKEncodedImageFormat.Png, 100))
                using (var outputStream2 = System.IO.File.OpenWrite(caminhoCompleto2))
                {
                    data2.SaveTo(outputStream2);
                }
                var timestamp2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                caminhos.Add($"{nomeArquivo2}?v={timestamp2}");
                _logger.LogInformation("Versão 2 (180°) salva: {Path}", nomeArquivo2);

                mockup2.Dispose();

                await EnviarEventoSSE("sucesso", new
                {
                    mensagem = "Mockup de teste gerado com sucesso!",
                    caminhos = caminhos.ToArray()
                });

                _logger.LogInformation("=== FIM Teste #1 Progressive - 2 versões geradas ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Teste #1");
                await EnviarEventoSSE("erro", new { mensagem = $"Erro: {ex.Message}" });
            }
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
