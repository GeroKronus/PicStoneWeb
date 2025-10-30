using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PicStoneFotoAPI.Data;
using PicStoneFotoAPI.Services;

namespace PicStoneFotoAPI.Controllers
{
    /// <summary>
    /// Controller temporário para forçar migrations (SEM autenticação para setup inicial)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(AppDbContext context, AuthService authService, ILogger<MigrationController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/migration/run
        /// Cria as tabelas e usuário admin no banco de dados
        /// </summary>
        [HttpGet("run")]
        public async Task<IActionResult> RunMigration()
        {
            try
            {
                _logger.LogInformation("=== INICIANDO MIGRATION MANUAL ===");

                // Verificar qual banco está sendo usado
                var databaseProvider = _context.Database.ProviderName;
                _logger.LogInformation("Provedor de banco: {Provider}", databaseProvider);

                // Verificar conexão
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Não foi possível conectar ao banco de dados");
                    return StatusCode(500, new
                    {
                        sucesso = false,
                        mensagem = "Erro ao conectar ao banco de dados",
                        provedor = databaseProvider
                    });
                }

                _logger.LogInformation("Conexão com banco estabelecida com sucesso");

                // Criar tabelas
                _logger.LogInformation("Criando/atualizando tabelas...");
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Tabelas criadas/atualizadas com sucesso");

                // Criar usuário admin
                _logger.LogInformation("Criando usuário admin...");
                await _authService.CriarUsuarioInicialAsync();
                _logger.LogInformation("Usuário admin criado/verificado com sucesso");

                // Verificar tabelas criadas
                var usuarios = await _context.Usuarios.CountAsync();
                var fotos = await _context.FotosMobile.CountAsync();

                _logger.LogInformation("Migration concluída - Usuários: {Usuarios}, Fotos: {Fotos}", usuarios, fotos);

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Migration executada com sucesso!",
                    detalhes = new
                    {
                        provedor = databaseProvider,
                        totalUsuarios = usuarios,
                        totalFotos = fotos,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar migration");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao executar migration",
                    erro = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// GET /api/migration/status
        /// Verifica o status do banco de dados
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var databaseProvider = _context.Database.ProviderName;
                var canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return Ok(new
                    {
                        conectado = false,
                        provedor = databaseProvider,
                        mensagem = "Não foi possível conectar ao banco de dados"
                    });
                }

                var usuarios = await _context.Usuarios.CountAsync();
                var fotos = await _context.FotosMobile.CountAsync();

                return Ok(new
                {
                    conectado = true,
                    provedor = databaseProvider,
                    totalUsuarios = usuarios,
                    totalFotos = fotos,
                    mensagem = "Banco de dados conectado e funcionando"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    erro = ex.Message,
                    mensagem = "Erro ao verificar status do banco"
                });
            }
        }
    }
}
