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
        /// GET /api/migration/create-admin
        /// Força a criação do usuário admin
        /// </summary>
        [HttpGet("create-admin")]
        public async Task<IActionResult> CreateAdmin()
        {
            try
            {
                // Hash da senha "admin123"
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

                // Criar usuário diretamente no banco
                var usuario = new Models.Usuario
                {
                    Username = "admin",
                    PasswordHash = passwordHash,
                    NomeCompleto = "Administrador",
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                // Verificar se já existe
                var existente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == "admin");
                if (existente != null)
                {
                    return Ok(new
                    {
                        sucesso = true,
                        mensagem = "Usuário admin já existe",
                        usuario = new { existente.Id, existente.Username, existente.NomeCompleto }
                    });
                }

                // Adicionar e salvar
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Usuário admin criado com sucesso!",
                    usuario = new { usuario.Id, usuario.Username, usuario.NomeCompleto }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário admin");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao criar usuário admin",
                    erro = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(10).ToArray()
                });
            }
        }

        /// <summary>
        /// GET /api/migration/add-new-columns
        /// Adiciona novas colunas Material e Bloco na tabela FotosMobile
        /// </summary>
        [HttpGet("add-new-columns")]
        public async Task<IActionResult> AddNewColumns()
        {
            try
            {
                _logger.LogInformation("=== ADICIONANDO NOVAS COLUNAS ===");

                // Verifica conexão
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return StatusCode(500, new
                    {
                        sucesso = false,
                        mensagem = "Não foi possível conectar ao banco de dados"
                    });
                }

                // Adiciona colunas Material e Bloco se não existirem
                // E remove restrição NOT NULL de Lote e Processo (agora são opcionais)
                var sqlCommands = new[]
                {
                    "ALTER TABLE \"FotosMobile\" ADD COLUMN IF NOT EXISTS \"Material\" TEXT NULL",
                    "ALTER TABLE \"FotosMobile\" ADD COLUMN IF NOT EXISTS \"Bloco\" TEXT NULL",
                    "ALTER TABLE \"FotosMobile\" ALTER COLUMN \"Lote\" DROP NOT NULL",
                    "ALTER TABLE \"FotosMobile\" ALTER COLUMN \"Processo\" DROP NOT NULL"
                };

                var results = new List<object>();

                foreach (var sql in sqlCommands)
                {
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(sql);
                        _logger.LogInformation("SQL executado: {Sql}", sql);
                        results.Add(new { comando = sql, sucesso = true, erro = (string)null });
                    }
                    catch (Exception exSql)
                    {
                        _logger.LogError(exSql, "Erro ao executar: {Sql}", sql);
                        results.Add(new { comando = sql, sucesso = false, erro = exSql.Message });
                    }
                }

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Colunas adicionadas com sucesso!",
                    comandos = results,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar colunas");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao adicionar colunas",
                    erro = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// GET /api/migration/clear-photos
        /// Limpa todos os dados da tabela FotosMobile (mantém usuários)
        /// </summary>
        [HttpGet("clear-photos")]
        public async Task<IActionResult> ClearPhotos()
        {
            try
            {
                _logger.LogInformation("=== LIMPANDO TABELA DE FOTOS ===");

                // Conta quantas fotos existem
                var count = await _context.FotosMobile.CountAsync();

                // Deleta todas as fotos
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"FotosMobile\"");

                _logger.LogInformation("{Count} fotos deletadas", count);

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Tabela de fotos limpa com sucesso!",
                    fotosRemovidas = count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar tabela de fotos");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao limpar tabela de fotos",
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

        /// <summary>
        /// GET /api/migration/populate-materials
        /// Cria tabela e popula com lista de materiais
        /// </summary>
        [HttpGet("populate-materials")]
        public async Task<IActionResult> PopulateMaterials()
        {
            try
            {
                _logger.LogInformation("=== POPULANDO TABELA DE MATERIAIS ===");

                var materiais = new[]
                {
                    "NUAGE", "CALACATA", "ARTIC WHITE", "SNOW FLAKES", "WHITE LUX",
                    "DELICATUS SUPREME", "INFINITY BLUE", "TYPHOON", "LONDON SKY",
                    "MAHALO", "PERLA VENATO", "MATARAZZO", "AZUL BLUE", "SNOW WHITE",
                    "AZZURRA BAY", "VICTORIA", "BIANCO ANTICO", "FANTASY LUX",
                    "WHITE TAJ", "ARANTIS", "ALGA GREEN", "SOLARIUS"
                };

                var materiaisExistentes = await _context.Materiais.CountAsync();

                if (materiaisExistentes > 0)
                {
                    return Ok(new
                    {
                        sucesso = true,
                        mensagem = "Materiais já existem no banco",
                        total = materiaisExistentes
                    });
                }

                // Adiciona materiais em ordem alfabética
                var materiaisOrdenados = materiais.OrderBy(m => m).ToArray();
                for (int i = 0; i < materiaisOrdenados.Length; i++)
                {
                    _context.Materiais.Add(new Models.Material
                    {
                        Nome = materiaisOrdenados[i],
                        Ativo = true,
                        Ordem = i + 1
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("{Count} materiais adicionados com sucesso", materiaisOrdenados.Length);

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Materiais populados com sucesso!",
                    total = materiaisOrdenados.Length,
                    materiais = materiaisOrdenados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao popular materiais");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao popular materiais",
                    erro = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
