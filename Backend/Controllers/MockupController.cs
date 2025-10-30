using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Models;
using PicStoneFotoAPI.Services;

namespace PicStoneFotoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MockupController : ControllerBase
    {
        private readonly MockupService _mockupService;
        private readonly ILogger<MockupController> _logger;

        public MockupController(MockupService mockupService, ILogger<MockupController> logger)
        {
            _mockupService = mockupService;
            _logger = logger;
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
    }
}
