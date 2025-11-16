using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PicStoneFotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<VersionController> _logger;

        public VersionController(ILogger<VersionController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetVersion()
        {
            try
            {
                var versionFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");

                if (!System.IO.File.Exists(versionFilePath))
                {
                    _logger.LogWarning("Arquivo version.json não encontrado");
                    return Ok(new { version = "1.0000", buildDate = DateTime.UtcNow, commit = "unknown" });
                }

                var json = System.IO.File.ReadAllText(versionFilePath);
                var versionInfo = JsonSerializer.Deserialize<VersionInfo>(json);

                return Ok(versionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler versão");
                return Ok(new { version = "1.0000", buildDate = DateTime.UtcNow, commit = "error" });
            }
        }
    }

    public class VersionInfo
    {
        public string version { get; set; } = "1.0000";
        public string buildDate { get; set; } = string.Empty;
        public string commit { get; set; } = string.Empty;
    }
}
