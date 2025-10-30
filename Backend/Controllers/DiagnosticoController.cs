using Microsoft.AspNetCore.Mvc;

namespace PicStoneFotoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticoController : ControllerBase
    {
        private readonly ILogger<DiagnosticoController> _logger;

        public DiagnosticoController(ILogger<DiagnosticoController> logger)
        {
            _logger = logger;
        }

        [HttpGet("ambiente")]
        public IActionResult VerificarAmbiente()
        {
            var caminhoDiretorio = Directory.GetCurrentDirectory();
            var caminhoMolduras = Path.Combine(caminhoDiretorio, "Molduras");
            var existeMolduras = Directory.Exists(caminhoMolduras);

            var arquivosMolduras = existeMolduras
                ? Directory.GetFiles(caminhoMolduras).Select(f => new {
                    Nome = Path.GetFileName(f),
                    Tamanho = new FileInfo(f).Length,
                    CaminhoCompleto = f
                }).ToArray()
                : Array.Empty<object>();

            var diagnostico = new
            {
                DiretorioAtual = caminhoDiretorio,
                DiretorioBase = AppContext.BaseDirectory,
                Ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),

                PastaMolduras = new
                {
                    CaminhoCompleto = caminhoMolduras,
                    Existe = existeMolduras,
                    Arquivos = arquivosMolduras
                },

                PastaUploads = new
                {
                    CaminhoCompleto = Path.Combine(caminhoDiretorio, "uploads"),
                    Existe = Directory.Exists(Path.Combine(caminhoDiretorio, "uploads"))
                },

                TodasPastas = Directory.GetDirectories(caminhoDiretorio)
                    .Select(d => Path.GetFileName(d))
                    .ToList(),

                VerificacaoMolduras = new
                {
                    CavaleteSimples = System.IO.File.Exists(Path.Combine(caminhoMolduras, "CAVALETE SIMPLES.png")),
                    CavaleteSimplesCinza = System.IO.File.Exists(Path.Combine(caminhoMolduras, "CAVALETE SIMPLES Cinza.png")),
                    CavaleteBase = System.IO.File.Exists(Path.Combine(caminhoMolduras, "CAVALETE BASE.png")),
                    CavaleteBaseCinza = System.IO.File.Exists(Path.Combine(caminhoMolduras, "CAVALETE BASE Cinza.png"))
                }
            };

            _logger.LogInformation("Diagnóstico completo: {@Diagnostico}", diagnostico);

            return Ok(diagnostico);
        }
    }
}
