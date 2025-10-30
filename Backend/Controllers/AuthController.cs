using Microsoft.AspNetCore.Mvc;
using PicStoneFotoAPI.Models;
using PicStoneFotoAPI.Services;

namespace PicStoneFotoAPI.Controllers
{
    /// <summary>
    /// Controller de autenticação
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/auth/login
        /// Autentica usuário e retorna token JWT
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized(new { mensagem = "Usuário ou senha inválidos" });
            }

            return Ok(response);
        }

        /// <summary>
        /// GET /api/auth/health
        /// Verifica se a API está online
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "online",
                timestamp = DateTime.Now,
                versao = "1.0.0"
            });
        }
    }
}
