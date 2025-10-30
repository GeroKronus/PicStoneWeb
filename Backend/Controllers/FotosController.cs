using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Models;
using PicStoneFotoAPI.Services;

namespace PicStoneFotoAPI.Controllers
{
    /// <summary>
    /// Controller de fotos (requer autenticação)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FotosController : ControllerBase
    {
        private readonly FotoService _fotoService;
        private readonly ILogger<FotosController> _logger;

        public FotosController(FotoService fotoService, ILogger<FotosController> logger)
        {
            _fotoService = fotoService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/fotos/upload
        /// Upload de foto com metadados (requer autenticação)
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] FotoUploadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _fotoService.ProcessarUploadAsync(request, User);

            if (!response.Sucesso)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// GET /api/fotos/historico
        /// Retorna histórico das últimas fotos (requer autenticação)
        /// </summary>
        [HttpGet("historico")]
        public async Task<IActionResult> Historico([FromQuery] int limite = 50)
        {
            try
            {
                var fotos = await _fotoService.ObterHistoricoAsync(limite);
                return Ok(new
                {
                    total = fotos.Count,
                    fotos = fotos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar histórico de fotos");
                return StatusCode(500, new { mensagem = "Erro ao buscar histórico" });
            }
        }

        /// <summary>
        /// GET /api/fotos/processos
        /// Retorna lista de processos disponíveis
        /// </summary>
        [HttpGet("processos")]
        public IActionResult Processos()
        {
            return Ok(new[]
            {
                "Polimento",
                "Resina",
                "Acabamento"
            });
        }
    }
}
