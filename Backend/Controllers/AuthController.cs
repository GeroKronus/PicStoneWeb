using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PicStoneFotoAPI.Data;
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
        private const string ADMIN_USERNAME = "rogerio@picstone.com.br";

        private readonly AuthService _authService;
        private readonly EmailService _emailService;
        private readonly HistoryService _historyService;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AuthService authService,
            EmailService emailService,
            HistoryService historyService,
            AppDbContext context,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _emailService = emailService;
            _historyService = historyService;
            _context = context;
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

            // Registra login no histórico
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (usuario != null)
            {
                await _historyService.RegistrarLoginAsync(usuario.Id);
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
                    expiresAt = DateTime.UtcNow.AddYears(100)
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

        // ========== GERENCIAMENTO DE USUÁRIOS ==========

        /// <summary>
        /// POST /api/auth/change-password
        /// Troca senha do usuário logado
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Extrai username do token
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new { mensagem = "Token inválido" });
                }

                var sucesso = await _authService.ChangePasswordAsync(username, request.SenhaAtual, request.NovaSenha);

                if (!sucesso)
                {
                    return BadRequest(new { mensagem = "Senha atual incorreta" });
                }

                return Ok(new { mensagem = "Senha alterada com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao trocar senha");
                return StatusCode(500, new { mensagem = "Erro ao trocar senha" });
            }
        }

        /// <summary>
        /// POST /api/auth/users
        /// Cria novo usuário (apenas admin)
        /// Senha padrão: 123456
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Verifica se é admin
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var novoUsuario = await _authService.CreateUserAsync(request.Username, request.NomeCompleto);

                if (novoUsuario == null)
                {
                    return BadRequest(new { mensagem = "Username já existe" });
                }

                return Ok(new
                {
                    mensagem = "Usuário criado com sucesso. Senha padrão: 123456",
                    usuario = novoUsuario
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário");
                return StatusCode(500, new { mensagem = "Erro ao criar usuário" });
            }
        }

        /// <summary>
        /// GET /api/auth/users
        /// Lista todos os usuários (apenas admin)
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> ListUsers()
        {
            try
            {
                // Verifica se é admin
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var usuarios = await _authService.ListUsersAsync();
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar usuários");
                return StatusCode(500, new { mensagem = "Erro ao listar usuários" });
            }
        }

        /// <summary>
        /// DELETE /api/auth/users/{id}
        /// Desativa usuário (apenas admin)
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                // Verifica se é admin
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var sucesso = await _authService.DeactivateUserAsync(id);

                if (!sucesso)
                {
                    return BadRequest(new { mensagem = "Não foi possível desativar usuário" });
                }

                return Ok(new { mensagem = "Usuário desativado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar usuário");
                return StatusCode(500, new { mensagem = "Erro ao desativar usuário" });
            }
        }

        /// <summary>
        /// PUT /api/auth/users/{id}/reactivate
        /// Reativa usuário (apenas admin)
        /// </summary>
        [HttpPut("users/{id}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int id, [FromBody] ApproveUserRequest? request)
        {
            try
            {
                // Verifica se é admin
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var dataExpiracao = request?.DataExpiracao;

                var (success, message) = await _authService.ReactivateUserAsync(id, dataExpiracao, _emailService);

                if (!success)
                {
                    return BadRequest(new { mensagem = message });
                }

                return Ok(new { mensagem = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao reativar usuário {id}");
                return StatusCode(500, new { mensagem = "Erro ao reativar usuário" });
            }
        }

        // ========== CADASTRO PÚBLICO E VERIFICAÇÃO ==========

        /// <summary>
        /// POST /api/auth/register
        /// Cadastro público de novo usuário
        /// Envia email de verificação
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Captura o endereço base do request para usar nos emails
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var (success, message, user) = await _authService.RegisterUserAsync(request, _emailService, baseUrl);

                if (!success)
                {
                    return BadRequest(new { mensagem = message });
                }

                _logger.LogInformation($"Novo cadastro: {request.Email}");

                return Ok(new
                {
                    mensagem = message,
                    usuario = new
                    {
                        id = user!.Id,
                        email = user.Email,
                        nomeCompleto = user.NomeCompleto,
                        status = user.Status.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no cadastro");
                return StatusCode(500, new { mensagem = "Erro ao realizar cadastro" });
            }
        }

        /// <summary>
        /// GET /api/auth/verify
        /// Verifica email com token enviado por email
        /// Processa a verificação e redireciona para a página principal
        /// </summary>
        [AllowAnonymous]
        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                // Processa a verificação do email no backend
                var (success, message) = await _authService.VerifyEmailAsync(token, _emailService);

                // Redireciona para a página principal com resultado
                if (success)
                {
                    return Redirect($"/?verified=success&message={Uri.EscapeDataString(message)}");
                }
                else
                {
                    return Redirect($"/?verified=error&message={Uri.EscapeDataString(message)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar email");
                return Redirect($"/?verified=error&message={Uri.EscapeDataString("Erro ao verificar email")}");
            }
        }

        // ========== GESTÃO DE SOLICITAÇÕES (ADMIN) ==========

        /// <summary>
        /// GET /api/auth/pending-users
        /// Lista usuários aguardando aprovação (apenas admin)
        /// </summary>
        [HttpGet("pending-users")]
        public async Task<IActionResult> ListPendingUsers()
        {
            try
            {
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var usuarios = await _authService.ListPendingUsersAsync();
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar usuários pendentes");
                return StatusCode(500, new { mensagem = "Erro ao listar usuários" });
            }
        }

        /// <summary>
        /// POST /api/auth/approve-user/{id}
        /// Aprova usuário (apenas admin)
        /// </summary>
        [HttpPost("approve-user/{id}")]
        public async Task<IActionResult> ApproveUser(int id, [FromBody] ApproveUserRequest? request)
        {
            try
            {
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var dataExpiracao = request?.DataExpiracao;

                var (success, message) = await _authService.ApproveUserAsync(id, dataExpiracao, _emailService);

                if (!success)
                {
                    return BadRequest(new { mensagem = message });
                }

                return Ok(new { mensagem = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao aprovar usuário {id}");
                return StatusCode(500, new { mensagem = "Erro ao aprovar usuário" });
            }
        }

        /// <summary>
        /// POST /api/auth/reject-user/{id}
        /// Rejeita usuário (apenas admin)
        /// </summary>
        [HttpPost("reject-user/{id}")]
        public async Task<IActionResult> RejectUser(int id, [FromBody] RejectUserRequest? request)
        {
            try
            {
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var motivo = request?.Motivo;

                var (success, message) = await _authService.RejectUserAsync(id, motivo, _emailService);

                if (!success)
                {
                    return BadRequest(new { mensagem = message });
                }

                return Ok(new { mensagem = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao rejeitar usuário {id}");
                return StatusCode(500, new { mensagem = "Erro ao rejeitar usuário" });
            }
        }

        /// <summary>
        /// POST /api/auth/approve-all-pending
        /// Aprova TODOS os usuários pendentes de uma vez (apenas admin)
        /// </summary>
        [HttpPost("approve-all-pending")]
        public async Task<IActionResult> ApproveAllPending([FromBody] ApproveUserRequest? request)
        {
            try
            {
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (username != ADMIN_USERNAME)
                {
                    return Forbid();
                }

                var dataExpiracao = request?.DataExpiracao;

                var (success, message, count) = await _authService.ApproveAllPendingUsersAsync(dataExpiracao, _emailService);

                if (!success)
                {
                    return BadRequest(new { mensagem = message });
                }

                return Ok(new
                {
                    mensagem = message,
                    usuariosAprovados = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aprovar usuários pendentes em lote");
                return StatusCode(500, new { mensagem = "Erro ao aprovar usuários em lote" });
            }
        }

        /// <summary>
        /// GET /api/auth/force-create-admin
        /// Força criação do usuário admin (público, use apenas uma vez)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("force-create-admin")]
        public async Task<IActionResult> ForceCreateAdmin()
        {
            try
            {
                _logger.LogInformation("Tentando criar usuário admin forçadamente...");
                await _authService.CriarUsuarioInicialAsync();

                var adminExists = await _authService.CheckAdminExistsAsync();

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Processo de criação do admin executado",
                    adminExiste = adminExists,
                    detalhes = adminExists
                        ? "Admin já existe ou foi criado agora"
                        : "Erro ao criar admin - verifique logs do servidor"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao forçar criação do admin");
                return StatusCode(500, new
                {
                    sucesso = false,
                    erro = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// GET /api/auth/test-email
        /// Testa envio de email SMTP (público, apenas para teste)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromQuery] string? to = null)
        {
            try
            {
                var destinatario = to ?? "rogerio@isidorio.com.br";

                _logger.LogInformation($"Testando envio de email para: {destinatario}");

                // Envia email de teste
                await _emailService.SendTestEmailAsync(destinatario);

                return Ok(new
                {
                    sucesso = true,
                    mensagem = $"Email de teste enviado com sucesso para {destinatario}",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email de teste");
                return StatusCode(500, new
                {
                    sucesso = false,
                    erro = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(10).ToArray()
                });
            }
        }
    }

    /// <summary>
    /// Request para aprovar usuário com data de expiração opcional
    /// </summary>
    public class ApproveUserRequest
    {
        public DateTime? DataExpiracao { get; set; }
    }

    /// <summary>
    /// Request para rejeitar usuário com motivo opcional
    /// </summary>
    public class RejectUserRequest
    {
        public string? Motivo { get; set; }
    }
}
