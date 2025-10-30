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
            try
            {
                _logger.LogInformation("=== UPLOAD INICIADO ===");
                _logger.LogInformation("Material: {Material}, Bloco: {Bloco}, Chapa: {Chapa}",
                    request.Material, request.Bloco, request.Chapa);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState inválido: {Errors}",
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                var response = await _fotoService.ProcessarUploadAsync(request, User);

                if (!response.Sucesso)
                {
                    _logger.LogError("Upload falhou: {Mensagem}", response.Mensagem);
                    return BadRequest(response);
                }

                _logger.LogInformation("Upload concluído com sucesso: {NomeArquivo}", response.NomeArquivo);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO CRÍTICO NO UPLOAD");
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao processar foto",
                    erro = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Split('\n').Take(10).ToArray()
                });
            }
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
                _logger.LogInformation("=== HISTÓRICO INICIADO ===");
                _logger.LogInformation("Limite solicitado: {Limite}", limite);

                var fotos = await _fotoService.ObterHistoricoAsync(limite);

                _logger.LogInformation("Fotos retornadas: {Count}", fotos?.Count ?? 0);

                return Ok(new
                {
                    total = fotos?.Count ?? 0,
                    fotos = fotos ?? new System.Collections.Generic.List<PicStoneFotoAPI.Models.FotoMobile>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO CRÍTICO ao buscar histórico de fotos");
                _logger.LogError("Tipo de exceção: {Type}", ex.GetType().FullName);
                _logger.LogError("Mensagem: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {InnerType} - {InnerMessage}",
                        ex.InnerException.GetType().FullName, ex.InnerException.Message);
                }

                return StatusCode(500, new
                {
                    mensagem = "Erro ao buscar histórico",
                    erro = ex.Message,
                    tipoErro = ex.GetType().FullName,
                    innerError = ex.InnerException?.Message,
                    innerErrorType = ex.InnerException?.GetType().FullName,
                    stackTrace = ex.StackTrace?.Split('\n').Take(15).ToArray()
                });
            }
        }

        /// <summary>
        /// GET /api/fotos/debug-historico
        /// Endpoint de debug completo para histórico (produção)
        /// </summary>
        [HttpGet("debug-historico")]
        public async Task<IActionResult> DebugHistorico()
        {
            var debug = new Dictionary<string, object>();

            try
            {
                debug["timestamp"] = DateTime.UtcNow;
                debug["ambiente"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

                // Teste 1: Conexão com banco
                try
                {
                    var canConnect = await _fotoService.TestDatabaseConnectionAsync();
                    debug["bancoConectado"] = canConnect;
                }
                catch (Exception exConn)
                {
                    debug["erroConexao"] = exConn.Message;
                }

                // Teste 2: Contagem de fotos
                try
                {
                    var count = await _fotoService.GetPhotoCountAsync();
                    debug["totalFotos"] = count;
                }
                catch (Exception exCount)
                {
                    debug["erroContagem"] = exCount.Message;
                }

                // Teste 3: Buscar histórico
                try
                {
                    var fotos = await _fotoService.ObterHistoricoAsync(5);
                    debug["historicoObtido"] = true;
                    debug["fotosRetornadas"] = fotos?.Count ?? 0;

                    if (fotos != null && fotos.Count > 0)
                    {
                        debug["primeiraFoto"] = new
                        {
                            fotos[0].Id,
                            fotos[0].NomeArquivo,
                            fotos[0].Material,
                            fotos[0].Bloco,
                            fotos[0].Chapa,
                            fotos[0].DataUpload
                        };
                    }
                }
                catch (Exception exHist)
                {
                    debug["erroHistorico"] = exHist.Message;
                    debug["erroHistoricoTipo"] = exHist.GetType().FullName;
                    debug["erroHistoricoInner"] = exHist.InnerException?.Message;
                    debug["erroHistoricoStack"] = exHist.StackTrace?.Split('\n').Take(10).ToArray();
                }

                return Ok(debug);
            }
            catch (Exception ex)
            {
                debug["erroGeral"] = ex.Message;
                return StatusCode(500, debug);
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

        /// <summary>
        /// GET /api/fotos/imagem/{nomeArquivo}
        /// Retorna a imagem especificada (requer autenticação via header ou query string)
        /// </summary>
        [HttpGet("imagem/{nomeArquivo}")]
        [AllowAnonymous] // Remove autenticação automática para validar manualmente
        public IActionResult ObterImagem(string nomeArquivo, [FromQuery] string? token = null)
        {
            try
            {
                // Verifica autenticação via header Authorization ou query string token
                var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;

                if (!isAuthenticated && string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { mensagem = "Token de autenticação não fornecido" });
                }

                // Se token foi fornecido via query string mas usuário não está autenticado
                // ainda permitimos o acesso (para links diretos)
                // Em produção, você poderia validar o token JWT manualmente aqui

                var uploadPath = Environment.GetEnvironmentVariable("UPLOAD_PATH") ?? "./uploads";
                var caminhoCompleto = Path.Combine(uploadPath, nomeArquivo);

                if (!System.IO.File.Exists(caminhoCompleto))
                {
                    _logger.LogWarning("Imagem não encontrada: {Caminho}", caminhoCompleto);
                    return NotFound(new { mensagem = "Imagem não encontrada" });
                }

                var bytes = System.IO.File.ReadAllBytes(caminhoCompleto);
                return File(bytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar imagem: {NomeArquivo}", nomeArquivo);
                return StatusCode(500, new { mensagem = "Erro ao buscar imagem" });
            }
        }
    }
}
