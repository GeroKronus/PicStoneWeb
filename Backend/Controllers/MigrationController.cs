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
        private readonly GraphicsTransformService _transformService;
        private readonly BancadaService _bancadaService;

        public MigrationController(AppDbContext context, AuthService authService, ILogger<MigrationController> logger, GraphicsTransformService transformService, BancadaService bancadaService)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
            _transformService = transformService;
            _bancadaService = bancadaService;
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
        /// GET /api/migration/add-email-verification-columns
        /// Adiciona colunas de verificação de email e sistema de aprovação na tabela Usuarios
        /// </summary>
        [HttpGet("add-email-verification-columns")]
        public async Task<IActionResult> AddEmailVerificationColumns()
        {
            try
            {
                _logger.LogInformation("=== ADICIONANDO COLUNAS DE VERIFICAÇÃO DE EMAIL ===");

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

                // Comandos SQL da migration 002_AddEmailVerificationColumns.sql
                var sqlCommands = new[]
                {
                    "ALTER TABLE \"Usuarios\" ADD COLUMN IF NOT EXISTS \"Email\" VARCHAR(255) NOT NULL DEFAULT ''",
                    "ALTER TABLE \"Usuarios\" ADD COLUMN IF NOT EXISTS \"EmailVerificado\" BOOLEAN NOT NULL DEFAULT FALSE",
                    "ALTER TABLE \"Usuarios\" ADD COLUMN IF NOT EXISTS \"TokenVerificacao\" VARCHAR(255)",
                    "ALTER TABLE \"Usuarios\" ADD COLUMN IF NOT EXISTS \"Status\" INTEGER NOT NULL DEFAULT 0",
                    "ALTER TABLE \"Usuarios\" ADD COLUMN IF NOT EXISTS \"DataExpiracao\" TIMESTAMP",
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Usuarios_Email\" ON \"Usuarios\" (\"Email\")",
                    "CREATE INDEX IF NOT EXISTS \"IX_Usuarios_TokenVerificacao\" ON \"Usuarios\" (\"TokenVerificacao\")"
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

                // Atualizar usuário admin existente se houver
                try
                {
                    var updateAdminSql = @"
                        UPDATE ""Usuarios""
                        SET
                            ""Email"" = 'admin@picstone.com.br',
                            ""EmailVerificado"" = TRUE,
                            ""Status"" = 2,
                            ""TokenVerificacao"" = NULL
                        WHERE ""Username"" = 'admin' AND (""Email"" = '' OR ""Email"" IS NULL)";

                    await _context.Database.ExecuteSqlRawAsync(updateAdminSql);
                    _logger.LogInformation("Usuário admin atualizado com email e status aprovado");
                    results.Add(new { comando = "UPDATE admin user", sucesso = true, erro = (string)null });
                }
                catch (Exception exUpdate)
                {
                    _logger.LogWarning(exUpdate, "Não foi possível atualizar admin (pode não existir ainda)");
                    results.Add(new { comando = "UPDATE admin user", sucesso = false, erro = exUpdate.Message });
                }

                var sucessos = results.Count(r => (bool)r.GetType().GetProperty("sucesso").GetValue(r));

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Colunas de verificação de email adicionadas!",
                    totalComandos = results.Count,
                    sucessos = sucessos,
                    comandos = results,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar colunas de verificação de email");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao adicionar colunas de verificação de email",
                    erro = ex.Message,
                    stackTrace = ex.StackTrace
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

                // Garante que a tabela existe usando Entity Framework (compatível com SQLite e PostgreSQL)
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Tabela Materiais verificada/criada com sucesso");

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

                // Log detalhado da inner exception
                var innerMsg = ex.InnerException?.Message ?? "Nenhuma inner exception";
                _logger.LogError("Inner Exception: {InnerMsg}", innerMsg);

                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao popular materiais",
                    erro = ex.Message,
                    innerException = innerMsg,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// POST /api/migration/test-transform
        /// Testa as transformações identificadas pela análise ultrathink
        /// Recebe imagem via upload e retorna a imagem transformada
        /// </summary>
        [HttpPost("test-transform")]
        public async Task<IActionResult> TestTransform(IFormFile imagem)
        {
            try
            {
                _logger.LogInformation("=== TESTE DE TRANSFORMAÇÃO ULTRATHINK ===");

                if (imagem == null || imagem.Length == 0)
                {
                    return BadRequest(new
                    {
                        sucesso = false,
                        mensagem = "Nenhuma imagem fornecida. Use form-data com campo 'imagem'"
                    });
                }

                _logger.LogInformation($"Imagem recebida: {imagem.FileName} ({imagem.Length} bytes)");

                // Carregar imagem original do upload
                SkiaSharp.SKBitmap imagemOriginal;
                using (var stream = imagem.OpenReadStream())
                {
                    imagemOriginal = SkiaSharp.SKBitmap.Decode(stream);
                }

                if (imagemOriginal == null)
                {
                    return StatusCode(500, new
                    {
                        sucesso = false,
                        mensagem = "Erro ao decodificar imagem"
                    });
                }

                _logger.LogInformation($"Imagem carregada: {imagemOriginal.Width}x{imagemOriginal.Height}");

                // TRANSFORMAÇÃO ESPECIALIZADA COM DISTORÇÃO NATURAL
                // Usa MapToCustomQuadrilateral() que aplica transformação de perspectiva
                // para quadrilátero irregular com vértices específicos

                _logger.LogInformation("=== USANDO TRANSFORMAÇÃO ESPECIALIZADA ===");
                _logger.LogInformation("Método: MapToCustomQuadrilateral()");
                _logger.LogInformation("Vértices: (560,714), (1624,929), (1083,1006), (193,730)");

                // Aplicar transformação com distorção natural
                // Canvas 2000x1863 (tamanho correto especificado)
                var resultado = _transformService.MapToCustomQuadrilateral(
                    input: imagemOriginal,
                    canvasWidth: 2000,
                    canvasHeight: 1863
                );

                _logger.LogInformation($"Resultado final: {resultado.Width}x{resultado.Height}");
                _logger.LogInformation("Transformação aplicada com perspectiva suave e natural");

                // Converter resultado para bytes (PNG)
                _logger.LogInformation("Codificando resultado como PNG...");
                byte[] imageBytes;
                using (var data = resultado.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                using (var ms = new System.IO.MemoryStream())
                {
                    data.SaveTo(ms);
                    imageBytes = ms.ToArray();
                }

                // Salvar também em wwwroot/debug para fácil acesso
                var debugPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "debug");
                Directory.CreateDirectory(debugPath);
                var savedPath = Path.Combine(debugPath, "ret_canvas.png");
                await System.IO.File.WriteAllBytesAsync(savedPath, imageBytes);
                _logger.LogInformation($"Imagem salva em: {savedPath}");

                // Limpar recursos
                var dimensoesOriginais = $"{imagemOriginal.Width}x{imagemOriginal.Height}";
                var dimensoesFinais = $"{resultado.Width}x{resultado.Height}";

                imagemOriginal.Dispose();
                resultado.Dispose();

                _logger.LogInformation("✅ Transformação concluída com sucesso!");

                // Retornar a imagem transformada como arquivo
                return File(imageBytes, "image/png", "ret_canvas.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aplicar transformações");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao aplicar transformações",
                    erro = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// POST /api/migration/test-bancada3
        /// Testa a geração completa da Bancada 3 com nova lógica
        /// </summary>
        [HttpPost("test-bancada3")]
        public async Task<IActionResult> TestBancada3(IFormFile imagem)
        {
            try
            {
                _logger.LogInformation("=== TESTE BANCADA 3 - NOVA LÓGICA ===");

                if (imagem == null || imagem.Length == 0)
                {
                    return BadRequest(new { mensagem = "Nenhuma imagem fornecida" });
                }

                _logger.LogInformation($"Imagem recebida: {imagem.FileName} ({imagem.Length} bytes)");

                // Carregar imagem
                SkiaSharp.SKBitmap imagemOriginal;
                using (var stream = imagem.OpenReadStream())
                {
                    imagemOriginal = SkiaSharp.SKBitmap.Decode(stream);
                }

                if (imagemOriginal == null)
                {
                    return StatusCode(500, new { mensagem = "Erro ao decodificar imagem" });
                }

                _logger.LogInformation($"Imagem carregada: {imagemOriginal.Width}x{imagemOriginal.Height}");

                // Gerar Bancada 3
                var resultados = _bancadaService.GerarBancada3(imagemOriginal, flip: false);

                if (resultados == null || resultados.Count == 0)
                {
                    return StatusCode(500, new { mensagem = "Erro ao gerar Bancada 3" });
                }

                _logger.LogInformation($"Bancada 3 gerada: {resultados.Count} variações");

                // Retornar a primeira variação
                byte[] imageBytes;
                using (var data = resultados[0].Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                using (var ms = new System.IO.MemoryStream())
                {
                    data.SaveTo(ms);
                    imageBytes = ms.ToArray();
                }

                // Salvar em debug
                var debugPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "debug");
                Directory.CreateDirectory(debugPath);
                var savedPath = Path.Combine(debugPath, "bancada3_teste.png");
                await System.IO.File.WriteAllBytesAsync(savedPath, imageBytes);
                _logger.LogInformation($"Bancada 3 salva em: {savedPath}");

                // Cleanup
                imagemOriginal.Dispose();
                foreach (var resultado in resultados)
                {
                    resultado.Dispose();
                }

                _logger.LogInformation("✅ Bancada 3 gerada com sucesso!");

                return File(imageBytes, "image/png", "bancada3_teste.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar Bancada 3");
                return StatusCode(500, new
                {
                    mensagem = "Erro ao gerar Bancada 3",
                    erro = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
