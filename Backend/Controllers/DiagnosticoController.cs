using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PicStoneFotoAPI.Data;
using Npgsql;

namespace PicStoneFotoAPI.Controllers
{
    /// <summary>
    /// Controller de diagnóstico para debug de conexão
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiagnosticoController> _logger;

        public DiagnosticoController(AppDbContext context, IConfiguration configuration, ILogger<DiagnosticoController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/diagnostico/full
        /// Diagnóstico completo do sistema
        /// </summary>
        [HttpGet("full")]
        public async Task<IActionResult> DiagnosticoCompleto()
        {
            var diagnostico = new
            {
                timestamp = DateTime.UtcNow,
                ambiente = new
                {
                    aspnetcoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
                },
                variaveisDeAmbiente = new
                {
                    databaseUrl = _configuration["DATABASE_URL"] != null ? "EXISTE" : "NÃO EXISTE",
                    databaseUrlLength = _configuration["DATABASE_URL"]?.Length ?? 0,
                    databaseUrlPrefix = _configuration["DATABASE_URL"]?.Substring(0, Math.Min(20, _configuration["DATABASE_URL"]?.Length ?? 0)) ?? "null",
                    useSqlite = _configuration["USE_SQLITE"],
                    sqlConnectionString = _configuration["SQL_CONNECTION_STRING"] != null ? "EXISTE" : "NÃO EXISTE",
                    jwtSecret = _configuration["JWT_SECRET"] != null ? "EXISTE" : "NÃO EXISTE",
                    uploadPath = _configuration["UPLOAD_PATH"]
                },
                entityFramework = new
                {
                    provedor = _context.Database.ProviderName,
                    connectionString = ObterConnectionStringMasked()
                },
                testesDeConexao = await TestarConexoes()
            };

            return Ok(diagnostico);
        }

        /// <summary>
        /// GET /api/diagnostico/env
        /// Mostra todas as variáveis de ambiente (mascaradas)
        /// </summary>
        [HttpGet("env")]
        public IActionResult VariaveisAmbiente()
        {
            var vars = new Dictionary<string, string>();

            foreach (var key in _configuration.AsEnumerable())
            {
                if (key.Value != null)
                {
                    // Mascarar valores sensíveis
                    if (key.Key.Contains("PASSWORD") || key.Key.Contains("SECRET") || key.Key.Contains("TOKEN"))
                    {
                        vars[key.Key] = "******";
                    }
                    else if (key.Key.Contains("DATABASE_URL") || key.Key.Contains("CONNECTION"))
                    {
                        vars[key.Key] = MaskConnectionString(key.Value);
                    }
                    else
                    {
                        vars[key.Key] = key.Value;
                    }
                }
            }

            return Ok(new {
                total = vars.Count,
                variaveis = vars.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value)
            });
        }

        /// <summary>
        /// GET /api/diagnostico/connection-test
        /// Testa conexão direta com PostgreSQL
        /// </summary>
        [HttpGet("connection-test")]
        public async Task<IActionResult> TestarConexaoDireta()
        {
            var resultados = new List<object>();

            // Teste 1: Via Entity Framework
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                resultados.Add(new
                {
                    teste = "Entity Framework CanConnectAsync",
                    sucesso = canConnect,
                    provedor = _context.Database.ProviderName,
                    erro = (string)null
                });
            }
            catch (Exception ex)
            {
                resultados.Add(new
                {
                    teste = "Entity Framework CanConnectAsync",
                    sucesso = false,
                    provedor = _context.Database.ProviderName,
                    erro = ex.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(5).ToArray()
                });
            }

