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
                var response = await _mockupService.GerarMockupAsync(request);

                if (!response.Sucesso)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar requisição de mockup");
                return StatusCode(500, new MockupResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro interno: {ex.Message}"
                });
            }
        }
    }
}
