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
        /// POST /api/auth/refresh
        /// Renova token JWT se ainda válido
        /// </summary>
        [HttpPost("refresh")]
        public IActionResult RefreshToken()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { mensagem = "Token não fornecido" });
                }

                var currentToken = authHeader.Replace("Bearer ", "");
                var newToken = _authService.RenovarToken(currentToken);

                if (newToken == null)
                {
                    return Unauthorized(new { mensagem = "Token inválido ou expirado" });
                }

                _logger.LogInformation("Token renovado com sucesso");

                return Ok(new {
                    token = newToken,
                    expiresAt = DateTime.UtcNow.AddHours(8)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao renovar token");
                return StatusCode(500, new { mensagem = "Erro ao renovar token" });
            }
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
                timestamp = DateTime.UtcNow,
                versao = "1.0.0"
            });
        }

        /// <summary>
        /// GET /api/auth/test-db
        /// Testa conexão com banco e verifica usuários
        /// </summary>
        [HttpGet("test-db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var canConnect = await _authService.TestDatabaseConnectionAsync();
                var userCount = await _authService.GetUserCountAsync();
                var adminExists = await _authService.CheckAdminExistsAsync();

                return Ok(new
                {
                    sucesso = true,
                    bancoConectado = canConnect,
                    totalUsuarios = userCount,
                    adminExiste = adminExists,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar banco de dados");
                return StatusCode(500, new
                {
                    sucesso = false,
                    erro = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(10).ToArray()
                });
            }
        }

        /// <summary>
        /// POST /api/auth/test-login
        /// Testa login com logging detalhado
        /// </summary>
        [HttpPost("test-login")]
        public async Task<IActionResult> TestLogin([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("=== TESTE DE LOGIN INICIADO ===");
                _logger.LogInformation("Username recebido: {Username}", request?.Username ?? "NULL");
                _logger.LogInformation("Password fornecida: {HasPassword}", !string.IsNullOrEmpty(request?.Password));

                if (request == null)
                {
                    return BadRequest(new { erro = "Request é null" });
                }

                if (string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest(new { erro = "Username está vazio" });
                }

                if (string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { erro = "Password está vazio" });
                }

                var response = await _authService.LoginAsync(request);

                if (response == null)
                {
                    _logger.LogWarning("Login retornou null para usuário: {Username}", request.Username);
                    return Unauthorized(new { mensagem = "Usuário ou senha inválidos" });
                }

                _logger.LogInformation("Login bem-sucedido para usuário: {Username}", request.Username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO DURANTE TESTE DE LOGIN");
                return StatusCode(500, new
                {
                    sucesso = false,
                    erro = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(15).ToArray()
                });
            }
        }
    }
}
