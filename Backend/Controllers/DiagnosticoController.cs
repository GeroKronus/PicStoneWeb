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
            var diagnostico = new
            {
                DiretorioAtual = Directory.GetCurrentDirectory(),
                DiretorioBase = AppContext.BaseDirectory,
                Ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),

                PastaMolduras = new
                {
                    CaminhoCompleto = Path.Combine(Directory.GetCurrentDirectory(), "Molduras"),
                    Existe = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Molduras")),
                    Arquivos = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Molduras"))
                        ? Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Molduras"))
                            .Select(f => new {
                                Nome = Path.GetFileName(f),
                                Tamanho = new FileInfo(f).Length,
                                CaminhoCompleto = f
                            }).ToList()
                        : new List<object>()
                },

                PastaUploads = new
                {
                    CaminhoCompleto = Path.Combine(Directory.GetCurrentDirectory(), "uploads"),
                    Existe = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "uploads"))
                },

                TodasPastas = Directory.GetDirectories(Directory.GetCurrentDirectory())
                    .Select(d => Path.GetFileName(d))
                    .ToList(),

                VerificacaoMolduras = new
                {
                    CavaleteSimples = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Molduras", "CAVALETE SIMPLES.png")),
                    CavaleteSimplesCinza = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Molduras", "CAVALETE SIMPLES Cinza.png")),
                    CavaleteBase = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Molduras", "CAVALETE BASE.png")),
                    CavaleteBaseCinza = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Molduras", "CAVALETE BASE Cinza.png"))
                }
            };

            _logger.LogInformation("Diagnóstico completo: {@Diagnostico}", diagnostico);

            return Ok(diagnostico);
        }
    }
}