            // Teste 2: Via CONNECTION STRING direta (DATABASE_URL)
            var databaseUrl = _configuration["DATABASE_URL"];
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                try
                {
                    using (var conn = new NpgsqlConnection(databaseUrl))
                    {
                        await conn.OpenAsync();
                        var version = conn.ServerVersion;
                        await conn.CloseAsync();

                        resultados.Add(new
                        {
                            teste = "Npgsql Connection Direta (DATABASE_URL)",
                            sucesso = true,
                            serverVersion = version,
                            connectionString = MaskConnectionString(databaseUrl),
                            erro = (string)null
                        });
                    }
                }
                catch (Exception ex)
                {
                    resultados.Add(new
                    {
                        teste = "Npgsql Connection Direta (DATABASE_URL)",
                        sucesso = false,
                        connectionString = MaskConnectionString(databaseUrl),
                        erro = ex.Message,
                        innerError = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace?.Split('\n').Take(5).ToArray()
                    });
                }
            }
            else
            {
                resultados.Add(new
                {
                    teste = "Npgsql Connection Direta (DATABASE_URL)",
                    sucesso = false,
                    erro = "DATABASE_URL não está configurada"
                });
            }

            // Teste 3: Tentar criar uma tabela temporária
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                resultados.Add(new
                {
                    teste = "Execute SQL Query (SELECT 1)",
                    sucesso = true,
                    erro = (string)null
                });
            }
            catch (Exception ex)
            {
                resultados.Add(new
                {
                    teste = "Execute SQL Query (SELECT 1)",
                    sucesso = false,
                    erro = ex.Message
                });
            }

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                totalTestes = resultados.Count,
                testesComSucesso = resultados.Count(r => (bool)r.GetType().GetProperty("sucesso").GetValue(r)),
                resultados = resultados
            });
        }

        /// <summary>
        /// GET /api/diagnostico/connectionstring-raw
        /// Análise byte a byte da connection string
        /// </summary>
        [HttpGet("connectionstring-raw")]
        public IActionResult ConnectionStringRaw()
        {
            try
            {
                var databaseUrl = _configuration["DATABASE_URL"];

                if (string.IsNullOrEmpty(databaseUrl))
                {
                    return Ok(new { erro = "DATABASE_URL está nula ou vazia" });
                }

                var primeiros50 = databaseUrl.Length > 50 ? databaseUrl.Substring(0, 50) : databaseUrl;
                var bytes = System.Text.Encoding.UTF8.GetBytes(primeiros50);
                var bytesHex = BitConverter.ToString(bytes);

                return Ok(new
                {
                    comprimento = databaseUrl.Length,
                    primeiros50Chars = primeiros50,
                    primeiros50Bytes = bytesHex,
                    primeiroChar = databaseUrl[0],
                    primeiroCharCode = (int)databaseUrl[0],
                    temEspacoNoInicio = databaseUrl.StartsWith(" "),
                    temTabNoInicio = databaseUrl.StartsWith("\t"),
                    stringTrimmed = databaseUrl.Trim(),
                    comprimentoAposTrim = databaseUrl.Trim().Length,
                    diferencaTrim = databaseUrl.Length - databaseUrl.Trim().Length
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/diagnostico/database-info
        /// Informações detalhadas do banco
        /// </summary>
        [HttpGet("database-info")]
        public async Task<IActionResult> InformacoesBanco()
        {
            try
            {
                var databaseUrl = _configuration["DATABASE_URL"];

                var info = new
                {
                    provedor = _context.Database.ProviderName,
                    connectionString = MaskConnectionString(databaseUrl),
                    podeConectar = await _context.Database.CanConnectAsync(),
                    tabelas = new List<string>()
                };

                // Tentar listar tabelas
                try
                {
                    var tabelas = await _context.Database
                        .SqlQueryRaw<string>("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
                        .ToListAsync();

                    return Ok(new
                    {
                        info.provedor,
                        info.connectionString,
                        info.podeConectar,
                        tabelas = tabelas,
                        totalTabelas = tabelas.Count
                    });
                }
                catch (Exception exTabelas)
                {
                    return Ok(new
                    {
                        info.provedor,
                        info.connectionString,
                        info.podeConectar,
                        tabelas = new List<string>(),
                        erroAoListarTabelas = exTabelas.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    erro = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(10).ToArray()
                });
            }
        }

        // Métodos auxiliares
        private async Task<object> TestarConexoes()
        {
            var testes = new List<object>();

            // Teste CanConnect
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                testes.Add(new { teste = "CanConnect", resultado = canConnect, erro = (string)null });
            }
            catch (Exception ex)
            {
                testes.Add(new { teste = "CanConnect", resultado = false, erro = ex.Message });
            }

            // Teste ExecuteSql
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                testes.Add(new { teste = "ExecuteSql", resultado = true, erro = (string)null });
            }
            catch (Exception ex)
            {
                testes.Add(new { teste = "ExecuteSql", resultado = false, erro = ex.Message });
            }

            return testes;
        }

        private string ObterConnectionStringMasked()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                return MaskConnectionString(connectionString);
            }
            catch
            {
                return "Erro ao obter connection string";
            }
        }

        private string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "null";

            // Mascarar senha
            var masked = connectionString;

            // Postgres format: postgresql://user:password@host:port/database
            if (masked.Contains("://") && masked.Contains("@"))
            {
                var parts = masked.Split("://");
                if (parts.Length > 1)
                {
                    var authAndRest = parts[1].Split("@");
                    if (authAndRest.Length > 1)
                    {
                        var auth = authAndRest[0];
                        var userPass = auth.Split(":");
                        if (userPass.Length > 1)
                        {
                            masked = $"{parts[0]}://{userPass[0]}:******@{authAndRest[1]}";
                        }
                    }
                }
            }

            return masked;
        }
    }
}
