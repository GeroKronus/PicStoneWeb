using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PicStoneFotoAPI.Data;

namespace PicStoneFotoAPI.Controllers
{
    /// <summary>
    /// Controller de materiais (sem autenticação para permitir acesso público)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MateriaisController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MateriaisController> _logger;

        public MateriaisController(AppDbContext context, ILogger<MateriaisController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/materiais
        /// Retorna lista de materiais ativos ordenados
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List()
        {
            try
            {
                var materiais = await _context.Materiais
                    .Where(m => m.Ativo)
                    .OrderBy(m => m.Ordem)
                    .ThenBy(m => m.Nome)
                    .Select(m => m.Nome)
                    .ToListAsync();

                return Ok(materiais);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar materiais");
                return StatusCode(500, new { mensagem = "Erro ao buscar materiais" });
            }
        }
    }
}
