using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Services;
using System.Security.Claims;

namespace PicStoneFotoAPI.Controllers
{
    /// <summary>
    /// Controller para histórico de acessos e ambientes gerados
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly HistoryService _historyService;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(HistoryService historyService, ILogger<HistoryController> logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/history/logins
        /// Retorna últimos logins do usuário logado
        /// </summary>
        [HttpGet("logins")]
        public async Task<IActionResult> GetMyLogins([FromQuery] int limite = 50)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var logins = await _historyService.GetLoginsUsuarioAsync(usuarioId, limite);

                return Ok(new
                {
                    total = logins.Count,
                    logins = logins.Select(l => new
                    {
                        dataHora = l.DataHora,
                        ipAddress = l.IpAddress,
                        userAgent = l.UserAgent
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logins");
                return StatusCode(500, new { message = "Erro ao buscar histórico de logins" });
            }
        }

        /// <summary>
        /// GET /api/history/ambientes
        /// Retorna últimos ambientes gerados pelo usuário logado
        /// </summary>
        [HttpGet("ambientes")]
        public async Task<IActionResult> GetMyAmbientes([FromQuery] int limite = 50)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var ambientes = await _historyService.GetAmbientesUsuarioAsync(usuarioId, limite);

                return Ok(new
                {
                    total = ambientes.Count,
                    ambientes = ambientes.Select(a => new
                    {
                        dataHora = a.DataHora,
                        tipoAmbiente = a.TipoAmbiente,
                        material = a.Material,
                        bloco = a.Bloco,
                        chapa = a.Chapa,
                        quantidadeImagens = a.QuantidadeImagens
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar ambientes");
                return StatusCode(500, new { message = "Erro ao buscar histórico de ambientes" });
            }
        }

        /// <summary>
        /// GET /api/history/stats
        /// Retorna estatísticas de uso do usuário logado
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetMyStats()
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var stats = await _historyService.GetUserStatsAsync(usuarioId);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas");
                return StatusCode(500, new { message = "Erro ao buscar estatísticas" });
            }
        }

        /// <summary>
        /// GET /api/history/admin/user/{usuarioId}/logins
        /// [ADMIN ONLY] Retorna logins de qualquer usuário
        /// </summary>
        [HttpGet("admin/user/{usuarioId}/logins")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserLogins(int usuarioId, [FromQuery] int limite = 100)
        {
            try
            {
                var logins = await _historyService.GetLoginsUsuarioAsync(usuarioId, limite);
                return Ok(logins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar logins do usuário {usuarioId}");
                return StatusCode(500, new { message = "Erro ao buscar histórico" });
            }
        }

        /// <summary>
        /// GET /api/history/admin/user/{usuarioId}/ambientes
        /// [ADMIN ONLY] Retorna ambientes de qualquer usuário
        /// </summary>
        [HttpGet("admin/user/{usuarioId}/ambientes")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserAmbientes(int usuarioId, [FromQuery] int limite = 100)
        {
            try
            {
                var ambientes = await _historyService.GetAmbientesUsuarioAsync(usuarioId, limite);
                return Ok(ambientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar ambientes do usuário {usuarioId}");
                return StatusCode(500, new { message = "Erro ao buscar histórico" });
            }
        }

        /// <summary>
        /// GET /api/history/admin/user/{usuarioId}/stats
        /// [ADMIN ONLY] Retorna estatísticas de qualquer usuário
        /// </summary>
        [HttpGet("admin/user/{usuarioId}/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserStats(int usuarioId)
        {
            try
            {
                var stats = await _historyService.GetUserStatsAsync(usuarioId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar stats do usuário {usuarioId}");
                return StatusCode(500, new { message = "Erro ao buscar estatísticas" });
            }
        }
    }
}
